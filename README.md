# MiniGPTSharp

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
  --max-new-tokens 100 \
  --temperature 0.8 \
  --top-k 40
```

### 4) Step Mode (one token at a time)

Step Mode runs the same autocomplete loop, but exposes each next-token choice so you can inspect it.

```bash
dotnet run --project MiniGPTCSharp.Cli -- \
  --prompt "The capital of France is" \
  --max-new-tokens 5 \
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

---

## Final notes

MiniGPTSharp is for learning, not production.

If you treat the code like an educational "transparent box" and inspect each step, you will build a practical mental model of how GPT-style models generate text.
