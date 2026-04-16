# MiniGPTSharp

MiniGPTSharp is a tiny C#/.NET teaching project for students who want to understand the mechanics behind GPT-style next-token prediction without hiding the interesting parts behind large frameworks.

It aims for the middle ground between:

- theory-heavy AI explanations that never become runnable
- production-scale ML repos that hide the core ideas under layers of infrastructure

If you live in .NET and want a small, readable GPT-style language model in C#, this repo is designed for that.

This repo is deliberately small, inspectable, and a little opinionated:

- it teaches tokenization, logits, softmax, sampling, and attention
- it exposes model internals directly in the CLI
- it includes guided walkthroughs and labs, not just raw code
- it favors readability over realism or performance

It is a toy model, not a production model. That is the point.

## Why this repo is useful

Most AI repos make one of two tradeoffs:

- they are realistic, but too large for beginners to follow
- they are simple, but too shallow to build a strong mental model

MiniGPTSharp tries to land in the middle. Students can:

- generate text
- inspect tokens and embedding vectors
- inspect attention weights for the last token
- compare deterministic generation with seeded sampling
- compare the baseline model against ablations like `--no-attention`

That makes it a much better teaching tool than a plain "text in, text out" demo.

## What students should learn here

By the end of a session with this repo, a student should understand:

- text is converted into tokens, then token IDs
- the model works with numbers, not raw strings
- logits are scores for candidate next tokens
- softmax turns logits into probabilities
- deterministic generation follows argmax
- sampling chooses from the probability distribution
- attention changes which earlier tokens matter most
- architecture choices like attention, position, and layer norm affect behavior

## Quick Start

### 1) Use the repo-local .NET environment

This repo includes a PowerShell helper that redirects .NET and NuGet state into `C:\MiniGPT\.dotnet` so builds and tests do not depend on a writable user profile.

```powershell
. .\scripts\Use-LocalDotNetEnv.ps1
Initialize-LocalDotNetEnv -RepoRoot "C:\MiniGPT"
```

If you use the scripts in `.\scripts`, this setup is applied automatically.

### 2) Build and test

```powershell
Invoke-RepoDotNet -Arguments @("build", ".\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj", "-c", "Release")
Invoke-RepoDotNet -Arguments @("test", ".\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj", "-c", "Release", "--no-build", "--no-restore")
```

### 3) Try the CLI

```powershell
$cli = ".\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"

dotnet run -c Release --project $cli -- --help
dotnet run -c Release --project $cli -- predict --prompt "The capital of France is" --topn 5
dotnet run -c Release --project $cli -- step --prompt "The capital of France is" --tokens 3 --seed 42 --explain
dotnet run -c Release --project $cli -- inspect pipeline --prompt "The capital of France is"
dotnet run -c Release --project $cli -- compare sampling --prompt "The capital of France is" --tokens 8
```

## Best Ways To Use This Repo

### Guided walkthrough

For a teacher-led or self-paced walkthrough:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-walkthrough.ps1
```

This pauses between sections and writes a transcript to `walkthrough-output.txt`.

### Student lab pack

For a shorter "show me the core ideas" lab sequence:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-labs.ps1
```

This runs five focused labs:

- tokenization
- embeddings
- attention
- sampling comparison
- architecture ablation

### Full regression/smoke checks

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-all.ps1
```

## CLI Commands

### `predict`

Shows the model's next-token belief distribution.

```powershell
dotnet run -c Release --project $cli -- predict --prompt "The capital of France is" --topn 5 --explain
```

Use this to teach:

- logits vs probabilities
- top-N candidates
- why "most likely next token" is not the same as "true answer"

### `generate`

Runs the normal autoregressive loop.

```powershell
dotnet run -c Release --project $cli -- generate --prompt "Hello" --tokens 8 --seed 42
dotnet run -c Release --project $cli -- generate --prompt "Hello" --tokens 8 --deterministic
```

Use this to teach:

- deterministic argmax vs sampling
- reproducibility with seeds
- how repeated next-token choice builds a sequence

### `step`

Shows generation one token at a time.

```powershell
dotnet run -c Release --project $cli -- step --prompt "The capital of France is" --tokens 3 --seed 42 --explain --show-logits
```

Use this to teach:

- forward pass
- logits
- softmax
- chosen token
- append and repeat

### `inspect`

The most useful upgrade in this repo. It exposes internals students normally only hear described.

#### `inspect tokens`

```powershell
dotnet run -c Release --project $cli -- inspect tokens --prompt "The capital of France is Paris."
```

Shows:

- token positions
- token texts
- token IDs
- whether a token came from the seeded toy vocabulary or was added at runtime

#### `inspect embeddings`

```powershell
dotnet run -c Release --project $cli -- inspect embeddings --prompt "AI model learning" --layers 0 --dims 8
```

Shows:

- token table
- embedding vector previews

Good for teaching:

- embeddings are numeric representations
- different tokens map to different vector patterns

#### `inspect attention`

```powershell
dotnet run -c Release --project $cli -- inspect attention --prompt "The capital of France is Paris" --attention-topn 5
```

Shows:

- which earlier tokens the final token is attending to
- the top attention targets for each layer

Good for teaching:

- attention is not magic
- the model weights previous tokens differently
- different layers can focus differently

#### `inspect pipeline`

```powershell
dotnet run -c Release --project $cli -- inspect pipeline --prompt "The capital of France is" --dims 6 --attention-topn 5
```

Shows all of the above in one report:

- token table
- embedding preview
- attention summary
- top next-token predictions

This is the best single command to show a student who asks, "What is the model doing right now?"

### `compare`

The second big upgrade in this repo. It lets students compare behaviors instead of memorizing definitions.

#### `compare sampling`

```powershell
dotnet run -c Release --project $cli -- compare sampling --prompt "The capital of France is" --tokens 8
```

Shows side by side:

- top next-token beliefs
- deterministic argmax continuation
- seeded sampling with one seed
- seeded sampling with another seed

Good for teaching:

- `predict` is belief, not choice
- argmax is one path through the distribution
- different seeds produce different sampled paths

#### `compare ablation`

```powershell
dotnet run -c Release --project $cli -- compare ablation --prompt "The capital of France is" --tokens 8
```

Shows side by side:

- baseline model
- no attention
- no position embeddings
- no layer norm

Good for teaching:

- architecture decisions change behavior
- removing a component is a useful learning tool
- "what each part does" becomes visible through comparison

### `learn`

`learn` is a guided entry point that runs curated topics.

```powershell
dotnet run -c Release --project $cli -- learn tokenization
dotnet run -c Release --project $cli -- learn embeddings
dotnet run -c Release --project $cli -- learn attention
dotnet run -c Release --project $cli -- learn sampling
dotnet run -c Release --project $cli -- learn ablation
```

This is useful when students are unsure which command to start with.

## Recommended Teaching Flow

If you have 10-15 minutes:

1. `predict`
2. `step --explain`
3. `compare sampling`

If you have 20-30 minutes:

1. `inspect tokens`
2. `inspect embeddings`
3. `inspect attention`
4. `compare sampling`
5. `compare ablation`

If you are teaching a full beginner session:

1. run `student-walkthrough.ps1`
2. stop after each section and ask what changed
3. use `inspect pipeline` on a student-chosen prompt
4. use `compare ablation` to show why architecture matters

## Mental Models That Work Well

### Predict vs generate

- `predict` = what the model currently believes
- `generate` = how the system chooses from that belief repeatedly

### GPT as autocomplete

The most useful beginner mental model is still:

> GPT is a probability-based next-token autocomplete loop.

That is not the whole story for large real models, but it is the right starting point.

### Why the output can feel "wrong"

If the repo predicts `capital` over `Paris`, that is a teaching opportunity, not a bug.

It shows:

- the model is ranking token continuations
- token probability is not the same as factual reasoning
- output depends on learned patterns in this toy system

## Project Structure

- [`MiniGPTCSharp`](/C:/MiniGPT/MiniGPTCSharp) contains the toy model, tokenizer, tensor wrapper, and inspection types
- [`MiniGPTCSharp.Cli`](/C:/MiniGPT/MiniGPTCSharp.Cli) contains the learning CLI
- [`MiniGPTCSharp.Tests`](/C:/MiniGPT/MiniGPTCSharp.Tests) contains golden tests and inspection tests
- [`scripts`](/C:/MiniGPT/scripts) contains walkthrough, lab, smoke, and test scripts

## What This Repo Is Not

This repo is not:

- a production LLM runtime
- a realistic training stack
- a benchmark of modern model quality
- a replacement for PyTorch-scale transformer implementations

It is a teaching tool.

That means a good change in this repo is one that makes the internals easier to see, compare, and explain.

## Testing

The test project checks:

- deterministic golden outputs
- prompt inspection structure
- disabled-attention identity behavior

Run:

```powershell
Invoke-RepoDotNet -Arguments @("test", ".\MiniGPTCSharp.Tests\MiniGPTCSharp.Tests.csproj", "-c", "Release")
```

## Suggested Next Improvements

If you want to keep evolving this repo as a teaching tool, the next high-value ideas are:

- add a tiny attention heatmap export for markdown or HTML
- add a "teacher notes" document with suggested discussion prompts
- add a few fixed classroom exercises with expected observations
- add a `compare prompt-a prompt-b` mode to show context sensitivity
- add a small web UI on top of the existing inspection APIs

Those would deepen the teaching experience without making the core model much bigger.
