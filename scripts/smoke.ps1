param(
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$Config = "Release"
)

$ErrorActionPreference = "Stop"

$CliProj = Join-Path $RepoRoot "MiniGPTSharp.Cli\MiniGPTSharp.Cli.csproj"

if (!(Test-Path $CliProj)) { throw "CLI project not found: $CliProj" }

Push-Location $RepoRoot
try {
  dotnet build (Join-Path $RepoRoot "miniGPTCSharp.sln") -c $Config | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "build failed" }

  dotnet run -c $Config --project $CliProj -- generate --prompt "Hello" --tokens 40 --seed 42 | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "run failed" }
}
finally { Pop-Location }
