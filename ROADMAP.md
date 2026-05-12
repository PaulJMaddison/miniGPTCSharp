# Roadmap

MiniGPTSharp is already useful as a teaching and portfolio demo. The next improvements should make it more visual, more shareable, and more clearly product-shaped.

## Near Term

- Publish the static GitHub Pages showcase and link it from the repository About section.
- Add captured screenshots for the README: desktop playground, mobile playground, generated report.
- Add a short animated demo GIF showing prompt inspection, probability updates, and report export.
- Add example prompt presets for repeatable demos.
- Add a `compare prompts` mode to show context sensitivity across two prompts.
- Add a small gallery of generated HTML reports under `artifacts/examples` or release assets.

## Product Polish

- Improve the GPT Microscope attention heatmap with richer hover and selection behavior.
- Add a generation replay control for stepping backward and forward through chosen tokens.
- Add a fork-from-step workflow: change seed or temperature at a previous step and branch generation.
- Add downloadable teaching packs with prompt, report, and discussion notes.
- Add a public hosted demo if deployment is appropriate.

## Engineering Depth

- Package the CLI as a local/global .NET tool.
- Add benchmark coverage for generation, inspection, report building, and web service response times.
- Add snapshot tests for key CLI text outputs and generated HTML report sections.
- Add an OpenAPI description for the web API.
- Add Docker or dev container support for consistent workshop environments.

## Teaching Depth

- Add teacher notes with expected observations and common misconceptions.
- Add a tiny training/fine-tuning demonstration so learners can change model behavior.
- Add a glossary page for logits, softmax, sampling, attention, ablation, and embeddings.
- Add a guided "debug this model" exercise where students diagnose why outputs change.

## Release Ideas

- `v1.1`: GPT Microscope, report generation, JSON exports, CI, docs polish.
- `v1.2`: screenshots, GIF, prompt presets, hosted demo, roadmap-driven README refresh.
- `v1.3`: generation replay, prompt comparison, richer attention interactions.
