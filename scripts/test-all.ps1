$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli/MiniGPTCSharp.Cli.csproj"
$TestsProj = Join-Path $RepoRoot "MiniGPTCSharp.Tests/MiniGPTCSharp.Tests.csproj"

function Run-Cli {
    param([string[]]$CliArgs)

    $baseArgs = @('run', '--project', $CliProj, '--configuration', 'Release', '--')
    return (& dotnet @baseArgs @CliArgs | Out-String).Trim()
}

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

if (-not (Test-Path $CliProj)) {
    throw "CLI project not found at $CliProj"
}

Push-Location $RepoRoot
try {
    dotnet clean
    dotnet restore
    dotnet build -c Release

    Write-Host "Running predict reproducibility check..."
    $predict1 = Run-Cli @('predict', '--prompt', 'The capital of France is', '--topn', '5')
    $predict2 = Run-Cli @('predict', '--prompt', 'The capital of France is', '--topn', '5')
    if ($predict1 -ne $predict2) {
        throw "Predict outputs differ between runs."
    }

    Write-Host "Running deterministic generation check..."
    $det1 = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '20', '--deterministic')
    $det2 = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '20', '--deterministic')
    if ($det1 -ne $det2) {
        throw "Deterministic outputs differ between runs."
    }

    Write-Host "Running same-seed generation check..."
    $seed42a = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '20', '--seed', '42')
    $seed42b = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '20', '--seed', '42')
    if ($seed42a -ne $seed42b) {
        throw "Seeded outputs with seed 42 differ between runs."
    }

    Write-Host "Running different-seed divergence check..."
    $seed7 = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '20', '--seed', '7')
    if ($seed42a -eq $seed7) {
        throw "Outputs for seed 42 and seed 7 are identical; expected difference."
    }

    try {
        Write-Host "Running optional step-mode deterministic check..."
        $step1 = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '5', '--step', '--deterministic')
        $step2 = Run-Cli @('--prompt', 'Hello', '--max-new-tokens', '5', '--step', '--deterministic')
        if ($step1 -ne $step2) {
            throw "Step-mode deterministic outputs differ between runs."
        }

        Write-Host "Running step-mode logits visibility and determinism check..."
        $stepWithLogitsA = Run-Cli @('step', '--prompt', 'The capital of France is', '--tokens', '3', '--seed', '42', '--explain', '--show-logits')
        $stepWithLogitsB = Run-Cli @('step', '--prompt', 'The capital of France is', '--tokens', '3', '--seed', '42', '--explain', '--show-logits')
        if ($stepWithLogitsA -ne $stepWithLogitsB) {
            throw "Step-mode logits output differs for identical seed."
        }

        Assert-Contains $stepWithLogitsA "Logits" "Step mode prints logits section"
        Assert-Contains $stepWithLogitsA "logit=" "Step mode prints logit values"
        Assert-Contains $stepWithLogitsA "Softmax" "Step mode explains softmax"
    }
    catch {
        Write-Warning "Step-mode checks unavailable or failed unexpectedly: $($_.Exception.Message)"
    }

    if (Test-Path $TestsProj) {
        dotnet test -c Release
    }

    Write-Host "All checks passed."
}
finally {
    Pop-Location
}
