param(
  [string]$CliProj = "C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj",
  [string]$Config = "Release"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

function Run([string[]]$args) {
  $cmd = @("dotnet","run","-c",$Config,"--project",$CliProj,"--") + $args
  Write-Host "`n> $($cmd -join ' ')" -ForegroundColor Cyan
  & $cmd[0] $cmd[1..($cmd.Count-1)]
  if ($LASTEXITCODE -ne 0) { throw "Command failed." }
}

function Pause($title) {
  Write-Host "`n$title" -ForegroundColor Yellow
  Read-Host "Press Enter to continue"
}

Pause "1) Predict: next-token probabilities"
Run @("predict","--prompt","The capital of France is","--topn","10")

Pause "2) Deterministic generate: same output every time"
Run @("generate","--prompt","Hello my name is","--tokens","40","--deterministic")
Run @("generate","--prompt","Hello my name is","--tokens","40","--deterministic")

Pause "3) Seeded sampling: repeatable randomness"
Run @("generate","--prompt","Hello my name is","--tokens","40","--seed","42")
Run @("generate","--prompt","Hello my name is","--tokens","40","--seed","7")

Pause "4) Step + explain: watch token-by-token decisions"
Run @("step","--prompt","Once upon a time","--tokens","10","--seed","123","--explain")

Write-Host "`nDone ✅" -ForegroundColor Green
