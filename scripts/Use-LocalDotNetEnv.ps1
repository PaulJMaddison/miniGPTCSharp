function Initialize-LocalDotNetEnv {
    param(
        [string]$RepoRoot = "C:\MiniGPT"
    )

    $localRoot = Join-Path $RepoRoot ".dotnet"
    $nugetRoot = Join-Path $localRoot "nuget"
    $toolsRoot = Join-Path $localRoot "tools"
    $homeRoot = Join-Path $localRoot "home"
    $configRoot = Join-Path $homeRoot "AppData\Roaming\NuGet"

    foreach ($path in @($localRoot, $nugetRoot, $toolsRoot, $homeRoot, $configRoot)) {
        if (!(Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }

    $env:DOTNET_CLI_HOME = $localRoot
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    $env:HOME = $homeRoot
    $env:USERPROFILE = $homeRoot
    $env:APPDATA = Join-Path $homeRoot "AppData\Roaming"
    $env:NUGET_PACKAGES = Join-Path $nugetRoot "packages"
    $env:NUGET_HTTP_CACHE_PATH = Join-Path $nugetRoot "http-cache"
    $env:NUGET_PLUGINS_CACHE_PATH = Join-Path $nugetRoot "plugins-cache"
    $env:NUGET_SCRATCH = Join-Path $nugetRoot "scratch"

    foreach ($path in @($env:NUGET_PACKAGES, $env:NUGET_HTTP_CACHE_PATH, $env:NUGET_PLUGINS_CACHE_PATH, $env:NUGET_SCRATCH)) {
        if (!(Test-Path $path)) {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
        }
    }

    $repoNuGetConfig = Join-Path $RepoRoot "NuGet.Config"
    $userNuGetConfig = Join-Path $configRoot "NuGet.Config"
    if ((Test-Path $repoNuGetConfig) -and !(Test-Path $userNuGetConfig)) {
        Copy-Item -LiteralPath $repoNuGetConfig -Destination $userNuGetConfig -Force
    }
}

function Invoke-RepoDotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $effectiveArguments = [System.Collections.Generic.List[string]]::new()
    $effectiveArguments.AddRange([string[]]$Arguments)

    if ($effectiveArguments.Count -gt 0) {
        $command = $effectiveArguments[0].ToLowerInvariant()

        if ($command -in @("build", "clean", "restore", "test")) {
            if ($effectiveArguments -notcontains "--disable-build-servers") {
                $effectiveArguments.Add("--disable-build-servers")
            }

            if ($effectiveArguments -notcontains "-m:1" -and $effectiveArguments -notcontains "/m:1") {
                $effectiveArguments.Add("-m:1")
            }
        }

        if ($command -eq "restore" -and $effectiveArguments -notcontains "--disable-parallel") {
            $effectiveArguments.Add("--disable-parallel")
        }
    }

    & dotnet @effectiveArguments
}
