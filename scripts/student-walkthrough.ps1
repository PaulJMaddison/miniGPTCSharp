param(
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$CliProj  = "C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj",
  [string]$Config   = "Release",
  [string]$OutputPath = $(Join-Path $RepoRoot "walkthrough-output.txt")
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$resolvedRepoRoot = if (Test-Path $RepoRoot) {
  (Resolve-Path $RepoRoot).Path
} else {
  $RepoRoot
}

$resolvedCliProj = if ($PSBoundParameters.ContainsKey("CliProj")) {
  $CliProj
} else {
  Join-Path $resolvedRepoRoot "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
}

if (-not $PSBoundParameters.ContainsKey("OutputPath")) {
  $OutputPath = Join-Path $resolvedRepoRoot "walkthrough-output.txt"
}

$parent = Split-Path -Parent $OutputPath
if ($parent -and -not (Test-Path $parent)) {
  New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

"" | Out-File -FilePath $OutputPath -Encoding utf8

function Write-Log([string]$message, [ConsoleColor]$color = [ConsoleColor]::Gray) {
  Write-Host $message -ForegroundColor $color
  $message | Out-File -FilePath $OutputPath -Append -Encoding utf8
}

function Write-Section([string]$title) {
  $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
  Write-Log ""
  Write-Log "============================================================" Yellow
  Write-Log "[$timestamp] $title" Yellow
  Write-Log "============================================================" Yellow
}

function Run-Step([string]$title, [string[]]$cliArgs) {
  Write-Section $title
  $cmd = @("dotnet", "run", "-c", $Config, "--project", $resolvedCliProj, "--") + $cliArgs
  Write-Log ("> " + ($cmd -join " ")) Cyan

  & $cmd[0] $cmd[1..($cmd.Count-1)] 2>&1 | Tee-Object -FilePath $OutputPath -Append

  if ($LASTEXITCODE -ne 0) {
    throw "Command failed with exit code $LASTEXITCODE: $($cmd -join ' ')"
  }
}

Write-Section "Student walkthrough started"
Write-Log "RepoRoot : $resolvedRepoRoot"
Write-Log "CliProj  : $resolvedCliProj"
Write-Log "Config   : $Config"
Write-Log "Output   : $OutputPath"

Run-Step "1) Help / commands" @("--help")
Run-Step "2) Predict (model belief)" @("predict", "--prompt", "The capital of France is", "--topn", "5", "--explain")
Run-Step "3) Deterministic generate (argmax)" @("generate", "--prompt", "The capital of France is", "--tokens", "10", "--deterministic", "--explain")
Run-Step "4) Seeded sampling (repeatable randomness)" @("generate", "--prompt", "The capital of France is", "--tokens", "10", "--seed", "42", "--explain")
Run-Step "5) Same seed again (prove repeatability)" @("generate", "--prompt", "The capital of France is", "--tokens", "10", "--seed", "42", "--explain")
Run-Step "6) Different seed (show different output)" @("generate", "--prompt", "The capital of France is", "--tokens", "10", "--seed", "7", "--explain")
Run-Step "7) Step mode (token-by-token loop)" @("step", "--prompt", "The capital of France is", "--tokens", "5", "--seed", "42", "--explain")

Write-Section "Walkthrough complete"
Write-Log "Walkthrough complete. Output saved to $OutputPath" Green
