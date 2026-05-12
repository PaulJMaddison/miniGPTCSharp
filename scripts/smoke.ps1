param(
  [string]$RepoRoot = "",
  [string]$Config = "Release"
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

. (Join-Path $PSScriptRoot "Use-LocalDotNetEnv.ps1")
Initialize-LocalDotNetEnv -RepoRoot $RepoRoot

$ErrorActionPreference = "Stop"

$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"

if (!(Test-Path $CliProj)) { throw "CLI project not found: $CliProj" }

Push-Location $RepoRoot
try {
  Invoke-RepoDotNet -Arguments @("build", $CliProj, "-c", $Config) | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "build failed" }

  Invoke-RepoDotNet -Arguments @("run", "-c", $Config, "--project", $CliProj, "--", "generate", "--prompt", "Hello", "--tokens", "40", "--seed", "42") | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "run failed" }
}
finally { Pop-Location }
