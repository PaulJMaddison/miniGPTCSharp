const promptTokens = [
  { text: "The", id: 4 },
  { text: "capital", id: 12 },
  { text: "of", id: 5 },
  { text: "France", id: 11 },
  { text: "is", id: 8 }
];

const probabilities = [
  { token: "capital", probability: 0.2138 },
  { token: "France", probability: 0.1757 },
  { token: "Paris", probability: 0.1552 },
  { token: "AI", probability: 0.0949 },
  { token: "learning", probability: 0.077 }
];

const heatmap = [
  0.04, 0.08, 0.13, 0.18, 0.57,
  0.07, 0.13, 0.15, 0.22, 0.43,
  0.09, 0.14, 0.22, 0.25, 0.30,
  0.12, 0.19, 0.24, 0.28, 0.17,
  0.09, 0.17, 0.18, 0.28, 0.28
];

const traces = {
  deterministic: {
    badge: "Deterministic argmax",
    seed: "n/a",
    temperature: "0.8",
    output: "the capital of France is capital Paris model next token learning",
    steps: [
      ["01", "capital", "21.38%", "argmax selected the highest candidate"],
      ["02", "Paris", "18.91%", "context shifted after appending capital"],
      ["03", "model", "16.44%", "new token became part of the context"],
      ["04", "next", "14.73%", "loop continues one token at a time"]
    ]
  },
  seeded: {
    badge: "Seeded sampling (42)",
    seed: "42",
    temperature: "0.8",
    output: "the capital of France is AI capital AI next next token",
    steps: [
      ["01", "AI", "9.49%", "sampling can choose a non-top token"],
      ["02", "capital", "20.11%", "seed keeps the sampled path repeatable"],
      ["03", "AI", "11.02%", "probability guides but does not dictate"],
      ["04", "next", "13.88%", "same seed gives the same continuation"]
    ]
  }
};

const tokenStrip = document.querySelector("#tokenStrip");
const probabilityList = document.querySelector("#probabilityList");
const attentionGrid = document.querySelector("#attentionGrid");
const timeline = document.querySelector("#timeline");
const generatedText = document.querySelector("#generatedText");
const modeBadge = document.querySelector("#modeBadge");
const seedMetric = document.querySelector("#seedMetric");
const temperatureMetric = document.querySelector("#temperatureMetric");
const replayButton = document.querySelector("#replayButton");
const switches = document.querySelectorAll(".switch");

let currentMode = "deterministic";

function renderTokens() {
  tokenStrip.innerHTML = "";
  promptTokens.forEach((token, index) => {
    const chip = document.createElement("div");
    chip.className = "token-chip";
    chip.style.animationDelay = `${index * 65}ms`;

    const tokenText = document.createElement("span");
    tokenText.textContent = token.text;

    const tokenId = document.createElement("span");
    tokenId.textContent = `id ${token.id}`;

    chip.append(tokenText, tokenId);
    tokenStrip.appendChild(chip);
  });
}

function renderProbabilities() {
  probabilityList.innerHTML = "";
  probabilities.forEach((item, index) => {
    const row = document.createElement("div");
    row.className = "prob-row";
    row.style.animationDelay = `${index * 70}ms`;

    const token = document.createElement("div");
    token.className = "prob-token";
    token.textContent = item.token;

    const track = document.createElement("div");
    track.className = "bar-track";

    const fill = document.createElement("div");
    fill.className = "bar-fill";
    track.appendChild(fill);

    const value = document.createElement("div");
    value.className = "prob-value";
    value.textContent = `${Math.round(item.probability * 100)}%`;

    row.append(token, track, value);
    probabilityList.appendChild(row);

    requestAnimationFrame(() => {
      fill.style.width = `${Math.min(item.probability * 360, 100)}%`;
    });
  });
}

function renderAttention() {
  attentionGrid.innerHTML = "";
  heatmap.forEach((weight, index) => {
    const cell = document.createElement("div");
    cell.className = "heat-cell";
    cell.style.animationDelay = `${index * 18}ms`;

    const lightness = 92 - weight * 58;
    const hue = 78 + weight * 64;
    cell.style.background = `hsl(${hue} 82% ${lightness}%)`;
    cell.textContent = weight.toFixed(2);

    attentionGrid.appendChild(cell);
  });
}

function renderTrace(mode) {
  const trace = traces[mode];
  generatedText.textContent = trace.output;
  modeBadge.textContent = trace.badge;
  seedMetric.textContent = trace.seed;
  temperatureMetric.textContent = trace.temperature;
  timeline.innerHTML = "";

  trace.steps.forEach((step, index) => {
    const row = document.createElement("div");
    row.className = "timeline-step";
    row.style.animationDelay = `${index * 85}ms`;

    const stepLabel = document.createElement("strong");
    stepLabel.textContent = `Step ${step[0]}`;

    const detail = document.createElement("p");
    const chosen = document.createElement("strong");
    chosen.textContent = step[1];
    detail.append(chosen, ` chosen at ${step[2]}. ${step[3]}.`);

    row.append(stepLabel, detail);
    timeline.appendChild(row);
  });
}

function setMode(mode) {
  currentMode = mode;
  switches.forEach((button) => {
    button.classList.toggle("active", button.dataset.mode === mode);
  });
  renderTrace(mode);
}

switches.forEach((button) => {
  button.addEventListener("click", () => setMode(button.dataset.mode));
});

replayButton.addEventListener("click", () => {
  renderTokens();
  renderProbabilities();
  renderAttention();
  renderTrace(currentMode);
});

renderTokens();
renderProbabilities();
renderAttention();
renderTrace(currentMode);
