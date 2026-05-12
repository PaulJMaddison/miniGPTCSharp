param(
    [string]$RepoRoot = "C:\MiniGPT",
    [string]$Config = "Release",
    [string]$Prompt = "The capital of France is",
    [string]$OutPath = ""
)

. (Join-Path $PSScriptRoot "Use-LocalDotNetEnv.ps1")
Initialize-LocalDotNetEnv -RepoRoot $RepoRoot

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutPath)) {
    $OutPath = Join-Path $RepoRoot "artifacts\demo-report.html"
}

$CliProj = Join-Path $RepoRoot "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
if (!(Test-Path $CliProj)) {
    throw "CLI project not found: $CliProj"
}

Push-Location $RepoRoot
try {
    Invoke-RepoDotNet -Arguments @(
        "run",
        "-c",
        $Config,
        "--project",
        $CliProj,
        "--",
        "report",
        "--prompt",
        $Prompt,
        "--out",
        $OutPath
    ) | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "report generation failed"
    }

    Write-Host "Demo report generated: $OutPath" -ForegroundColor Green
}
finally {
    Pop-Location
}
