# Showcase

MiniGPTSharp is not trying to be the biggest AI repo in the room. It is designed to show something more commercially useful: the ability to make complex AI behavior visible, testable, and explainable in a clean .NET product experience.

## What This Demonstrates

### Explainable AI product thinking

The GPT Microscope turns hidden model mechanics into product surfaces:

- tokenization as inspectable token chips
- logits and probabilities as ranked candidates
- attention as layer-by-layer inspection data
- generation as a step-by-step timeline
- ablations as side-by-side behavioral comparisons

That matters for internal AI tools, regulated workflows, onboarding, compliance review, technical sales, and training platforms where users need to understand what a system is doing.

### Commercial .NET engineering

The repo demonstrates a practical .NET delivery shape:

- a reusable core library
- a CLI for automation and teaching
- an ASP.NET Core web playground
- JSON exports for integration
- standalone HTML reports
- xUnit tests and GitHub Actions CI
- PowerShell scripts for repeatable local workflows

This is the kind of structure that scales from demo to internal tool without starting again.

### Local-first AI-adjacent tooling

MiniGPTSharp is intentionally offline and deterministic. There are no external model calls, no hidden APIs, and no third-party prompt processing in the toy model. That makes it useful for demos where privacy, repeatability, or classroom reliability matters.

### Visual education as a product

The repo is built around a simple belief: technical systems become more valuable when people can see how they work. The web playground, CLI, reports, and teaching scripts all point at the same product idea: explainability should be designed, not left as an afterthought.

## Client-Relevant Use Cases

- AI onboarding tools for engineering teams
- internal LLM literacy workshops
- explainable workflow prototypes
- technical sales demos for AI products
- local-first training environments
- audit-friendly model behavior reports
- executive demos that make AI behavior concrete without exposing production systems

## Three-Minute Demo Path

```powershell
dotnet build .\miniGPTCSharp.sln -c Release
powershell -ExecutionPolicy Bypass -File .\scripts\demo-report.ps1
dotnet run -c Release --project .\MiniGPTCSharp.Web\MiniGPTCSharp.Web.csproj --urls http://localhost:5088
```

Then open `http://localhost:5088` and inspect the default prompt in GPT Microscope.

## Public Static Showcase

The repo also includes a static site in `site` for GitHub Pages. Use it as the public front door:

```text
https://pauljmaddison.github.io/miniGPTCSharp/
```

That site is intentionally static, so it can be hosted for free from GitHub Pages. It previews the product story, visual design, demo data, and run commands. The local ASP.NET Core app remains the full interactive version.

## What To Point Out In A Demo

- The same core C# model powers CLI, reports, tests, and web.
- The web UI does not fake model behavior in JavaScript.
- The CLI supports both human-readable output and machine-readable JSON.
- The report generator produces a standalone artifact that can be shared outside the app.
- The ablation comparison shows architecture tradeoffs instead of just describing them.

## Positioning Line

MiniGPTSharp is a compact proof that your company can turn AI complexity into software people can inspect, trust, test, and use.
