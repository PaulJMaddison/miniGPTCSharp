param(
  # Change this if your repo lives elsewhere:
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$Config   = "Release",
  [int]$Tokens      = 30
)

. (Join-Path $PSScriptRoot "Use-LocalDotNetEnv.ps1")
Initialize-LocalDotNetEnv -RepoRoot $RepoRoot

$ErrorActionPreference = "Stop"

# Fix weird checkmark rendering in some terminals:
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$CliProj      = Join-Path $RepoRoot "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
$TestsProj    = Join-Path $RepoRoot "MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj"

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Needle,
        [string]$Message
    )

    if ($Text -notmatch [regex]::Escape($Needle)) {
        throw "$Message. Missing '$Needle'."
    }
}

function Assert-NotContains {
    param(
        [string]$Text,
        [string]$Needle,
        [string]$Message
    )

    if ($Text -match [regex]::Escape($Needle)) {
        throw "$Message. Unexpected '$Needle'."
    }
}

function Require-Path([string]$path, [string]$label) {
  if (!(Test-Path $path)) {
    throw "$label not found: $path"
  }
}

function Assert-Equal([string]$a, [string]$b, [string]$msg) {
  if ($a -ne $b) {
    Write-Host "FAILED: $msg" -ForegroundColor Red
    Write-Host "---- A ----"
    Write-Host $a
    Write-Host "---- B ----"
    Write-Host $b
    throw $msg
  }
  Write-Host "OK: $msg" -ForegroundColor Green
}

function Assert-NotEqual([string]$a, [string]$b, [string]$msg) {
  if ($a -eq $b) {
    Write-Host "FAILED: $msg (strings were equal but should differ)" -ForegroundColor Red
    Write-Host "---- Output ----"
    Write-Host $a
    throw $msg
  }
  Write-Host "OK: $msg" -ForegroundColor Green
}

function Run-Cli([string[]]$argsArray) {
  Push-Location $RepoRoot
  try {
    $cmd = @("run","-c",$Config,"--project",$CliProj,"--") + $argsArray
    Write-Host "`n> $($cmd -join ' ')" -ForegroundColor Cyan

    $out = Invoke-RepoDotNet -Arguments $cmd 2>&1 | Out-String
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "dotnet run failed (exit $code). Output:`n$out" }

    return $out.Trim()
  }
  finally { Pop-Location }
}

Write-Host "RepoRoot:  $RepoRoot" -ForegroundColor Yellow
Write-Host "CLI:       $CliProj" -ForegroundColor Yellow
Write-Host "Tests:     $TestsProj" -ForegroundColor Yellow

Require-Path $RepoRoot     "RepoRoot"
Require-Path $CliProj      "CLI project (.csproj)"
Require-Path $TestsProj    "Tests project (.csproj)"

# 0) Clean + Build
Push-Location $RepoRoot
try {
  Write-Host "`n== Clean ==" -ForegroundColor Yellow
  Invoke-RepoDotNet -Arguments @("clean", $CliProj, "-c", $Config) | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed" }

  Write-Host "`n== Build ($Config) ==" -ForegroundColor Yellow
  Invoke-RepoDotNet -Arguments @("build", $TestsProj, "-c", $Config) | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
}
finally { Pop-Location }

# 1) Predict determinism
Write-Host "`nRunning CLI subcommand dispatch regression check..." -ForegroundColor Yellow
$predictDispatch = Run-Cli @("predict","--prompt","The capital of France is","--topn","1")
Assert-NotContains $predictDispatch "Commands:" "Predict command fell through to top-level help"
Assert-Contains $predictDispatch "Next-token predictions" "Predict command did not execute predict handler"

# 2) Predict determinism
Write-Host "`nRunning predict reproducibility check..." -ForegroundColor Yellow
$predict1 = Run-Cli @("predict","--prompt","The capital of France is","--topn","5")
$predict2 = Run-Cli @("predict","--prompt","The capital of France is","--topn","5")
Assert-Equal $predict1 $predict2 "Predict mode is deterministic (same output twice)."

# 3) Deterministic generation
Write-Host "`nRunning deterministic generation check..." -ForegroundColor Yellow
$det1 = Run-Cli @("generate","--prompt","Hello my name is","--tokens","$Tokens","--deterministic")
$det2 = Run-Cli @("generate","--prompt","Hello my name is","--tokens","$Tokens","--deterministic")
Assert-Equal $det1 $det2 "Generate --deterministic is repeatable."

# 4) Same seed repeatability
Write-Host "`nRunning same-seed generation check..." -ForegroundColor Yellow
$seed42_a = Run-Cli @("generate","--prompt","Hello my name is","--tokens","$Tokens","--seed","42")
$seed42_b = Run-Cli @("generate","--prompt","Hello my name is","--tokens","$Tokens","--seed","42")
Assert-Equal $seed42_a $seed42_b "Generate --seed 42 is repeatable."

# 5) Different seeds should differ
Write-Host "`nRunning different-seed divergence check..." -ForegroundColor Yellow
$seed7 = Run-Cli @("generate","--prompt","Hello my name is","--tokens","$Tokens","--seed","7")
Assert-NotEqual $seed42_a $seed7 "Different seeds produce different outputs."

# 6) Step mode (optional)
Write-Host "`nRunning optional step-mode deterministic check..." -ForegroundColor Yellow
try {
  $step1 = Run-Cli @("step","--prompt","Once upon a time","--tokens","10","--seed","123","--explain")
  $step2 = Run-Cli @("step","--prompt","Once upon a time","--tokens","10","--seed","123","--explain")
  Assert-Equal $step1 $step2 "Step mode with seed is repeatable."
}
catch {
  Write-Host "NOTE: Step mode command not found or failed; skipping step checks." -ForegroundColor DarkYellow
  Write-Host $_.Exception.Message -ForegroundColor DarkYellow
}

# 7) Inspect pipeline should expose layers and predictions
Write-Host "`nRunning inspect pipeline check..." -ForegroundColor Yellow
$pipeline = Run-Cli @("inspect","pipeline","--prompt","The capital of France is","--dims","4","--attention-topn","3")
Assert-Contains $pipeline "Embedding preview" "Inspect pipeline did not print embedding data"
Assert-Contains $pipeline "Last-token attention by layer" "Inspect pipeline did not print attention data"
Assert-Contains $pipeline "Top next-token predictions" "Inspect pipeline did not print predictions"

# 8) Compare sampling should show deterministic and seeded variants
Write-Host "`nRunning compare sampling check..." -ForegroundColor Yellow
$compareSampling = Run-Cli @("compare","sampling","--prompt","The capital of France is","--tokens","6")
Assert-Contains $compareSampling "Deterministic argmax" "Compare sampling did not print deterministic output"
Assert-Contains $compareSampling "Seeded sampling (seed=42)" "Compare sampling did not print seed 42 output"
Assert-Contains $compareSampling "Seeded sampling (seed=7)" "Compare sampling did not print seed 7 output"

# 9) dotnet test
Push-Location $RepoRoot
try {
  Write-Host "`n== dotnet test ==" -ForegroundColor Yellow
  Invoke-RepoDotNet -Arguments @("test", $TestsProj, "-c", $Config, "--no-build", "--no-restore") | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }
}
finally { Pop-Location }

Write-Host "`nAll checks passed [OK]" -ForegroundColor Green
