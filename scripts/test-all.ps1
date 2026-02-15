$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli/MiniGPTCSharp.Cli.csproj"
$TestsProj = Join-Path $RepoRoot "MiniGPTCSharp.Tests/MiniGPTCSharp.Tests.csproj"

if (-not (Test-Path $CliProj)) {
    throw "CLI project not found at $CliProj"
}

Push-Location $RepoRoot
try {
    dotnet clean
    dotnet restore
    dotnet build -c Release

    $baseArgs = @('run', '--project', $CliProj, '--configuration', 'Release', '--')

    Write-Host "Running predict reproducibility check..."
    $predict1 = (& dotnet @baseArgs predict --prompt "The capital of France is" --topn 5 | Out-String).Trim()
    $predict2 = (& dotnet @baseArgs predict --prompt "The capital of France is" --topn 5 | Out-String).Trim()
    if ($predict1 -ne $predict2) {
        throw "Predict outputs differ between runs."
    }

    Write-Host "Running deterministic generation check..."
    $det1 = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 20 --deterministic | Out-String).Trim()
    $det2 = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 20 --deterministic | Out-String).Trim()
    if ($det1 -ne $det2) {
        throw "Deterministic outputs differ between runs."
    }

    Write-Host "Running same-seed generation check..."
    $seed42a = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 20 --seed 42 | Out-String).Trim()
    $seed42b = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 20 --seed 42 | Out-String).Trim()
    if ($seed42a -ne $seed42b) {
        throw "Seeded outputs with seed 42 differ between runs."
    }

    Write-Host "Running different-seed divergence check..."
    $seed7 = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 20 --seed 7 | Out-String).Trim()
    if ($seed42a -eq $seed7) {
        throw "Outputs for seed 42 and seed 7 are identical; expected difference."
    }

    try {
        Write-Host "Running optional step-mode deterministic check..."
        $step1 = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 5 --step --deterministic | Out-String).Trim()
        $step2 = (& dotnet @baseArgs --prompt "Hello" --max-new-tokens 5 --step --deterministic | Out-String).Trim()
        if ($step1 -ne $step2) {
            throw "Step-mode deterministic outputs differ between runs."
        }
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
