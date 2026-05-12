param(
    [string]$RepoRoot = "",
    [string]$Config = "Release",
    [string]$Prompt = "The capital of France is",
    [int]$Tokens = 8,
    [int]$WebPort = 5179,
    [switch]$SkipWeb,
    [switch]$SkipStudentScripts,
    [switch]$KeepWebServer,
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}
else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$LocalDotNetEnvScript = Join-Path $RepoRoot "scripts\Use-LocalDotNetEnv.ps1"
if (Test-Path -LiteralPath $LocalDotNetEnvScript) {
    . $LocalDotNetEnvScript
    Initialize-LocalDotNetEnv -RepoRoot $RepoRoot
}

$Solution = Join-Path $RepoRoot "miniGPTCSharp.sln"
$LibraryProj = Join-Path $RepoRoot "MiniGPTCSharp\MiniGPTCSharp.csproj"
$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
$TestsProj = Join-Path $RepoRoot "MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj"
$WebProj = Join-Path $RepoRoot "MiniGPTCSharp.Web\MiniGPTCSharp.Web.csproj"
$ArtifactsDir = Join-Path $RepoRoot "artifacts\verification"
$ReportOut = Join-Path $ArtifactsDir "gpt-microscope-report.html"
$DemoReportOut = Join-Path $ArtifactsDir "demo-report-script.html"
$WebUrl = "http://127.0.0.1:$WebPort"
$WebLogOut = Join-Path $ArtifactsDir "web.stdout.log"
$WebLogErr = Join-Path $ArtifactsDir "web.stderr.log"

New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null

$script:Failures = [System.Collections.Generic.List[string]]::new()
$script:WebProcess = $null
$script:SupportsPredictJson = $false
$script:SupportsInspectJson = $false
$script:SupportsCompareJson = $false
$script:SupportsReport = $false

function Write-Section {
    param([Parameter(Mandatory = $true)][string]$Title)

    Write-Host ""
    Write-Host "== $Title ==" -ForegroundColor Yellow
}

function Write-Pass {
    param([Parameter(Mandatory = $true)][string]$Message)

    Write-Host "PASS: $Message" -ForegroundColor Green
}

function Write-Info {
    param([Parameter(Mandatory = $true)][string]$Message)

    Write-Host "INFO: $Message" -ForegroundColor Cyan
}

function Add-Failure {
    param([Parameter(Mandatory = $true)][string]$Message)

    $script:Failures.Add($Message)
    Write-Host "FAIL: $Message" -ForegroundColor Red
}

function Complete-WithFailuresIfAny {
    if ($script:Failures.Count -eq 0) {
        return
    }

    Write-Section "Summary"
    Write-Host "Verification stopped with $($script:Failures.Count) failure(s)." -ForegroundColor Red
    foreach ($failure in $script:Failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }

    exit 1
}

function Require-Path {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (!(Test-Path -LiteralPath $Path)) {
        throw "$Label not found: $Path"
    }

    Write-Pass "$Label exists: $Path"
}

function Assert-Contains {
    param(
        [AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Text -notmatch [regex]::Escape($Needle)) {
        throw "$Message. Missing '$Needle'."
    }
}

function Assert-NotContains {
    param(
        [AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Text -match [regex]::Escape($Needle)) {
        throw "$Message. Unexpected '$Needle'."
    }
}

function Assert-JsonProperty {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($null -eq $Object.PSObject.Properties[$Name] -or $null -eq $Object.$Name) {
        throw $Message
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    Require-Path $Path "Generated file"
    $text = Get-Content -Raw -LiteralPath $Path
    Assert-Contains -Text $text -Needle $Needle -Message $Message
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [switch]$Required
    )

    Write-Section $Name
    try {
        & $Action
        Write-Pass $Name
    }
    catch {
        Add-Failure "$Name - $($_.Exception.Message)"
        if ($Required) {
            Complete-WithFailuresIfAny
        }
    }
}

function Invoke-DotNetChecked {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Label,
        [switch]$CaptureOutput
    )

    Push-Location $RepoRoot
    try {
        Write-Host "> dotnet $($Arguments -join ' ')" -ForegroundColor DarkCyan

        if (Get-Command Invoke-RepoDotNet -ErrorAction SilentlyContinue) {
            if ($CaptureOutput) {
                $output = Invoke-RepoDotNet -Arguments $Arguments 2>&1 | Out-String
            }
            else {
                Invoke-RepoDotNet -Arguments $Arguments 2>&1 | Tee-Object -Variable output | Out-Host
                $output = $output | Out-String
            }
        }
        else {
            if ($CaptureOutput) {
                $output = & dotnet @Arguments 2>&1 | Out-String
            }
            else {
                & dotnet @Arguments 2>&1 | Tee-Object -Variable output | Out-Host
                $output = $output | Out-String
            }
        }

        if ($LASTEXITCODE -ne 0) {
            throw "$Label failed with exit code $LASTEXITCODE.`n$output"
        }

        if ($CaptureOutput -or $VerboseOutput) {
            return $output.Trim()
        }

        return ""
    }
    finally {
        Pop-Location
    }
}

function Invoke-Cli {
    param([Parameter(Mandatory = $true)][string[]]$CliArguments)

    $runArgs = @("run", "--no-build", "-c", $Config, "--project", $CliProj, "--") + $CliArguments
    return Invoke-DotNetChecked -Arguments $runArgs -Label "CLI command" -CaptureOutput
}

function Convert-VerifiedJson {
    param(
        [Parameter(Mandatory = $true)][string]$Json,
        [Parameter(Mandatory = $true)][string]$Label
    )

    try {
        return $Json | ConvertFrom-Json
    }
    catch {
        throw "$Label did not return valid JSON. Raw output:`n$Json"
    }
}

function Test-CliCommandSupported {
    param(
        [Parameter(Mandatory = $true)][string]$HelpText,
        [Parameter(Mandatory = $true)][string]$Command
    )

    return $HelpText -match "(?m)^\s*$([regex]::Escape($Command))(\s|$)"
}

function Invoke-ExistingScript {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    if (!(Test-Path -LiteralPath $Path)) {
        Write-Info "Skipping missing script: $Path"
        return
    }

    Write-Host "> powershell -ExecutionPolicy Bypass -File $Path $($Arguments -join ' ')" -ForegroundColor DarkCyan
    & powershell -ExecutionPolicy Bypass -File $Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$(Split-Path -Leaf $Path) failed with exit code $LASTEXITCODE."
    }
}

function Start-WebApp {
    Require-Path $WebProj "Web project"

    if (Test-Path -LiteralPath $WebLogOut) { Remove-Item -LiteralPath $WebLogOut -Force }
    if (Test-Path -LiteralPath $WebLogErr) { Remove-Item -LiteralPath $WebLogErr -Force }

    $args = @(
        "run",
        "--no-build",
        "-c",
        $Config,
        "--project",
        $WebProj,
        "--urls",
        $WebUrl
    )

    Write-Host "> dotnet $($args -join ' ')" -ForegroundColor DarkCyan
    $script:WebProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList $args `
        -WorkingDirectory $RepoRoot `
        -RedirectStandardOutput $WebLogOut `
        -RedirectStandardError $WebLogErr `
        -WindowStyle Hidden `
        -PassThru

    Write-Info "Web logs: $WebLogOut ; $WebLogErr"
}

function Stop-WebApp {
    if ($KeepWebServer -or $null -eq $script:WebProcess) {
        if ($KeepWebServer -and $null -ne $script:WebProcess) {
            Write-Info "Keeping web process running on $WebUrl with PID $($script:WebProcess.Id)."
        }
        return
    }

    try {
        if (!$script:WebProcess.HasExited) {
            Stop-Process -Id $script:WebProcess.Id -Force
            Write-Info "Stopped web process $($script:WebProcess.Id)."
        }
    }
    catch {
        Add-Failure "Could not stop web process cleanly: $($_.Exception.Message)"
    }
}

function Wait-ForHttpOk {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastError = $null

    do {
        if ($null -ne $script:WebProcess -and $script:WebProcess.HasExited) {
            $stdout = if (Test-Path -LiteralPath $WebLogOut) { Get-Content -Raw -LiteralPath $WebLogOut } else { "" }
            $stderr = if (Test-Path -LiteralPath $WebLogErr) { Get-Content -Raw -LiteralPath $WebLogErr } else { "" }
            throw "Web process exited early with code $($script:WebProcess.ExitCode).`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return $response
            }
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url. Last error: $lastError"
}

try {
    Write-Host "MiniGPTSharp full verification" -ForegroundColor White
    Write-Host "RepoRoot: $RepoRoot"
    Write-Host "Config:   $Config"
    Write-Host "Prompt:   $Prompt"
    Write-Host "Tokens:   $Tokens"
    Write-Host "Artifacts: $ArtifactsDir"

    Invoke-Step -Name "Required paths" -Action {
        Require-Path $RepoRoot "Repo root"
        Require-Path $Solution "Solution file"
        Require-Path $LibraryProj "Library project"
        Require-Path $CliProj "CLI project"
        Require-Path $TestsProj "Test project"
        if (Test-Path -LiteralPath $WebProj) {
            Require-Path $WebProj "Web project"
        }
        else {
            Write-Info "Web project not present; web checks will be skipped."
        }
    } -Required

    Invoke-Step -Name ".NET SDK info" -Action {
        $info = Invoke-DotNetChecked -Arguments @("--info") -Label "dotnet --info" -CaptureOutput
        Assert-Contains -Text $info -Needle ".NET SDK" -Message "dotnet --info did not include SDK information"
    } -Required

    Invoke-Step -Name "Clean solution" -Action {
        Invoke-DotNetChecked -Arguments @("clean", $Solution, "-c", $Config) -Label "dotnet clean" | Out-Null
    } -Required

    Invoke-Step -Name "Build solution" -Action {
        Invoke-DotNetChecked -Arguments @("build", $Solution, "-c", $Config) -Label "dotnet build" | Out-Null
    } -Required

    Invoke-Step -Name "Run .NET tests" -Action {
        Invoke-DotNetChecked -Arguments @("test", $TestsProj, "-c", $Config, "--no-build", "--no-restore") -Label "dotnet test" | Out-Null
    } -Required

    Invoke-Step "CLI help surface" {
        $help = Invoke-Cli @("--help")
        foreach ($expected in @("predict", "generate", "step", "inspect", "compare", "learn")) {
            if (!(Test-CliCommandSupported -HelpText $help -Command $expected)) {
                throw "CLI help did not list expected command '$expected'."
            }
        }

        $script:SupportsReport = Test-CliCommandSupported -HelpText $help -Command "report"
        if ($script:SupportsReport) {
            Write-Pass "CLI help lists report"
        }
        else {
            Write-Info "CLI help does not list report; report checks will be skipped."
        }

        $predictHelp = Invoke-Cli @("predict", "--help")
        $inspectHelp = Invoke-Cli @("inspect", "--help")
        $compareHelp = Invoke-Cli @("compare", "--help")
        $script:SupportsPredictJson = $predictHelp -match "--json"
        $script:SupportsInspectJson = $inspectHelp -match "--json"
        $script:SupportsCompareJson = $compareHelp -match "--json"
    }

    Invoke-Step "Predict text mode" {
        $output = Invoke-Cli @("predict", "--prompt", $Prompt, "--topn", "5")
        Assert-Contains -Text $output -Needle "Next-token predictions" -Message "Predict text mode did not print prediction heading"
        Assert-NotContains -Text $output -Needle "Commands:" -Message "Predict text mode fell through to top-level help"
    }

    if ($script:SupportsPredictJson) {
        Invoke-Step "Predict JSON mode" {
            $json = Invoke-Cli @("predict", "--prompt", $Prompt, "--topn", "5", "--json")
            $parsed = Convert-VerifiedJson -Json $json -Label "predict --json"
            Assert-JsonProperty -Object $parsed -Name "schemaVersion" -Message "Predict JSON missing schemaVersion."
            Assert-JsonProperty -Object $parsed -Name "predictions" -Message "Predict JSON missing predictions."
            if ($parsed.predictions.Count -lt 1) { throw "Predict JSON returned no predictions." }
        }
    }
    else {
        Write-Info "Skipping predict JSON check; predict --help does not advertise --json."
    }

    Invoke-Step "Inspect pipeline text mode" {
        $output = Invoke-Cli @("inspect", "pipeline", "--prompt", $Prompt, "--dims", "4", "--attention-topn", "3")
        Assert-Contains -Text $output -Needle "Tokens:" -Message "Inspect pipeline did not print tokens"
        Assert-Contains -Text $output -Needle "Embedding preview" -Message "Inspect pipeline did not print embeddings"
        Assert-Contains -Text $output -Needle "Last-token attention by layer" -Message "Inspect pipeline did not print attention"
        Assert-Contains -Text $output -Needle "Top next-token predictions" -Message "Inspect pipeline did not print predictions"
    }

    if ($script:SupportsInspectJson) {
        Invoke-Step "Inspect pipeline JSON mode" {
            $json = Invoke-Cli @("inspect", "pipeline", "--prompt", $Prompt, "--dims", "4", "--attention-topn", "3", "--json")
            $parsed = Convert-VerifiedJson -Json $json -Label "inspect pipeline --json"
            Assert-JsonProperty -Object $parsed -Name "tokens" -Message "Inspect JSON missing tokens."
            Assert-JsonProperty -Object $parsed -Name "predictions" -Message "Inspect JSON missing predictions."
            if ($parsed.tokens.Count -lt 1) { throw "Inspect JSON returned no tokens." }
            if ($parsed.predictions.Count -lt 1) { throw "Inspect JSON returned no predictions." }
            if ($null -eq $parsed.layers -and $null -eq $parsed.attention -and $null -eq $parsed.attentionLayers) {
                throw "Inspect JSON did not include attention-like data."
            }
        }
    }
    else {
        Write-Info "Skipping inspect JSON check; inspect --help does not advertise --json."
    }

    Invoke-Step "Deterministic generation repeatability" {
        $a = Invoke-Cli @("generate", "--prompt", "Hello my name is", "--tokens", "$Tokens", "--deterministic")
        $b = Invoke-Cli @("generate", "--prompt", "Hello my name is", "--tokens", "$Tokens", "--deterministic")
        if ($a -ne $b) { throw "Deterministic generation differed between runs." }
    }

    Invoke-Step "Seeded generation repeatability and divergence" {
        $seed42a = Invoke-Cli @("generate", "--prompt", "Hello my name is", "--tokens", "$Tokens", "--seed", "42")
        $seed42b = Invoke-Cli @("generate", "--prompt", "Hello my name is", "--tokens", "$Tokens", "--seed", "42")
        $seed7 = Invoke-Cli @("generate", "--prompt", "Hello my name is", "--tokens", "$Tokens", "--seed", "7")
        if ($seed42a -ne $seed42b) { throw "Same seed did not repeat." }
        if ($seed42a -eq $seed7) { throw "Different seeds produced identical output." }
    }

    Invoke-Step "Step mode explanation and logits" {
        $output = Invoke-Cli @("step", "--prompt", $Prompt, "--tokens", "3", "--seed", "42", "--explain", "--show-logits", "--logits-topn", "5")
        Assert-Contains -Text $output -Needle "Generation Step 1" -Message "Step mode did not show generation steps"
        Assert-Contains -Text $output -Needle "Logits" -Message "Step mode did not show logits"
    }

    Invoke-Step "Compare sampling text mode" {
        $output = Invoke-Cli @("compare", "sampling", "--prompt", $Prompt, "--tokens", "6")
        Assert-Contains -Text $output -Needle "Comparison mode: sampling" -Message "Compare sampling missing heading"
        Assert-Contains -Text $output -Needle "Deterministic argmax" -Message "Compare sampling missing deterministic output"
        Assert-Contains -Text $output -Needle "Seeded sampling (seed=42)" -Message "Compare sampling missing seed 42 output"
        Assert-Contains -Text $output -Needle "Seeded sampling (seed=7)" -Message "Compare sampling missing seed 7 output"
    }

    Invoke-Step "Compare ablation text mode" {
        $output = Invoke-Cli @("compare", "ablation", "--prompt", $Prompt, "--tokens", "6")
        foreach ($expected in @("Baseline", "No attention", "No position", "No layer norm")) {
            Assert-Contains -Text $output -Needle $expected -Message "Ablation comparison missing $expected"
        }
    }

    if ($script:SupportsCompareJson) {
        Invoke-Step "Compare sampling JSON mode" {
            $json = Invoke-Cli @("compare", "sampling", "--prompt", $Prompt, "--tokens", "6", "--json")
            $parsed = Convert-VerifiedJson -Json $json -Label "compare sampling --json"
            Assert-JsonProperty -Object $parsed -Name "runs" -Message "Compare sampling JSON missing runs."
            if ($parsed.runs.Count -lt 3) { throw "Compare sampling JSON expected at least three runs." }
        }

        Invoke-Step "Compare ablation JSON mode" {
            $json = Invoke-Cli @("compare", "ablation", "--prompt", $Prompt, "--tokens", "6", "--json")
            $parsed = Convert-VerifiedJson -Json $json -Label "compare ablation --json"
            Assert-JsonProperty -Object $parsed -Name "runs" -Message "Compare ablation JSON missing runs."
            foreach ($expected in @("Baseline", "No attention", "No position embeddings", "No layer norm")) {
                if (!($parsed.runs | Where-Object { $_.label -eq $expected })) {
                    throw "Compare ablation JSON missing run '$expected'."
                }
            }
        }
    }
    else {
        Write-Info "Skipping compare JSON checks; compare --help does not advertise --json."
    }

    if ($script:SupportsReport) {
        Invoke-Step "HTML report generation" {
            $output = Invoke-Cli @("report", "--prompt", $Prompt, "--out", $ReportOut, "--tokens", "6", "--dims", "6")
            Assert-Contains -Text $output -Needle "Report written" -Message "Report command did not confirm output"
            Assert-FileContains -Path $ReportOut -Needle "GPT Internals Report" -Message "Report did not include title"
            Assert-FileContains -Path $ReportOut -Needle "tokens" -Message "Report did not include tokens"
            if ((Get-Content -Raw -LiteralPath $ReportOut) -notmatch "(?i)predictions|probabilities|probability") {
                throw "Report did not include predictions or probability data."
            }
            if ((Get-Content -Raw -LiteralPath $ReportOut) -notmatch "(?i)attention") {
                throw "Report did not include attention."
            }
            if ((Get-Content -Raw -LiteralPath $ReportOut) -notmatch "(?i)ablation") {
                throw "Report did not include ablation."
            }
        }
    }

    if (!$SkipStudentScripts) {
        Invoke-Step "Optional repo scripts" {
            Invoke-ExistingScript -Path (Join-Path $RepoRoot "scripts\smoke.ps1") -Arguments @("-RepoRoot", $RepoRoot, "-Config", $Config)
            Invoke-ExistingScript -Path (Join-Path $RepoRoot "scripts\test-all.ps1") -Arguments @("-RepoRoot", $RepoRoot, "-Config", $Config, "-Tokens", "$Tokens")
            Invoke-ExistingScript -Path (Join-Path $RepoRoot "scripts\student-labs.ps1") -Arguments @("-RepoRoot", $RepoRoot, "-Config", $Config)
            Invoke-ExistingScript -Path (Join-Path $RepoRoot "scripts\demo-report.ps1") -Arguments @("-RepoRoot", $RepoRoot, "-Config", $Config, "-Prompt", $Prompt, "-OutPath", $DemoReportOut)
            if (Test-Path -LiteralPath $DemoReportOut) {
                Assert-FileContains -Path $DemoReportOut -Needle "GPT Internals Report" -Message "demo-report.ps1 output was invalid"
            }
        }
    }
    else {
        Write-Info "Skipping optional repo/student scripts because -SkipStudentScripts was set."
    }

    if (!$SkipWeb -and (Test-Path -LiteralPath $WebProj)) {
        Invoke-Step "Web playground health, API, and report export" {
            Start-WebApp
            $health = Wait-ForHttpOk -Url "$WebUrl/health" -TimeoutSeconds 60
            Assert-Contains -Text $health.Content -Needle "GPT Microscope" -Message "Health endpoint did not identify the app"

            $homeResponse = Invoke-WebRequest -Uri $WebUrl -UseBasicParsing -TimeoutSec 10
            if ($homeResponse.Content -notmatch "GPT Microscope|MiniGPTSharp|MiniGPT") {
                throw "Home page did not include the expected product name."
            }

            $body = @{
                prompt = $Prompt
                deterministic = $true
                seed = 42
                temperature = 0.8
                topK = 10
                tokenCount = 6
                disableAttention = $false
                disablePositionEmbeddings = $false
                disableLayerNorm = $false
            } | ConvertTo-Json -Depth 5

            $inspect = Invoke-RestMethod -Uri "$WebUrl/api/inspect" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 20
            Assert-JsonProperty -Object $inspect -Name "promptTokens" -Message "Web inspect API returned no promptTokens property."
            Assert-JsonProperty -Object $inspect -Name "nextTokenCandidates" -Message "Web inspect API returned no nextTokenCandidates property."
            Assert-JsonProperty -Object $inspect -Name "timeline" -Message "Web inspect API returned no timeline property."
            if ($inspect.promptTokens.Count -lt 1) { throw "Web inspect API returned no prompt tokens." }
            if ($inspect.nextTokenCandidates.Count -lt 1) { throw "Web inspect API returned no next-token candidates." }
            if ($inspect.timeline.Count -lt 1) { throw "Web inspect API returned no timeline." }

            if ($null -ne $inspect.PSObject.Properties["ablations"]) {
                if ($inspect.ablations.Count -lt 1) { throw "Web inspect API included ablations but returned none." }
            }

            try {
                $exportResponse = Invoke-WebRequest -Uri "$WebUrl/api/export-report" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 20 -UseBasicParsing
                Assert-Contains -Text $exportResponse.Content -Needle "GPT Internals Report" -Message "Web export report endpoint returned unexpected HTML"
            }
            catch {
                throw "Web report export failed or is unavailable: $($_.Exception.Message)"
            }
        }
    }
    elseif ($SkipWeb) {
        Write-Info "Skipping web checks because -SkipWeb was set."
    }
    else {
        Write-Info "Skipping web checks because no web project was found."
    }

    Write-Section "Summary"
    if ($script:Failures.Count -gt 0) {
        Write-Host "Verification completed with $($script:Failures.Count) failure(s)." -ForegroundColor Red
        foreach ($failure in $script:Failures) {
            Write-Host "- $failure" -ForegroundColor Red
        }
        exit 1
    }

    Write-Host "All verification checks passed." -ForegroundColor Green
    Write-Host "Artifacts written to: $ArtifactsDir" -ForegroundColor Green
}
finally {
    Stop-WebApp
}
