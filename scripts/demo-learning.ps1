param(
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$CliProj  = "C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj",
  [string]$Config   = "Release"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

function Run([string[]]$cliArgs) {
  $cmd = @("dotnet","run","-c",$Config,"--project",$CliProj,"--") + $cliArgs
  Write-Host "`n> $($cmd -join ' ')" -ForegroundColor Cyan
  & $cmd[0] $cmd[1..($cmd.Count-1)]
  if ($LASTEXITCODE -ne 0) { throw "Command failed." }
}

function Pause($title, $whatToLookFor) {
  Write-Host "`n$title" -ForegroundColor Yellow
  if ($whatToLookFor) {
    Write-Host $whatToLookFor -ForegroundColor DarkGray
  }
  Read-Host "Press Enter to continue" | Out-Null
}

Pause "0) Help / commands" @"
Look for:
- predict / generate / step
- --explain flag
- sampling controls (seed, temperature, top-k)
"@
Run @("--help")

Pause "1) Predict (model belief: next-token probabilities)" @"
Look for:
- tokenization (tokens + IDs)
- logits -> softmax -> probabilities
- top candidates (Paris is not always #1 — that's the lesson)
"@
Run @("predict","--prompt","The capital of France is","--topn","5","--explain")

Pause "2) Deterministic generate (argmax: always pick #1)" @"
Look for:
- it explains deterministic argmax
- it repeats the loop exactly N times
"@
Run @("generate","--prompt","The capital of France is","--tokens","10","--deterministic","--explain")

Pause "3) Seeded sampling (repeatable randomness)" @"
Look for:
- it explains sampling vs argmax
- seed makes output repeatable
"@
Run @("generate","--prompt","The capital of France is","--tokens","10","--seed","42","--explain")
Run @("generate","--prompt","The capital of France is","--tokens","10","--seed","42","--explain")

Pause "4) Different seed (same probabilities, different random choices)" @"
Look for:
- output differs because random draws differ
- probabilities are the same distribution, but sampling picks different tokens
"@
Run @("generate","--prompt","The capital of France is","--tokens","10","--seed","7","--explain")

Pause "5) Step mode (token-by-token, with explanations)" @"
Look for each step:
- forward pass -> logits -> softmax
- chosen token + why
- token appended to context
This is the GPT loop: predict next token, append, repeat.
"@
Run @("step","--prompt","The capital of France is","--tokens","5","--seed","42","--explain")

Write-Host "`nDone [OK]" -ForegroundColor Green

