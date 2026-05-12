# 30-Minute Teaching Guide

This guide is for a short live session where students already know basic programming but are new to GPT-style text generation. Keep the tone practical: every concept should be tied to a command they can run.

## Setup

Prerequisites:

- .NET 8 SDK
- PowerShell
- A local clone of this repo

Suggested preflight:

```powershell
dotnet test .\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj -c Release
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- --help
```

If the machine has a locked-down user profile, initialize the repo-local .NET environment first:

```powershell
. .\scripts\Use-LocalDotNetEnv.ps1
Initialize-LocalDotNetEnv
```

## Flow

### 0-3 minutes: Set expectations

Say up front:

- This is a toy model for learning, not a production LLM.
- The goal is to see the loop: tokenize, score, choose, append, repeat.
- Weird output is useful because it exposes the mechanism.

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- --help
```

### 3-8 minutes: Predict is belief, not text generation

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- predict --prompt "The capital of France is" --topn 5 --explain
```

Ask:

- What did the model receive as tokens?
- Which token is highest probability?
- Why is a likely next token not the same as a factual answer?

Key point: `predict` reports a distribution. It does not append anything.

### 8-13 minutes: Generate repeats the next-token loop

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- generate --prompt "The capital of France is" --tokens 6 --deterministic --explain
```

Ask:

- What changes after each chosen token is appended?
- Why is deterministic generation repeatable?
- Where do logits become probabilities?

Key point: GPT-style generation is repeated next-token prediction.

### 13-18 minutes: Sampling changes the path

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- compare sampling --prompt "The capital of France is" --tokens 8
```

Ask:

- How does argmax differ from seeded sampling?
- What does the seed control?
- Why can lower-probability tokens still appear?

Key point: sampling chooses from the distribution instead of always taking the top token.

### 18-24 minutes: Inspect the internals

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- inspect pipeline --prompt "The capital of France is" --dims 6 --attention-topn 5
```

Ask:

- Which tokens came from the seed vocabulary?
- What do embeddings show that raw text cannot?
- Which previous tokens receive attention from the last token?

Key point: model internals can be inspected as data, not just described in slides.

### 24-28 minutes: Break the model on purpose

Command:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj -- compare ablation --prompt "The capital of France is" --tokens 8
```

Ask:

- Which ablation changed the output most?
- What does attention appear to contribute?
- Why is removing a component a useful learning technique?

Key point: architecture choices affect behavior, even in a toy model.

### 28-30 minutes: Wrap up

Reinforce:

- Text becomes token IDs.
- The model scores candidate next tokens with logits.
- Softmax turns logits into probabilities.
- Generation chooses one token, appends it, and repeats.
- Attention changes how previous context is weighted.

Next command for self-study:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-labs.ps1
```

Optional portfolio artifact:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\demo-report.ps1
```

Optional visual follow-up:

```powershell
dotnet run -c Release --project .\MiniGPTCSharp.Web\MiniGPTCSharp.Web.csproj --urls http://localhost:5088
```

## Instructor Tips

- Let students choose one prompt for `inspect pipeline`; ownership makes the output more memorable.
- Keep the "toy model" caveat visible so students do not overinterpret quality.
- When output looks wrong, ask which part of the loop produced it instead of treating it as a failure.
- Use `student-walkthrough.ps1` for a guided session with pauses and transcript output.
