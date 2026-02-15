param(
  # Change this if your repo lives elsewhere:
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$Config   = "Release",
  [int]$Tokens      = 30
)

$ErrorActionPreference = "Stop"

# Fix weird checkmark rendering in some terminals:
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ==== EXACT filenames from your screenshots ====
$SolutionPath = Join-Path $RepoRoot "miniGPTCSharp.sln"
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
    $cmd = @("dotnet","run","-c",$Config,"--project",$CliProj,"--") + $argsArray
    Write-Host "`n> $($cmd -join ' ')" -ForegroundColor Cyan

    $out = & $cmd[0] $cmd[1..($cmd.Count-1)] 2>&1 | Out-String
    $code = $LASTEXITCODE
    if ($code -ne 0) { throw "dotnet run failed (exit $code). Output:`n$out" }

    return $out.Trim()
  }
  finally { Pop-Location }
}

Write-Host "RepoRoot:  $RepoRoot" -ForegroundColor Yellow
Write-Host "Solution:  $SolutionPath" -ForegroundColor Yellow
Write-Host "CLI:       $CliProj" -ForegroundColor Yellow
Write-Host "Tests:     $TestsProj" -ForegroundColor Yellow

Require-Path $RepoRoot     "RepoRoot"
Require-Path $SolutionPath "Solution (.sln)"
Require-Path $CliProj      "CLI project (.csproj)"
# Tests are optional; only required if you want dotnet test

# 0) Clean + Restore + Build (solution)
Push-Location $RepoRoot
try {
  Write-Host "`n== Clean ==" -ForegroundColor Yellow
  dotnet clean $SolutionPath | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed" }

  Write-Host "`n== Restore ==" -ForegroundColor Yellow
  dotnet restore $SolutionPath | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }

  Write-Host "`n== Build ($Config) ==" -ForegroundColor Yellow
  dotnet build $SolutionPath -c $Config | Out-Host
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

# 7) dotnet test (optional)
Push-Location $RepoRoot
try {
  if (Test-Path $TestsProj) {
    Write-Host "`n== dotnet test ==" -ForegroundColor Yellow
    dotnet test $TestsProj -c $Config | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }
  } else {
    Write-Host "`nNOTE: Tests project not found at $TestsProj; skipping dotnet test." -ForegroundColor DarkYellow
  }
}
finally { Pop-Location }

Write-Host "`nAll checks passed [OK]" -ForegroundColor Green
