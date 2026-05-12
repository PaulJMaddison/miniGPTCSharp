param(
    [string]$RepoRoot = "",
    [string]$Config = "Release",
    [int]$Port = 5088,
    [switch]$KeepServer
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

. (Join-Path $PSScriptRoot "Use-LocalDotNetEnv.ps1")
Initialize-LocalDotNetEnv -RepoRoot $RepoRoot

$ErrorActionPreference = "Stop"

$webProj = Join-Path $RepoRoot "MiniGPTCSharp.Web\MiniGPTCSharp.Web.csproj"
$assetsDir = Join-Path $RepoRoot "docs\assets"
$stdout = Join-Path $assetsDir "capture-web.stdout.log"
$stderr = Join-Path $assetsDir "capture-web.stderr.log"
$url = "http://127.0.0.1:$Port"

if (!(Test-Path $webProj)) {
    throw "Web project not found: $webProj"
}

if (!(Test-Path $assetsDir)) {
    New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
}

function Wait-ForHttpOk([string]$Url, [int]$TimeoutSeconds = 45) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url"
}

$process = $null
try {
    $args = @("run", "-c", $Config, "--project", $webProj, "--urls", $url)
    Write-Host "> dotnet $($args -join ' ')" -ForegroundColor Cyan
    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList $args `
        -WorkingDirectory $RepoRoot `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr `
        -WindowStyle Hidden `
        -PassThru

    Wait-ForHttpOk "$url/health"
    Write-Host "GPT Microscope is running at $url" -ForegroundColor Green
    Write-Host ""
    Write-Host "Capture these assets into $assetsDir:" -ForegroundColor Yellow
    Write-Host "- gpt-microscope-desktop.png at 1440x1000"
    Write-Host "- gpt-microscope-mobile.png at 390x844"
    Write-Host "- gpt-report-preview.png from a generated report"
    Write-Host "- gpt-microscope-demo.gif under 10 seconds"
    Write-Host ""
    Write-Host "Suggested browser flow:"
    Write-Host "1. Open $url"
    Write-Host "2. Use prompt: The capital of France is"
    Write-Host "3. Show tokens, probabilities, attention, timeline, ablations, and report export."
}
finally {
    if (!$KeepServer -and $null -ne $process) {
        try {
            if (!$process.HasExited) {
                Stop-Process -Id $process.Id -Force
            }
        }
        catch {
            Write-Host "Could not stop web process cleanly: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }
}
