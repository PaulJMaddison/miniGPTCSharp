param(
    [string]$Remote = "origin",
    [string]$PagesBranch = "gh-pages",
    [switch]$ConfigurePages,
    [switch]$SkipBuildTrigger
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Push-Location $repoRoot
try {
    if (-not (Test-Path "site/index.html")) {
        throw "Expected site/index.html to exist. Run this script from the MiniGPTSharp repository."
    }

    $status = git status --porcelain
    if ($status) {
        Write-Warning "Working tree has uncommitted changes. Publishing the committed HEAD version of site."
    }

    Write-Host "Creating a Pages commit from site/..."
    $split = git subtree split --prefix site HEAD
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($split)) {
        throw "git subtree split failed."
    }

    $refspec = "$split`:refs/heads/$PagesBranch"
    Write-Host "Pushing $PagesBranch to $Remote..."
    git push $Remote $refspec
    if ($LASTEXITCODE -ne 0) {
        throw "git push failed."
    }

    if ($ConfigurePages) {
        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            Write-Warning "GitHub CLI was not found. Configure Pages manually to deploy from $PagesBranch /."
            return
        }

        $repo = gh repo view --json nameWithOwner | ConvertFrom-Json
        $repoName = $repo.nameWithOwner

        Write-Host "Configuring GitHub Pages for $repoName..."
        gh api --method PUT "repos/$repoName/pages" -f build_type=legacy -f "source[branch]=$PagesBranch" -f "source[path]=/" | Out-Null

        if (-not $SkipBuildTrigger) {
            Write-Host "Queuing a GitHub Pages build..."
            gh api --method POST "repos/$repoName/pages/builds" | Out-Null
        }
    }

    Write-Host "Done. Pages source is $PagesBranch /."
}
finally {
    Pop-Location
}
