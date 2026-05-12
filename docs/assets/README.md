# Showcase Assets

This folder is for visual proof used by the README, releases, and client-facing demos.

Recommended assets:

- `gpt-microscope-desktop.png`: desktop playground screenshot
- `gpt-microscope-mobile.png`: mobile playground screenshot
- `gpt-report-preview.png`: generated HTML report screenshot
- `gpt-microscope-demo.gif`: short interaction GIF
- `gpt-microscope-showcase.svg`: static branded visual for README and docs

## Capture Checklist

Before publishing screenshots:

- build the repo in `Release`
- start `MiniGPTCSharp.Web`
- use the default prompt: `The capital of France is`
- show token chips, probabilities, attention, timeline, and ablations
- capture desktop around 1440x1000
- capture mobile around 390x844
- keep browser chrome out of final images where possible
- verify text is readable at GitHub README width

## Suggested GIF Flow

1. Open GPT Microscope.
2. Type or keep `The capital of France is`.
3. Run inspection.
4. Toggle deterministic/sampling.
5. Change seed.
6. Show ablation comparison.
7. Export report.

Keep the GIF under 10 seconds if possible.
