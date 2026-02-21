# MiniGPTCSharp

MiniGPTCSharp is a small C# project that shows how a GPT style language model generates text one token at a time.
It is built for learning, teaching, and careful debugging through a simple command line interface.

## What this project does

This project runs a compact transformer model and lets you inspect each part of inference in plain English.
You can give a prompt, watch the model score possible next tokens, and see how one token is chosen at each step.

The CLI can show:

- Tokenisation details for the input prompt
- Forward pass behaviour at each generation step
- Logits before probabilities are calculated
- Probabilities after softmax
- Decoding decisions that select the next token

This helps answer a common question.
What actually happened inside the model when this output was produced?

## What this project is not

MiniGPTCSharp is not trying to compete with very large production models.
It is designed to make internal behaviour easy to inspect so people can understand how language models work.

If you want the highest quality writing, this is not the goal.
If you want to understand inference clearly and practically, this is the goal.

## Why outputs can change for the same prompt

Many people expect one prompt to always return one fixed answer.
That is only true in deterministic mode.

In sampling mode, the model treats probabilities as chances and can pick different tokens on different runs.
Even when the prompt is the same, a different random draw can change the next token.
A small change early in generation can lead to very different text later.

So different outputs do not always mean the model is broken.
Often they mean the decoding settings are allowing variation.

## Deterministic generation versus sampling

Deterministic generation always picks the highest probability token at each step.
This gives stable repeatable output, but it can be narrow and repetitive.

Sampling generation picks from a probability distribution.
This can produce more varied text, but it can also pick a less likely token and move in a surprising direction.

Both modes are useful.
Deterministic mode is useful for controlled tests.
Sampling mode is useful when you want to study variation and probability driven behaviour.

## Why decoding strategy is often mistaken for a model bug

In real systems, teams often report model bugs when behaviour changes between runs.
A large part of these reports come from misunderstanding decoding strategy.

If temperature, top k, seed, or deterministic settings differ, the output path can differ too.
The model weights may be unchanged, but the token choice process is not the same.

This project helps engineers separate true model problems from decoding configuration problems.

## Step mode and the token by token loop

Step mode shows generation one token at a time.
You can inspect each loop iteration, read candidate probabilities, and see the exact token that was selected.

This makes the generation process concrete.
Instead of treating the model as a black box, you can inspect each decision in sequence.

## Why you would use this in real life

MiniGPTCSharp is useful for teaching, debugging, onboarding, and interview demonstrations.
It gives a shared practical view of inference so teams can discuss behaviour with evidence instead of guesswork.

It is helpful when a candidate says they understand language models and you want to test depth.
It is helpful when a new engineer joins and needs to learn what logits, probabilities, and decoding actually mean in practice.
It is helpful when a production issue appears and you need to explain behaviour clearly to non specialists.

It is also useful when a model chooses a word that seems logically incorrect.
By inspecting token scores and decoding choices, you can show that the model followed its probability distribution rather than symbolic logic.
This often resolves confusion and points to the right fix, such as prompt changes, decoding changes, or model improvement work.

## Getting started

Run one of these scripts from the project root.

Demo learning script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\demo-learning.ps1
```

Student walkthrough script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\student-walkthrough.ps1
```

These scripts guide you through the core commands and help you see how inference decisions are made.
