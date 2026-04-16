param(
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$CliProj  = "C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj",
  [string]$Config   = "Release"
)

. (Join-Path $PSScriptRoot "Use-LocalDotNetEnv.ps1")
Initialize-LocalDotNetEnv -RepoRoot $RepoRoot

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Run-Lab([string]$title, [string[]]$cliArgs) {
  $cmd = @("run", "-c", $Config, "--project", $CliProj, "--") + $cliArgs
  Write-Host "`n===== $title =====" -ForegroundColor Yellow
  Write-Host "> $($cmd -join ' ')" -ForegroundColor Cyan
  Invoke-RepoDotNet -Arguments $cmd
  if ($LASTEXITCODE -ne 0) { throw "Command failed." }
}

Run-Lab "Lab 1: Tokenization" @("inspect","tokens","--prompt","The capital of France is Paris.")
Run-Lab "Lab 2: Embeddings" @("inspect","embeddings","--prompt","AI model learning","--layers","0","--dims","8")
Run-Lab "Lab 3: Attention" @("inspect","attention","--prompt","The capital of France is Paris","--attention-topn","5")
Run-Lab "Lab 4: Sampling comparison" @("compare","sampling","--prompt","The capital of France is","--tokens","8")
Run-Lab "Lab 5: Architecture ablation" @("compare","ablation","--prompt","The capital of France is","--tokens","8")

Write-Host "`nAll student labs completed [OK]" -ForegroundColor Green
