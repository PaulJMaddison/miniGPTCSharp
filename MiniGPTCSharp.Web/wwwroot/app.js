const els = {
  prompt: document.querySelector("#prompt"),
  status: document.querySelector("#status"),
  seed: document.querySelector("#seed"),
  temperature: document.querySelector("#temperature"),
  topK: document.querySelector("#topK"),
  tokenCount: document.querySelector("#tokenCount"),
  temperatureValue: document.querySelector("#temperatureValue"),
  topKValue: document.querySelector("#topKValue"),
  tokenCountValue: document.querySelector("#tokenCountValue"),
  disableAttention: document.querySelector("#disableAttention"),
  disablePosition: document.querySelector("#disablePosition"),
  disableLayerNorm: document.querySelector("#disableLayerNorm"),
  runButton: document.querySelector("#runButton"),
  exportButton: document.querySelector("#exportButton"),
  modeLabel: document.querySelector("#modeLabel"),
  generatedText: document.querySelector("#generatedText"),
  tokenCountMeta: document.querySelector("#tokenCountMeta"),
  candidateMeta: document.querySelector("#candidateMeta"),
  timelineMeta: document.querySelector("#timelineMeta"),
  tokens: document.querySelector("#tokens"),
  probabilities: document.querySelector("#probabilities"),
  layerTabs: document.querySelector("#layerTabs"),
  attentionHeatmap: document.querySelector("#attentionHeatmap"),
  attentionTargets: document.querySelector("#attentionTargets"),
  timeline: document.querySelector("#timeline"),
  ablations: document.querySelector("#ablations"),
  inspectView: document.querySelector("#inspectView"),
  compareView: document.querySelector("#compareView")
};
let samplingMode = "deterministic";
let activeLayer = 0;
let latestResponse = null;
function requestFromControls() {
  return {
    prompt: els.prompt.value,
    deterministic: samplingMode === "deterministic",
    seed: readInt(els.seed.value, 42),
    temperature: Number.parseFloat(els.temperature.value),
    topK: readInt(els.topK.value, 10),
    tokenCount: readInt(els.tokenCount.value, 6),
    disableAttention: els.disableAttention.checked,
    disablePositionEmbeddings: els.disablePosition.checked,
    disableLayerNorm: els.disableLayerNorm.checked
  };
}
async function runInspection() {
  setStatus("Running");
  els.runButton.disabled = true;
  try {
    const response = await fetch("/api/inspect", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(requestFromControls()) });
    if (!response.ok) throw new Error(await response.text());
    latestResponse = await response.json();
    render(latestResponse);
    setStatus("Rendered");
  } catch (error) {
    setStatus("Error");
    els.generatedText.textContent = error instanceof Error ? error.message : String(error);
  } finally {
    els.runButton.disabled = false;
  }
}
async function exportReport() {
  setStatus("Exporting");
  const response = await fetch("/api/export-report", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(requestFromControls()) });
  if (response.ok) {
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "gpt-microscope-report.html";
    link.click();
    URL.revokeObjectURL(url);
    setStatus("Report exported");
    return;
  }
  const payload = await response.json().catch(() => null);
  setStatus(payload?.message ?? "Report export unavailable");
}
function render(data) {
  const settings = data.settings;
  activeLayer = Math.min(activeLayer, Math.max(0, data.attentionLayers.length - 1));
  els.modeLabel.textContent = settings.decisionMode;
  els.generatedText.textContent = data.generatedText;
  els.tokenCountMeta.textContent = `${data.promptTokens.length} prompt tokens`;
  els.candidateMeta.textContent = `top ${data.nextTokenCandidates.length}`;
  els.timelineMeta.textContent = `${data.timeline.length} steps`;
  renderTokens(data.promptTokens);
  renderCandidates(data.nextTokenCandidates, els.probabilities);
  renderLayerTabs(data.attentionLayers);
  renderAttention(data.attentionLayers[activeLayer], data.promptTokens);
  renderTimeline(data.timeline);
  renderAblations(data.ablations);
}
function renderTokens(tokens) {
  els.tokens.replaceChildren(...tokens.map(token => {
    const chip = document.createElement("span");
    chip.className = `token-chip${token.wasAlreadyInVocabulary ? "" : " fresh"}`;
    chip.innerHTML = `<span class="token-pos">${token.position}</span><span>${escapeHtml(token.text)}</span><span class="token-id">#${token.tokenId}</span>`;
    return chip;
  }));
}
function renderCandidates(candidates, target) {
  if (!candidates.length) {
    target.replaceChildren(empty("No candidates"));
    return;
  }
  const maxProbability = Math.max(...candidates.map(candidate => candidate.probability), 0.0001);
  target.replaceChildren(...candidates.map(candidate => {
    const row = document.createElement("div");
    row.className = "prob-row";
    const width = Math.max(2, candidate.probability / maxProbability * 100);
    row.innerHTML = `<div class="prob-token" title="${escapeAttr(candidate.text)}">${escapeHtml(candidate.text)}</div><div><div class="bar-track"><div class="bar-fill" style="width:${width}%"></div></div><div class="logit">logit ${formatNumber(candidate.logit)}</div></div><div class="prob-value">${formatPercent(candidate.probability)}</div>`;
    return row;
  }));
}
function renderLayerTabs(layers) {
  els.layerTabs.replaceChildren(...layers.map(layer => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `layer-tab${layer.layerIndex === activeLayer ? " active" : ""}`;
    button.textContent = `Layer ${layer.layerIndex}`;
    button.addEventListener("click", () => {
      activeLayer = layer.layerIndex;
      if (latestResponse) {
        renderLayerTabs(latestResponse.attentionLayers);
        renderAttention(latestResponse.attentionLayers[activeLayer], latestResponse.promptTokens);
      }
    });
    return button;
  }));
}
function renderAttention(layer, tokens) {
  if (!layer) {
    els.attentionHeatmap.replaceChildren(empty("No attention layers"));
    els.attentionTargets.replaceChildren();
    return;
  }
  const tokenLabels = tokens.map(token => `${token.position}:${token.text}`);
  const heatmap = document.createElement("div");
  heatmap.className = "heatmap";
  heatmap.style.gridTemplateColumns = `minmax(86px, 126px) repeat(${tokenLabels.length}, minmax(44px, 58px))`;
  heatmap.appendChild(label(""));
  tokenLabels.forEach(text => heatmap.appendChild(label(text)));
  layer.weights.forEach((row, rowIndex) => {
    heatmap.appendChild(label(tokenLabels[rowIndex] ?? rowIndex.toString()));
    row.forEach(value => {
      const cell = document.createElement("div");
      cell.className = "heat-cell";
      cell.style.background = heatColor(value);
      cell.style.color = value > 0.45 ? "#ffffff" : "#10221f";
      cell.textContent = value.toFixed(2);
      heatmap.appendChild(cell);
    });
  });
  els.attentionHeatmap.replaceChildren(heatmap);
  els.attentionTargets.replaceChildren(...layer.topTargets.map(target => {
    const chip = document.createElement("span");
    chip.className = "target-chip";
    chip.innerHTML = `<span>${escapeHtml(target.tokenText)}</span><span class="prob-value">${formatPercent(target.weight)}</span>`;
    return chip;
  }));
}
function renderTimeline(steps) {
  els.timeline.replaceChildren(...steps.map(step => {
    const item = document.createElement("article");
    item.className = "timeline-step";
    const topCandidates = step.topCandidates.slice(0, 4);
    const maxProbability = Math.max(...topCandidates.map(candidate => candidate.probability), 0.0001);
    const miniBars = document.createElement("div");
    miniBars.className = "mini-bars";
    miniBars.replaceChildren(...topCandidates.map(candidate => {
      const candidateEl = document.createElement("div");
      candidateEl.className = "mini-candidate";
      candidateEl.innerHTML = `<div class="mini-label"><strong>${escapeHtml(candidate.text)}</strong><span>${formatPercent(candidate.probability)}</span></div><div class="bar-track"><div class="bar-fill" style="width:${Math.max(2, candidate.probability / maxProbability * 100)}%"></div></div>`;
      return candidateEl;
    }));
    item.innerHTML = `<div class="step-index">Step ${step.stepNumber}</div><div class="step-body"><div class="chosen-chip"><span>${escapeHtml(step.chosen.text)}</span><span class="token-id">#${step.chosen.tokenId}</span><span class="prob-value">${formatPercent(step.chosen.probability)}</span></div><p class="step-text">${escapeHtml(step.textAfterStep)}</p></div>`;
    item.querySelector(".step-body").appendChild(miniBars);
    return item;
  }));
}
function renderAblations(ablations) {
  els.ablations.replaceChildren(...ablations.map(run => {
    const card = document.createElement("article");
    card.className = "ablation-card";
    const flag = run.disabledComponents.length ? run.disabledComponents.join(", ") : "all enabled";
    card.innerHTML = `<div class="ablation-title"><strong>${escapeHtml(run.label)}</strong><span class="flag">${escapeHtml(flag)}</span></div><p class="ablation-output">${escapeHtml(run.generatedText)}</p><div class="chosen-row"></div><div class="probability-list"></div>`;
    card.querySelector(".chosen-row").replaceChildren(...run.chosenTokens.map(token => {
      const chip = document.createElement("span");
      chip.className = "chosen-chip";
      chip.innerHTML = `<span>${escapeHtml(token.text)}</span><span class="token-id">#${token.tokenId}</span>`;
      return chip;
    }));
    renderCandidates(run.firstStepCandidates.slice(0, 3), card.querySelector(".probability-list"));
    return card;
  }));
}
function label(text) { const el = document.createElement("div"); el.className = "heat-label"; el.title = text; el.textContent = text; return el; }
function empty(text) { const el = document.createElement("div"); el.className = "empty-state"; el.textContent = text; return el; }
function heatColor(value) {
  const alpha = Math.min(0.96, 0.08 + value * 0.88);
  if (value > 0.6) return `rgba(173, 63, 71, ${alpha})`;
  if (value > 0.28) return `rgba(198, 134, 20, ${alpha})`;
  return `rgba(15, 118, 110, ${alpha})`;
}
function syncSliderLabels() {
  els.temperatureValue.textContent = Number.parseFloat(els.temperature.value).toFixed(2).replace(/0$/, "").replace(/\.$/, "");
  els.topKValue.textContent = els.topK.value;
  els.tokenCountValue.textContent = els.tokenCount.value;
}
function setMode(mode) {
  samplingMode = mode;
  document.querySelectorAll(".segment").forEach(button => button.classList.toggle("active", button.dataset.mode === mode));
  els.seed.disabled = mode === "deterministic";
}
function setView(view) {
  document.querySelectorAll(".view-tab").forEach(button => button.classList.toggle("active", button.dataset.view === view));
  els.inspectView.classList.toggle("active", view === "inspect");
  els.compareView.classList.toggle("active", view === "compare");
}
function setStatus(text) { els.status.textContent = text; }
function readInt(value, fallback) { const parsed = Number.parseInt(value, 10); return Number.isFinite(parsed) ? parsed : fallback; }
function formatPercent(value) { return `${(value * 100).toFixed(value < 0.01 ? 2 : 1)}%`; }
function formatNumber(value) { return Number(value).toFixed(3); }
function escapeHtml(value) { return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;"); }
function escapeAttr(value) { return escapeHtml(value).replaceAll("`", "&#096;"); }
document.querySelectorAll(".segment").forEach(button => button.addEventListener("click", () => setMode(button.dataset.mode)));
document.querySelectorAll(".view-tab").forEach(button => button.addEventListener("click", () => setView(button.dataset.view)));
[els.temperature, els.topK, els.tokenCount].forEach(input => input.addEventListener("input", syncSliderLabels));
els.runButton.addEventListener("click", runInspection);
els.exportButton.addEventListener("click", exportReport);
els.prompt.addEventListener("keydown", event => { if ((event.ctrlKey || event.metaKey) && event.key === "Enter") runInspection(); });
syncSliderLabels();
setMode("deterministic");
runInspection();
