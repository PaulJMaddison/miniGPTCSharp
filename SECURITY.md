# Security

MiniGPTSharp is a local teaching and portfolio demo. It does not call external model APIs and it does not send prompts to a third-party service.

## Local-First Behavior

- The toy model runs inside this repo.
- CLI prompts are processed locally.
- GPT Microscope web requests are handled by the local ASP.NET Core app.
- Generated reports are written to local files.
- No cloud inference provider is contacted by the model code.

This makes the project suitable for workshops, demos, and internal evaluations where repeatability and prompt privacy matter.

## Scope

MiniGPTSharp is not production AI infrastructure. It is not designed to process untrusted internet traffic, store user accounts, or protect sensitive production data.

Treat it as:

- an educational model implementation
- a local explainability demo
- a portfolio-quality sample application

Do not treat it as:

- a hardened public SaaS product
- a production inference endpoint
- a secure document-processing pipeline

## Reporting Issues

If you find a security issue in this repository, please avoid posting sensitive details publicly. Open a private advisory or contact the repository owner through the normal GitHub profile channels.

For ordinary bugs, documentation issues, and teaching-flow problems, a public GitHub issue is appropriate.
