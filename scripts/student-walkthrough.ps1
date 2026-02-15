param(
  [string]$RepoRoot = "C:\MiniGPT",
  [string]$CliProj  = "C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj",
  [string]$Config   = "Release",
  [string]$OutFile  = "walkthrough-output.txt"
)

# Console output as UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

# Ensure output file is next to this script (unless absolute path provided)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (![System.IO.Path]::IsPathRooted($OutFile)) {
  $OutFile = Join-Path $scriptDir $OutFile
}

function Strip-Nul([string]$s) {
  if ($null -eq $s) { return "" }
  return ($s -replace "`0", "")
}

function Append-Utf8([string]$text) {
  $clean = Strip-Nul $text
  if ($clean.Length -eq 0) { return }
  [System.IO.File]::AppendAllText($OutFile, $clean + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
}

function Write-Both([string]$text) {
  $clean = Strip-Nul $text
  Write-Host $clean
  Append-Utf8 $clean
}

function Run([string[]]$cliArgs, [string]$title) {
  $cmd = @("dotnet","run","-c",$Config,"--project",$CliProj,"--") + $cliArgs
  $cmdLine = "> " + ($cmd -join ' ')

  Write-Both ""
  Write-Both ("===== " + $title + " =====")
  Write-Both $cmdLine
  Write-Both ""

  # Capture ALL output, then sanitize it before writing to file.
  $raw = & $cmd[0] $cmd[1..($cmd.Count-1)] 2>&1 | Out-Strin
  $code = $LASTEXITCODE

  $out = Strip-Nul ($raw.TrimEnd())
  if ($out.Length -gt 0) {
    Write-Host $out
    Append-Utf8 $out
  }

  if ($code -ne 0) {
    throw "Command failed with exit code $($code): $cmdLine"
  }
}

function Pause([string]$title, [string]$note) {
  Write-Both ""
  Write-Both ("--- " + $title + " ---")
  if ($note) { Write-Both $note }
  Read-Host "Press Enter to continue" | Out-Null
}

# --- Preconditions ---
if (!(Test-Path $CliProj)) { throw "CLI project not found: $CliProj" }

# Reset output file (UTF-8)
[System.IO.File]::WriteAllText(
  $OutFile,
  "MiniGPT Student Walkthrough - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" + [Environment]::NewLine,
  [System.Text.Encoding]::UTF8
)

Write-Both "RepoRoot : $RepoRoot"
Write-Both "CliProj  : $CliProj"
Write-Both "Config   : $Config"
Write-Both "Output   : $OutFile"

Pause "0) Help / commands" @"
Look for:
- predict / generate / step
- --explain flag
- sampling controls (seed, temperature, top-k)
"@
Run @("--help") "Help"

Pause "1) Predict (next-token probabilities + logits explanation)" @"
Goal:
- See tokens + IDs
- See logits -> probabilities
- Notice the highest probability is NOT always the human "correct" answer
"@
Run @("predict","--prompt","The capital of France is","--topn","5","--explain") "Predict (top 5)"

Pause "2) Deterministic generate (argmax)" @"
Goal:
- Greedy decoding: always pick the highest-probability token each step
- Repeatable (no randomness)
"@
Run @("generate","--prompt","The capital of France is","--tokens","8","--deterministic","--explain") "Generate deterministic"

Pause "3) Seeded sampling (repeatable randomness)" @"
Goal:
- Sampling uses randomness, but a fixed seed makes it repeatable
- Run twice with the same seed: identical output
"@
Run @("generate","--prompt","The capital of France is","--tokens","8","--seed","42","--explain") "Generate seed=42 (run 1)"
Run @("generate","--prompt","The capital of France is","--tokens","8","--seed","42","--explain") "Generate seed=42 (run 2)"

Pause "4) Different seed (same distribution, different random draws)" @"
Goal:
- Probabilities can be similar, but random draws differ
- Different seed => often different text
"@
Run @("generate","--prompt","The capital of France is","--tokens","8","--seed","7","--explain") "Generate seed=7"

Pause "5) Step mode (token-by-token teaching loop)" @"
Goal:
- Watch the core GPT loop:
  1) forward pass -> logits
  2) softmax -> probabilities
  3) pick a token (argmax or sampling)
  4) append it
  5) repeat
"@
Run @("step","--prompt","The capital of France is","--tokens","5","--seed","42","--explain","--show-logits","--logits-topn","10","--logits-format","raw") "Step mode (seed=42)"

Write-Both ""
Write-Both "Done [OK]"
Write-Host "`nSaved walkthrough output to: $OutFile" -ForegroundColor Green
