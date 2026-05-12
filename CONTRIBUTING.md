# Contributing

Thanks for helping keep MiniGPTSharp useful as a teaching repo and credible as a .NET portfolio sample.

## Prerequisites

- .NET 8 SDK
- PowerShell 7 or Windows PowerShell
- Git

The repo includes `global.json` so local builds stay on the .NET 8 SDK family.

## Local Setup

For most machines, direct `dotnet` commands are enough:

```powershell
dotnet restore .\miniGPTCSharp.sln
dotnet build .\miniGPTCSharp.sln -c Release --no-restore
dotnet test .\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj -c Release --no-build --no-restore
```

If your user profile cannot write normal .NET or NuGet state, use the repo-local helper:

```powershell
. .\scripts\Use-LocalDotNetEnv.ps1
Initialize-LocalDotNetEnv
Invoke-RepoDotNet -Arguments @("restore", ".\miniGPTCSharp.sln")
Invoke-RepoDotNet -Arguments @("build", ".\miniGPTCSharp.sln", "-c", "Release", "--no-restore")
Invoke-RepoDotNet -Arguments @("test", ".\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj", "-c", "Release", "--no-build", "--no-restore")
```

## Useful Checks

Run the quick smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke.ps1
```

Run the full local regression flow:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-all.ps1
```

Run the short teaching labs:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-labs.ps1
```

Generate the demo HTML report:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\demo-report.ps1
```

Run the GPT Microscope playground:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Web\MiniGPTCSharp.Web.csproj --urls http://localhost:5088
```

## Test Expectations

- Preserve deterministic golden outputs unless the model behavior intentionally changes.
- Keep prompt inspection coverage when adding or changing internals.
- Add or update CLI smoke checks when changing command, JSON export, report, or web behavior.
- Update docs when a command, script, or teaching flow changes.

## Contribution Style

- Prefer simple, readable C# over clever abstractions.
- Preserve the educational purpose: internals should be easy to inspect and explain.
- Keep dependencies minimal.
- Do not add production-LLM claims; this is a teaching model.
- Keep scripts usable from any clone path, not only `C:\MiniGPT`.
