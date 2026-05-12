# Architecture

MiniGPTSharp is intentionally small: one library, one CLI, one test project, and a few PowerShell scripts. The design goal is to make GPT-style mechanics visible enough for teaching while keeping the repo credible as a .NET portfolio sample.

## Solution Layout

```text
MiniGPTCSharp/
  MiniGPTCSharp.csproj          Core toy model library
MiniGPTCSharp.Cli/
  MiniGPTCSharp.Cli.csproj      Teaching-oriented command-line interface
MiniGPTCSharp.Tests/
  MiniGPTCSharp.Tests.csproj    xUnit regression tests
scripts/
  *.ps1                         Local setup, smoke tests, labs, walkthroughs
docs/
  architecture.md               This overview
  teaching-guide.md             30-minute workshop plan
.github/workflows/
  dotnet-ci.yml                 Windows .NET build/test workflow
```

## Core Library

`MiniGPTCSharp` contains the model and the inspection types used by the CLI.

- `MiniGptModel` orchestrates tokenization, embeddings, transformer blocks, logits, sampling, generation, and prompt inspection.
- `VocabularyTokenizer` owns the tiny seed vocabulary and adds unknown prompt tokens at runtime so learners can see token IDs change.
- `Tensor`, `SelfAttention`, and `TransformerBlock` keep the model math visible without pulling in a machine-learning framework.
- `GptConfig` exposes teaching toggles such as layer count, top-k, temperature, disabled attention, disabled position embeddings, and disabled layer normalization.
- `PromptInspection`, `NextTokenPrediction`, and `GenerationStepResult` are small DTOs that let the CLI print internals without duplicating model logic.

The library favors deterministic, inspectable behavior over realism. It is not a production inference runtime.

## CLI

`MiniGPTCSharp.Cli` is the teaching surface. It keeps all features available through subcommands rather than requiring users to edit code.

- `predict` shows next-token probabilities.
- `generate` runs the autoregressive loop.
- `step` shows generation one token at a time.
- `inspect` exposes tokens, embeddings, attention, or the full pipeline.
- `compare sampling` contrasts probabilities, argmax, and seeded sampling.
- `compare ablation` shows how output changes when parts of the toy architecture are disabled.
- `learn` gives students curated entry points for common topics.

The CLI is intentionally verbose in explanation modes because its primary job is to make the mental model observable.

## Tests

`MiniGPTCSharp.Tests` uses xUnit so `dotnet test` discovers and reports tests in the standard .NET way.

- Golden generation tests lock down deterministic outputs for representative prompts.
- Prompt inspection tests check token, embedding, layer, prediction, and disabled-attention behavior.
- PowerShell scripts provide additional end-to-end coverage of CLI dispatch, seeded generation, inspection output, and comparison commands.

## Scripts

The scripts are optimized for Windows teaching environments while still working from any clone location.

- `Use-LocalDotNetEnv.ps1` redirects .NET and NuGet state into `.dotnet` for machines with restricted user profiles.
- `smoke.ps1` builds the CLI and runs a short seeded generation.
- `test-all.ps1` runs clean, build, CLI regression checks, and `dotnet test`.
- `student-labs.ps1` runs the five shortest teaching demos.
- `student-walkthrough.ps1` runs a pause-driven instructor walkthrough and writes a transcript.
- `demo-learning.ps1` is a compact interactive demo script.

## CI

GitHub Actions runs a clean Windows .NET 8 restore, build, and xUnit test flow. CI uses direct `dotnet` commands because hosted runners do not need the repo-local environment helper.

## Naming

The repository is branded MiniGPTSharp for readability. Project folders and namespaces use MiniGPTCSharp to preserve the original C# project identity and avoid ambiguity in tooling.
