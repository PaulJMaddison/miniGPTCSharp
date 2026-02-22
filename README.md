# MiniGPTSharp

MiniGPTSharp is a **small, readable GPT-style language model in C#** built for people who want to understand how text generation works—without wading through massive ML frameworks.

## Why this project exists

Most AI repos are either:
- too abstract (lots of theory, not enough runnable code), or
- too complex (production-scale stacks that hide the core ideas).

MiniGPTSharp is the middle ground: a practical learning project where you can inspect tokenization, logits, softmax, and sampling behavior from a simple CLI.

## Why use MiniGPTSharp

- **Learn by running real commands**: `predict`, `step`, and `generate` expose how next-token selection actually happens.
- **C#-first experience**: ideal if you live in .NET and want AI concepts in familiar language and tooling.
- **Transparent internals**: model components are intentionally approachable so you can read and modify them.
- **Great for teaching and demos**: seeded runs and deterministic mode make lessons reproducible.
- **Fast onboarding**: beginner-friendly walkthrough scripts help students and teams get productive quickly.

## Who it's for

- **C# and .NET developers** curious about LLM fundamentals.
- **Students and educators** teaching probability-driven generation.
- **Engineering teams** that want a lightweight internal demo for AI onboarding.
- **Hackers and tinkerers** who prefer understanding the mechanics over black-box usage.

## What you can do with it

- Explain token prediction and probability distributions in live workshops.
- Compare deterministic argmax decoding vs seeded sampling.
- Demonstrate how temperature/top-k alter output behavior.
- Create classroom labs and internal training material using the included scripts.


## Student walkthrough script

Students can run a guided walkthrough that prints live output and saves the full session to `walkthrough-output.txt`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-walkthrough.ps1
```

By default, the script writes to `C:\MiniGPT\walkthrough-output.txt`. You can override paths with parameters like `-RepoRoot`, `-CliProj`, and `-OutputPath`.

## Learning Walkthrough (5 minutes)

This mini-lab is for beginner C# developers who want to see how GPT-style generation works with real CLI output.

- **Tokens** are small text pieces (words, punctuation, or sub-word chunks) that the model uses instead of raw strings.
- **Logits** are raw scores the model gives each possible next token before probabilities are calculated.
- **Softmax** converts logits into probabilities that add up to 100%.
- **Temperature** changes randomness (lower = safer/more focused, higher = more varied).
- **Top-k** limits sampling to only the K highest-scoring tokens.

> ✅ Key idea:
> This callout highlights the core concept for a section.
>
> 👀 What to notice:
> This points out the exact behavior to watch in command output.
>
> ⚠️ Common confusion:
> This flags beginner traps that are easy to misread the first time.
>
> 🧪 Try it:
> This gives a small command-driven check you can run immediately.
>
> 🛠️ If your output differs:
> Use this to quickly troubleshoot setup or expectation mismatches.

These callouts appear throughout the walkthrough to make key learning moments easier to spot while you run each command.

### 1) Set a CLI path variable

```powershell
$cli = "MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
```

What to look for:
- You can reuse `$cli` in all following commands.
- Commands stay short and easier to read.

Concept: **repeatable command setup** for quick experiments.

> ✅ Key idea:
> Set `$cli` once, then reuse it everywhere.
> This keeps experiments repeatable and reduces copy/paste mistakes.

### 2) Run CLI help

```powershell
dotnet run -c Release --project $cli -- --help
```

What to look for:
- `generate`, `step`, and `predict` commands.
- Sampling options like `--temperature` and `--top-k`.

Concept: **what controls generation behavior**.

> 👀 What to notice:
> `predict` shows probabilities only.
> `generate` and `step` are where token selection and randomness happen.

### 3) Run `predict`

```powershell
dotnet run -c Release --project $cli -- predict --prompt "The capital of France is" --topn 5
```

Example output (your probabilities may differ):

```text
1. capital   p=0.34
2. Paris     p=0.21
3. city      p=0.11
4. the       p=0.08
5. and       p=0.05
```

What to look for:
- Top next-token candidates.
- Each candidate's probability.

Concept: **the model predicts a probability distribution, not one fixed token**.

> ✅ Key idea:
> `predict` shows the model's belief distribution over next tokens.
> It does not choose or generate a token.

> ⚠️ Common confusion:
> `predict` does not use randomness; it reports probabilities from one forward pass.
> It is showing belief distribution, not facts or logical reasoning.
> Think IntelliSense/autocomplete: likely suggestions from patterns, not guaranteed truth.

## Why isn't 'Paris' the top prediction?

Great question. This is one of the most important lessons in this repo.

When you run `predict`, the model is **not** doing logic like "France -> capital -> Paris." It is also **not** looking up a fact from a database.

Instead, it does this:

1. Runs a forward pass over the prompt tokens.
2. Produces one score per possible next token (**logits**).
3. Converts those scores into probabilities with softmax.
4. Shows the top-N highest probabilities.

Logits are the raw scores the model outputs before turning them into probabilities.

If "capital" appears above "Paris," that is not a bug. It just means the model gave "capital" a higher next-token probability in that exact context.

Think of it like IntelliSense/autocomplete in C#: suggestions are based on learned patterns, not on what is universally true.

Also important: `predict` does **not** sample. There is no randomness there. Seed, temperature, and top-k do not apply to `predict` output.

> 👀 What to notice:
> If `capital` ranks above `Paris`, read that as a pattern signal from training.
> Treat rankings as "what this model expects next," not "what is objectively correct."

> [!TIP]
> ### Mental model
> - **Predict = what the model believes** (top probabilities)
> - **Generate/Step = how we pick from that belief** (argmax vs random weighted pick)

| Command | Random? | What it teaches |
| --- | --- | --- |
| `predict` | No | model belief distribution |
| `generate --deterministic` | No | argmax (always pick top token) |
| `generate` / `step` with `--seed` | Yes | sampling from probabilities |

### Student exercise: see the difference yourself

Use these exact commands:

```powershell
$cli="C:\MiniGPT\MiniGPTCSharp.Cli\MiniGPTCSharp.Cli.csproj"
dotnet run -c Release --project $cli -- predict --prompt "The capital of France is" --topn 5
```

> 👀 What to notice:
> The ranking can feel wrong, but it reflects training patterns in this model.

```powershell
dotnet run -c Release --project $cli -- generate --prompt "The capital of France is" --tokens 10 --deterministic
```

> 👀 What to notice:
> Deterministic mode always picks the #1 token at each step (argmax).

```powershell
dotnet run -c Release --project $cli -- step --prompt "The capital of France is" --tokens 5 --seed 42 --explain
```

> 👀 What to notice:
> Seeded sampling can pick non-#1 tokens.
> With the same seed, that sampled path stays reproducible.

> 🛠️ If your output differs:
> Use `-c Release` for all runs.
> Verify `--deterministic` and `--seed` flags match the examples.
> Different model weights or training data can change token rankings.

### So how do we make Paris #1?

To truly change the ranking, you must change the **model**, not the random number settings.

- Train more on high-quality factual examples.
- Fine-tune so weights shift toward better factual next-token rankings.

You can also add a **logit bias** as a teaching demo (a controlled "cheat"), but that is not the same as the model actually learning better weights.

### 4) Run step mode with explanations (probabilities)

```powershell
dotnet run -c Release --project $cli -- step --prompt "The capital of France is" --tokens 3 --seed 42 --explain
```

What to look for:
- One generation step at a time.
- Context length and sampling settings.
- Candidate list with `logit=` and `p=`.

Concept: **generation is a loop of next-token decisions**.

> 🧪 Try it:
> Re-run this command with `--seed 42` twice.
> Then change to a different seed and compare token choices.

### 5) Run step mode with logits + probabilities

```powershell
dotnet run -c Release --project $cli -- step --prompt "The capital of France is" --tokens 3 --seed 42 --explain --show-logits --logits-topn 5 --logits-format centered
```

What to look for:
- `Logits (pre-softmax)` section.
- The same candidate tokens shown with transformed logits.
- The note that centered logits use `logit - max_logit`.

Concept: **softmax starts from logits, and formatting helps you see score relationships**.

### 6) Compare deterministic vs seeded sampling

```powershell
dotnet run -c Release --project $cli -- --prompt "Hello" --tokens 6 --deterministic
dotnet run -c Release --project $cli -- --prompt "Hello" --tokens 6 --seed 42
dotnet run -c Release --project $cli -- --prompt "Hello" --tokens 6 --seed 42
```

What to look for:
- Deterministic mode always picks argmax.
- Same seed gives the same output again.
- Different seeds can produce different continuations.

Concept: **randomness is controllable for learning and testing**.

### 7) Optional: run tests

```powershell
dotnet test -c Release
```

What to look for:
- Green test output.
- Confidence that changes did not break core behavior.

Concept: **verification and regression safety**.

## What is an AI Model (in simple terms)?

If you're a C# developer, the easiest way to think about a base AI model is:

> A probability-based pattern generator that predicts the next token in a sequence based on learned relationships between tokens.

### What it is **not**

A base model is **not**:

- thinking
- deciding
- planning
- understanding
- storing knowledge like a database
- remembering documents or conversations

It does not keep a hidden folder of full docs, chats, or source files.

### What it **does** contain

A trained model contains:

- a vocabulary of tokens (words, code pieces, punctuation, symbols)
- learned statistical patterns from training data
- a very large set of internal numbers called **weights**

You can think of weights as configuration values that capture how often certain tokens appear together, similar to how autocomplete learns common method chains.

### Training (simple view)

During training, the model sees many examples of token sequences and keeps adjusting its internal values so it gets better at predicting what usually comes next.

After training, it does **not** store:

- sentences
- documents
- conversations
- the original training dataset

It stores only learned patterns in its weights.

### What happens at runtime

When you send input text, the model:

1. splits the input into tokens
2. calculates which token is most likely to come next
3. appends that token to the sequence
4. repeats

This is similar to IntelliSense repeatedly predicting the next method or variable name.

### Why outputs can look "smart"

A base model alone:

- has no goals
- does not plan
- cannot check its own work
- cannot decide what to do next
- cannot pursue outcomes

It only generates likely continuations of input.

Most real-world "intelligent behavior" comes from software around the model, such as:

- reinforcement learning from human feedback (RLHF)
- agent frameworks
- tool-use systems
- orchestration logic
- evaluation and retry loops

These are software systems built around the model that interpret its output and decide what actions to take.

MiniGPTSharp is a **C#/.NET educational re-implementation** of Andrej Karpathy's Python project, [minGPT](https://github.com/karpathy/minGPT).

This project is built with **TorchSharp** and is designed to help C# developers understand how GPT-style models work internally.

> [!IMPORTANT]
> MiniGPTSharp is **not** production AI infrastructure.
> It is **not** optimized.
> It is **not** intended for real-world AI workloads.
>
> It is intentionally written for:
> - readability
> - learning
> - exploration
> - understanding what GPT is doing under the hood

---

## Project Overview

Think of this repo as a "readable engine diagram" for GPT in C#.

Instead of hiding details behind big frameworks and production abstractions, MiniGPTSharp keeps things simple so you can inspect each step:

1. Convert text into token IDs
2. Convert IDs into numeric vectors
3. Pass vectors through transformer blocks
4. Produce scores for possible next tokens
5. Pick one token
6. Repeat (autocomplete loop)

---

## Why this project exists

If you are a C# developer, most GPT examples are in Python and assume AI background knowledge.

MiniGPTSharp exists to make GPT internals accessible using familiar .NET patterns:
- classes with clear responsibilities
- config objects
- pipeline-style processing
- step-by-step flow you can debug

The goal is not to "use AI quickly." The goal is to **understand AI internals clearly**.

---

## Relationship to Andrej Karpathy's minGPT

Andrej Karpathy's minGPT is a small, educational PyTorch implementation of GPT.

MiniGPTSharp follows the same learning-first spirit, but ports the architecture into C#/.NET with TorchSharp so C# developers can study the same ideas in a familiar ecosystem.

---

## What is GPT? (simple explanation)

A GPT model is basically a very advanced autocomplete system.

Given a sequence of tokens, it predicts the most likely next token, then does it again, and again.

Important mental model:
- GPT does **not** understand language like a human.
- GPT does **not** think.
- GPT does **not** reason.
- GPT does **not** "know" facts in a human sense.
- GPT predicts likely next tokens based on patterns seen during training.

Think IntelliSense: it does not understand your business domain, but it can still predict likely next method calls.

---

## What is a Transformer? (no math)

A transformer is a **pipeline of processing steps**, similar to middleware.

Each layer takes the current token information, refines it, and passes it to the next layer.

You can picture it like:

`input -> layer1 -> layer2 -> layer3 -> output`

Each layer helps the model decide what context matters for the next-token prediction.

---

## What is Tokenization?

Tokenization is splitting text into smaller units before processing.

C# analogy:
- Like parsing a string into an array/list of values before running logic.
- But instead of strings, the model uses integer IDs.

Example idea:
- Text: `"Hello world"`
- Tokens: `["Hello", " world"]`
- Token IDs: `[15496, 995]`

---

## What are Embeddings?

Embeddings map each token ID to a numeric vector.

C# analogy:
- Similar to a lookup table: `Dictionary<int, float[]>`
- Input: token ID
- Output: vector that carries "meaning-like" signal used by later layers

You can think of it as converting discrete IDs into a richer numeric representation that the model can process.

---

## What is Self-Attention?

Self-attention is the model deciding which earlier tokens are most important when predicting the next token.

C# analogy:
- You have a `List<int>` of previous tokens.
- For the current position, the model scores which earlier items matter most.
- It then builds a context-aware representation using those scores.

So instead of treating all previous tokens equally, it focuses more on relevant ones.

---

## What is Causal Attention?

Causal attention means: **a token can only look backward, never forward**.

Why:
- During generation, future tokens do not exist yet.
- So prediction must use only current and previous tokens.

C# analogy:
- Processing a list with a rule: index `i` can only read `0..i`, never `i+1..end`.

---

## How text generation works (step-by-step pipeline)

Think of generation as a C# data-processing pipeline:

`Prompt`
`-> Tokenizer`
`-> Token IDs`
`-> Embeddings`
`-> Transformer Blocks`
`-> Logits`
`-> Sampling`
`-> Next Token`
`-> Loop`

### 1) Prompt
You provide initial text, e.g. `"Once upon a time"`.

### 2) Tokenizer
The tokenizer splits text into tokens and maps them to integer IDs.

### 3) Token IDs
The model works on token IDs, not raw strings.

### 4) Embeddings
Each ID becomes a vector from an embedding table.

### 5) Transformer Blocks
A stack of blocks refines token representations using attention + feed-forward steps.
Think: middleware pipeline for sequence data.

### 6) Logits
The model outputs a score for every possible next token.

### 7) Sampling
A token is chosen from the scores.
- **Temperature**: randomness level (like tuning how wide `Random.Next()` choices feel)
- **Top-K**: only allow selection from the K highest-scoring tokens

### 8) Next Token
Append the chosen token to the sequence.

### 9) Loop
Repeat until max length or stop condition.

### Reproducible Results

By default, GPT-style text generation uses randomness, so the same prompt can produce different outputs.

MiniGPTSharp now supports two teaching-friendly options:

- `--seed 42`: keeps sampling behavior, but makes it repeatable.
  - Same prompt + same model + same settings + same seed = same output.
- `--deterministic`: turns off sampling and always picks the most likely next token (greedy ArgMax).
  - This removes randomness completely.

Use deterministic mode when you want stable, easy-to-explain walkthroughs in class or documentation.

---

## Mental model: "How the model predicts the next word"

Imagine this loop:

1. Read everything generated so far.
2. Decide which earlier tokens matter most right now.
3. Build a best guess for what token should come next.
4. Pick one candidate (more deterministic or more random depending on settings).
5. Append and continue.

That is the core behavior of GPT: repeated next-token prediction.

---

## High-level code walkthrough

Below is the typical responsibility of key classes:

### `BpeTokenizer`
- Loads vocabulary/merge rules.
- Encodes text into token IDs.
- Decodes token IDs back to text.

Think: parser + encoder/decoder component.

### `GptConfig`
- Holds model and generation settings (sizes, layer counts, limits, etc.).

Think: strongly-typed options class.

### `GptModel`
- Main model class.
- Wires embeddings, transformer stack, and output projection.
- Runs forward pass to produce logits.

Think: orchestrator for the core pipeline.

### `TransformerBlock`
- One processing stage in the stack.
- Applies attention and feed-forward transformations.

Think: one middleware step in a repeated sequence pipeline.

### `Generate()`
- Implements the autoregressive loop.
- Repeatedly gets logits, samples next token, appends, and continues.

Think: an autocomplete `while` loop over token sequences.

---

## If you are new to AI, start here

Use this order when reading the codebase:

1. `GptConfig` (understand knobs and sizes)
2. `BpeTokenizer` (understand text <-> IDs)
3. `TransformerBlock` (understand one block)
4. `GptModel` (see how blocks are wired together)
5. `Generate()` flow (understand inference loop end-to-end)

Tip: step through generation with a debugger and inspect token IDs at each iteration.

---

## How to Run

> The exact command names may vary slightly by folder layout. Use this as the conceptual flow.

### 1) Export `state_dict` from Python (minGPT/PyTorch side)

```bash
python export_state_dict.py \
  --checkpoint path/to/model.pt \
  --out path/to/model_state_dict.pt
```

### 2) Load exported weights in MiniGPTSharp CLI

```bash
dotnet run --project src/MiniGPTSharp.CLI -- \
  --model path/to/model_state_dict.pt \
  --tokenizer path/to/tokenizer \
  --prompt "Once upon a time"
```

### 3) Run generation with sampling controls

```bash
dotnet run --project src/MiniGPTSharp.CLI -- \
  --model path/to/model_state_dict.pt \
  --tokenizer path/to/tokenizer \
  --prompt "The C# developer opened the editor and" \
  --tokens 100 \
  --temperature 0.8 \
  --top-k 40
```

### 4) Step Mode (one token at a time)

Step Mode runs the same autocomplete loop, but exposes each next-token choice so you can inspect it.

```bash
dotnet run --project MiniGPTCSharp.Cli -- \
  --prompt "The capital of France is" \
  --tokens 5 \
  --temperature 0.8 \
  --top-k 5 \
  --step \
  --explain
```

With `--step`, the CLI calls `Step(...)` repeatedly, appends one token each loop, and prints the growing decoded text.

### 5) Predict Mode (next token only)

Predict mode prints what the model thinks should come next for the current prompt. It runs one forward pass and shows the highest-probability next tokens without generating a full sentence. This is autocomplete over tokens, not a facts database.

```bash
dotnet run --project MiniGPTCSharp.Cli -- \
  predict --prompt "The capital of France is" --topn 10 --temp 1.0 --topk 0
```

## Performance Notes

MiniGPTSharp is a learning project first, not a performance-tuned runtime.
TorchSharp is a .NET binding over LibTorch (the native backend used by PyTorch), so tensor-heavy math executes in native code.
That said, end-to-end speed can still differ from Python projects for reasons outside core kernels.
- Python tooling may include optimization paths (for example JIT/compile workflows) that are different or unavailable in TorchSharp.
- Interop boundaries, allocations, data loading, and many small operations can add noticeable overhead in managed apps.
- Throughput/latency also depends heavily on your model size, prompt shape, hardware, and runtime settings.

If you care about performance, benchmark on your machine and treat results as workload-dependent.

---

## Final notes

MiniGPTSharp is for learning, not production.

If you treat the code like an educational "transparent box" and inspect each step, you will build a practical mental model of how GPT-style models generate text.

## Golden Tests

The `MiniGPTCSharp.Tests` project includes golden tests for deterministic generation.

- They run `Generate()` with a fixed prompt, seed, token count, and `deterministic=true`.
- They compare the full generated string against a locked expected output.
- If a golden test fails after a code change, generation behavior has changed and the learning demo may have regressed.
