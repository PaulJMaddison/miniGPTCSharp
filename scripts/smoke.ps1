$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli/MiniGPTCSharp.Cli.csproj"

if (-not (Test-Path $CliProj)) {
    throw "CLI project not found at $CliProj"
}

Push-Location $RepoRoot
try {
    dotnet clean
    dotnet restore
    dotnet build -c Release
    dotnet run --project $CliProj --configuration Release -- --prompt "Hello" --max-new-tokens 40 --seed 42
}
finally {
    Pop-Location
}
