# Architecture

MiniGPTSharp is intentionally small: one model library, one teaching CLI, one ASP.NET Core playground, one test project, and a few PowerShell scripts. The design goal is to make GPT-style mechanics visible enough for teaching while keeping the repo credible as a .NET portfolio sample.

## Solution Layout

```text
MiniGPTCSharp/
  MiniGPTCSharp.csproj          Core toy model library
MiniGPTCSharp.Cli/
  MiniGPTCSharp.Cli.csproj      Teaching-oriented command-line interface
MiniGPTCSharp.Web/
  MiniGPTCSharp.Web.csproj      GPT Microscope web playground
MiniGPTCSharp.Tests/
  MiniGPTCSharp.Tests.csproj    xUnit regression tests
site/
  index.html                    Static GitHub Pages showcase
scripts/
  *.ps1                         Local setup, smoke tests, labs, walkthroughs
docs/
  architecture.md               This overview
  showcase.md                   Client-facing product and portfolio framing
  teaching-guide.md             30-minute workshop plan
  assets/                       README, release, and demo visuals
ROADMAP.md                      Product and engineering improvement path
SECURITY.md                     Local-first behavior and security scope
.github/workflows/
  dotnet-ci.yml                 Windows .NET build/test workflow
```

## Core Library

`MiniGPTCSharp` contains the model and the inspection/export types used by the CLI and web playground.

- `MiniGptModel` orchestrates tokenization, embeddings, transformer blocks, logits, sampling, generation, and prompt inspection.
- `VocabularyTokenizer` owns the tiny seed vocabulary and adds unknown prompt tokens at runtime so learners can see token IDs change.
- `Tensor`, `SelfAttention`, and `TransformerBlock` keep the model math visible without pulling in a machine-learning framework.
- `GptConfig` exposes teaching toggles such as layer count, top-k, temperature, disabled attention, disabled position embeddings, and disabled layer normalization.
- `PromptInspection`, `NextTokenPrediction`, and `GenerationStepResult` are small DTOs that let the CLI print internals without duplicating model logic.
- `ExportDtos`, `MiniGptExports`, and `MiniGptHtmlReport` provide stable JSON/report shapes for automation and portfolio artifacts.

The library favors deterministic, inspectable behavior over realism. It is not a production inference runtime.

## CLI

`MiniGPTCSharp.Cli` is the teaching surface. It keeps all features available through subcommands rather than requiring users to edit code.

- `predict` shows next-token probabilities.
- `generate` runs the autoregressive loop.
- `step` shows generation one token at a time.
- `inspect` exposes tokens, embeddings, attention, or the full pipeline.
- `compare sampling` contrasts probabilities, argmax, and seeded sampling.
- `compare ablation` shows how output changes when parts of the toy architecture are disabled.
- `report` writes a standalone HTML report for a prompt.
- `learn` gives students curated entry points for common topics.

The CLI is intentionally verbose in explanation modes because its primary job is to make the mental model observable.

JSON output is available for `predict`, `inspect`, and `compare` so tests, scripts, and external tools can consume stable camelCase shapes instead of parsing console prose.

## Web Playground

`MiniGPTCSharp.Web` is the GPT Microscope playground. It uses ASP.NET Core minimal APIs and static assets under `wwwroot`.

- `/health` returns a simple health payload for smoke checks.
- `/api/inspect` runs the same model pipeline used by the CLI and returns prompt tokens, next-token candidates, attention layers, generation timeline, ablations, and report availability.
- `/api/export-report` returns the standalone HTML report for download.

The web project depends on the core library rather than reimplementing model behavior.

## Static Showcase

`site` is a frontend-only showcase for GitHub Pages. It is deliberately separate from `MiniGPTCSharp.Web`:

- `site` sells and previews the project with static HTML, CSS, JavaScript, and assets.
- `MiniGPTCSharp.Web` runs the real local ASP.NET Core API-backed playground.
- `scripts/publish-pages.ps1` publishes `site` to the `gh-pages` branch for GitHub Pages.

This split keeps the public portfolio page free and easy to host while preserving the richer app experience for local demos or separate app hosting.

## Tests

`MiniGPTCSharp.Tests` uses xUnit so `dotnet test` discovers and reports tests in the standard .NET way.

- Golden generation tests lock down deterministic outputs for representative prompts.
- Prompt inspection tests check token, embedding, layer, prediction, and disabled-attention behavior.
- Export/report tests check JSON shape stability and standalone HTML report content.
- Web playground tests cover the service-level response shape without requiring a long-running server.
- PowerShell scripts provide additional end-to-end coverage of CLI dispatch, seeded generation, inspection output, and comparison commands.

## Scripts

The scripts are optimized for Windows teaching environments while still working from any clone location.

- `Use-LocalDotNetEnv.ps1` redirects .NET and NuGet state into `.dotnet` for machines with restricted user profiles.
- `smoke.ps1` builds the CLI and runs a short seeded generation.
- `test-all.ps1` runs clean, build, CLI regression checks, and `dotnet test`.
- `student-labs.ps1` runs the five shortest teaching demos.
- `student-walkthrough.ps1` runs a pause-driven instructor walkthrough and writes a transcript.
- `demo-learning.ps1` is a compact interactive demo script.
- `demo-report.ps1` generates `artifacts/demo-report.html` through the CLI report command.
- `publish-pages.ps1` publishes the static brochure site from `site` to the `gh-pages` branch.

## CI

GitHub Actions runs a clean Windows .NET 8 restore, build, xUnit test, and CLI smoke flow. CI uses direct `dotnet` commands because hosted runners do not need the repo-local environment helper.

## Showcase Layer

The repo includes a small portfolio layer around the implementation:

- `site` contains the static GitHub Pages showcase.
- `docs/showcase.md` explains the commercial value of explainable AI tooling.
- `docs/assets` stores README and release visuals.
- `scripts/capture-showcase-assets.ps1` starts the web playground for screenshot and GIF capture.
- `scripts/publish-pages.ps1` republishes the static brochure site.
- `ROADMAP.md` communicates where the demo can grow next.
- `SECURITY.md` makes the local-first, no-external-model-calls behavior explicit.

## Naming

The repository is branded MiniGPTSharp for readability. Project folders and namespaces use MiniGPTCSharp to preserve the original C# project identity and avoid ambiguity in tooling.
