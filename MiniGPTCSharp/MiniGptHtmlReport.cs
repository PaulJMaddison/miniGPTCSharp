using System.Globalization;
using System.Net;
using System.Text;

namespace MiniGPTCSharp;

public static class MiniGptHtmlReport
{
    public static string BuildHtml(GptInternalsReportExport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"  <title>{H(report.Summary.Text)} - GPT Internals Report</title>");
        AppendStyles(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <main class=\"page\">");
        AppendHeader(sb, report);
        AppendSummaryCards(sb, report);
        AppendTokenTable(sb, report.Pipeline.Tokens);
        AppendEmbeddingPreview(sb, report.Pipeline.Embeddings ?? []);
        AppendAttention(sb, report.Pipeline.Layers ?? []);
        AppendProbabilityBars(sb, report.Pipeline.Predictions ?? []);
        AppendSamplingComparison(sb, report.Sampling);
        AppendAblationComparison(sb, report.Ablation);
        AppendNotes(sb, report.Notes);
        sb.AppendLine("  </main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("  <style>");
        sb.AppendLine("""
    :root {
      --paper: #fbfaf7;
      --surface: #ffffff;
      --ink: #18212f;
      --muted: #5d6673;
      --line: #dfd9cc;
      --soft-line: #eee8dc;
      --green: #28665f;
      --gold: #c27a20;
      --coral: #b84f45;
      --blue: #3f67a1;
      --code: #f4efe5;
    }

    * { box-sizing: border-box; }

    body {
      margin: 0;
      background: var(--paper);
      color: var(--ink);
      font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, "Helvetica Neue", Arial, sans-serif;
      line-height: 1.5;
      letter-spacing: 0;
    }

    .page {
      width: min(1120px, calc(100% - 32px));
      margin: 0 auto;
      padding: 36px 0 52px;
    }

    .report-header {
      border-bottom: 1px solid var(--line);
      padding-bottom: 24px;
    }

    .eyebrow {
      color: var(--green);
      font-size: 13px;
      font-weight: 700;
      text-transform: uppercase;
    }

    h1 {
      margin: 8px 0 12px;
      font-size: 38px;
      line-height: 1.08;
      font-weight: 780;
    }

    h2 {
      margin: 0 0 12px;
      font-size: 24px;
      line-height: 1.2;
    }

    h3 {
      margin: 20px 0 10px;
      font-size: 17px;
      line-height: 1.25;
    }

    p { margin: 0; }

    .prompt {
      max-width: 840px;
      color: var(--muted);
      font-size: 19px;
      overflow-wrap: anywhere;
    }

    .prompt strong { color: var(--ink); }

    .section {
      border-top: 1px solid var(--line);
      padding: 30px 0;
    }

    .section-intro {
      max-width: 760px;
      color: var(--muted);
      margin-bottom: 16px;
    }

    .summary-grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 12px;
      padding: 22px 0 6px;
    }

    .stat-card,
    .note-card {
      background: var(--surface);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 14px;
      min-width: 0;
    }

    .stat-label {
      color: var(--muted);
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
    }

    .stat-value {
      margin-top: 6px;
      color: var(--ink);
      font-size: 24px;
      font-weight: 780;
      overflow-wrap: anywhere;
    }

    .table-wrap {
      overflow-x: auto;
      background: var(--surface);
      border: 1px solid var(--line);
      border-radius: 8px;
    }

    table {
      width: 100%;
      min-width: 560px;
      border-collapse: collapse;
    }

    th,
    td {
      border-bottom: 1px solid var(--soft-line);
      padding: 10px 12px;
      text-align: left;
      vertical-align: top;
    }

    th {
      color: var(--muted);
      font-size: 12px;
      font-weight: 750;
      text-transform: uppercase;
      white-space: nowrap;
    }

    tr:last-child td { border-bottom: 0; }

    .mono {
      font-family: "Cascadia Mono", "SFMono-Regular", Consolas, "Liberation Mono", monospace;
      font-size: 13px;
      overflow-wrap: anywhere;
    }

    .pill {
      display: inline-block;
      max-width: 100%;
      padding: 3px 7px;
      border-radius: 6px;
      background: var(--code);
      color: var(--ink);
      overflow-wrap: anywhere;
    }

    .vector {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      min-width: 260px;
    }

    .vector span {
      border-radius: 6px;
      background: var(--code);
      color: var(--ink);
      padding: 3px 6px;
    }

    .bar-list { display: grid; gap: 12px; }

    .bar-item {
      display: grid;
      grid-template-columns: minmax(120px, 220px) 1fr minmax(64px, auto);
      gap: 12px;
      align-items: center;
      min-width: 0;
    }

    .bar-token {
      min-width: 0;
      overflow-wrap: anywhere;
    }

    .bar-track {
      height: 12px;
      overflow: hidden;
      border-radius: 999px;
      background: #ebe5d8;
    }

    .bar-fill {
      height: 100%;
      border-radius: 999px;
      background: linear-gradient(90deg, var(--green), var(--gold));
    }

    .bar-percent {
      color: var(--muted);
      font-variant-numeric: tabular-nums;
      text-align: right;
    }

    .attention-layer { margin-top: 14px; }
    .heatmap-cell { min-width: 86px; font-variant-numeric: tabular-nums; }

    .notes-grid {
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 12px;
    }

    .note-card {
      border-top: 4px solid var(--green);
      color: var(--muted);
      overflow-wrap: anywhere;
    }

    .note-card:nth-child(2) { border-top-color: var(--gold); }
    .note-card:nth-child(3) { border-top-color: var(--blue); }
    .note-card:nth-child(4) { border-top-color: var(--coral); }
    .note-card:nth-child(5) { border-top-color: var(--ink); }

    @media (max-width: 860px) {
      .summary-grid,
      .notes-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }

      .bar-item { grid-template-columns: minmax(0, 1fr) minmax(68px, auto); }
      .bar-track { grid-column: 1 / -1; grid-row: 2; }
    }

    @media (max-width: 560px) {
      .page {
        width: min(100% - 22px, 1120px);
        padding-top: 24px;
      }

      h1 { font-size: 30px; }
      .prompt { font-size: 17px; }
      .summary-grid,
      .notes-grid { grid-template-columns: 1fr; }
      th,
      td { padding: 9px 10px; }
    }
""");
        sb.AppendLine("  </style>");
    }

    private static void AppendHeader(StringBuilder sb, GptInternalsReportExport report)
    {
        sb.AppendLine("    <header class=\"report-header\">");
        sb.AppendLine("      <div class=\"eyebrow\">MiniGPTSharp offline inspection</div>");
        sb.AppendLine("      <h1>GPT Internals Report</h1>");
        sb.AppendLine($"      <p class=\"prompt\"><strong>Prompt:</strong> {H(report.Summary.Text)}</p>");
        sb.AppendLine("    </header>");
    }

    private static void AppendSummaryCards(StringBuilder sb, GptInternalsReportExport report)
    {
        var bestPrediction = report.Pipeline.Predictions?.FirstOrDefault();
        sb.AppendLine("    <section class=\"summary-grid\" aria-label=\"Prompt summary\">");
        AppendStatCard(sb, "Characters", report.Summary.CharacterCount.ToString(CultureInfo.InvariantCulture));
        AppendStatCard(sb, "Tokens", report.Summary.TokenCount.ToString(CultureInfo.InvariantCulture));
        AppendStatCard(sb, "Distinct IDs", report.Summary.DistinctTokenCount.ToString(CultureInfo.InvariantCulture));
        AppendStatCard(sb, "Top next token", bestPrediction is null ? "n/a" : bestPrediction.TokenText);
        sb.AppendLine("    </section>");
    }

    private static void AppendStatCard(StringBuilder sb, string label, string value)
    {
        sb.AppendLine("      <article class=\"stat-card\">");
        sb.AppendLine($"        <div class=\"stat-label\">{H(label)}</div>");
        sb.AppendLine($"        <div class=\"stat-value\">{H(value)}</div>");
        sb.AppendLine("      </article>");
    }

    private static void AppendTokenTable(StringBuilder sb, IReadOnlyList<TokenExport> tokens)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Token Table</h2>");
        sb.AppendLine("      <p class=\"section-intro\">The prompt becomes discrete token IDs before the model can do any numeric work.</p>");
        sb.AppendLine("      <div class=\"table-wrap\"><table>");
        sb.AppendLine("        <thead><tr><th>Position</th><th>Token</th><th>ID</th><th>Vocabulary source</th></tr></thead><tbody>");
        foreach (var token in tokens)
        {
            sb.AppendLine("        <tr>");
            sb.AppendLine($"          <td class=\"mono\">{token.Position}</td><td><span class=\"pill mono\">{H(token.Text)}</span></td><td class=\"mono\">{token.TokenId}</td><td>{H(token.VocabularySource)}</td>");
            sb.AppendLine("        </tr>");
        }
        sb.AppendLine("        </tbody></table></div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendEmbeddingPreview(StringBuilder sb, IReadOnlyList<EmbeddingPreviewExport> embeddings)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Embedding Preview</h2>");
        sb.AppendLine("      <p class=\"section-intro\">Each token ID is expanded into a numeric vector. This report shows the first dimensions to keep the signal readable.</p>");
        sb.AppendLine("      <div class=\"table-wrap\"><table>");
        sb.AppendLine("        <thead><tr><th>Token</th><th>ID</th><th>First dimensions</th></tr></thead><tbody>");
        foreach (var item in embeddings)
        {
            sb.AppendLine("        <tr>");
            sb.AppendLine($"          <td><span class=\"pill mono\">{H(item.TokenText)}</span></td><td class=\"mono\">{item.TokenId}</td><td><div class=\"vector mono\">");
            foreach (var value in item.Values)
            {
                sb.AppendLine($"            <span>{FormatFloat(value)}</span>");
            }
            sb.AppendLine("          </div></td>");
            sb.AppendLine("        </tr>");
        }
        sb.AppendLine("        </tbody></table></div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendAttention(StringBuilder sb, IReadOnlyList<LayerAttentionExport> layers)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Attention By Layer</h2>");
        sb.AppendLine("      <p class=\"section-intro\">The heatmap shows how strongly the final token attends to each token in the prompt. Darker cells carry more weight.</p>");

        if (layers.Count == 0)
        {
            sb.AppendLine("      <p>No transformer layers are active, so there are no attention weights to inspect.</p>");
            sb.AppendLine("    </section>");
            return;
        }

        foreach (var layer in layers)
        {
            sb.AppendLine("      <div class=\"attention-layer\">");
            sb.AppendLine($"        <h3>Layer {layer.LayerIndex}</h3>");
            sb.AppendLine("        <div class=\"table-wrap\"><table><thead><tr>");
            foreach (var weight in layer.LastTokenWeights)
            {
                sb.AppendLine($"          <th>{H(weight.TokenText)}</th>");
            }
            sb.AppendLine("        </tr></thead><tbody><tr>");
            foreach (var weight in layer.LastTokenWeights)
            {
                var alpha = Math.Clamp(0.10f + weight.Weight * 0.75f, 0.10f, 0.90f);
                sb.AppendLine($"          <td class=\"heatmap-cell mono\" style=\"background: rgba(40, 102, 95, {FormatAlpha(alpha)});\">{FormatPercent(weight.Weight)}</td>");
            }
            sb.AppendLine("        </tr></tbody></table></div></div>");
        }
        sb.AppendLine("    </section>");
    }

    private static void AppendProbabilityBars(StringBuilder sb, IReadOnlyList<PredictionExport> predictions)
    {
        var maxProbability = predictions.Count == 0 ? 1f : predictions.Max(prediction => prediction.Probability);
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Top Next-Token Probability Bars</h2>");
        sb.AppendLine("      <p class=\"section-intro\">These are the model's strongest beliefs for the next token after the prompt.</p>");
        sb.AppendLine("      <div class=\"bar-list\">");
        foreach (var prediction in predictions)
        {
            var width = maxProbability <= 0 ? 0 : Math.Clamp(prediction.Probability / maxProbability * 100f, 2f, 100f);
            sb.AppendLine("        <div class=\"bar-item\">");
            sb.AppendLine($"          <div class=\"bar-token\"><span class=\"pill mono\">{H(prediction.TokenText)}</span></div>");
            sb.AppendLine($"          <div class=\"bar-track\"><div class=\"bar-fill\" style=\"width: {FormatPercentWidth(width)};\"></div></div>");
            sb.AppendLine($"          <div class=\"bar-percent mono\">{FormatPercent(prediction.Probability)}</div>");
            sb.AppendLine("        </div>");
        }
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendSamplingComparison(StringBuilder sb, SamplingComparisonExport sampling)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Sampling Comparison</h2>");
        sb.AppendLine("      <p class=\"section-intro\">Argmax follows the highest probability every time. Seeded sampling uses the same belief distribution but allows different repeatable paths.</p>");
        AppendRunTable(sb, sampling.Runs);
        sb.AppendLine("    </section>");
    }

    private static void AppendAblationComparison(StringBuilder sb, AblationComparisonExport ablation)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>Ablation Comparison</h2>");
        sb.AppendLine("      <p class=\"section-intro\">Removing one mechanism at a time makes the contribution of attention, position, and layer norm easier to see.</p>");
        sb.AppendLine("      <div class=\"table-wrap\"><table>");
        sb.AppendLine("        <thead><tr><th>Variant</th><th>Changed mechanism</th><th>Output</th></tr></thead><tbody>");
        foreach (var run in ablation.Runs)
        {
            sb.AppendLine($"        <tr><td>{H(run.Label)}</td><td>{H(DescribeDisabledMechanisms(run.Config))}</td><td>{H(run.Output)}</td></tr>");
        }
        sb.AppendLine("        </tbody></table></div>");
        sb.AppendLine("    </section>");
    }

    private static void AppendRunTable(StringBuilder sb, IReadOnlyList<GenerationRunExport> runs)
    {
        sb.AppendLine("      <div class=\"table-wrap\"><table>");
        sb.AppendLine("        <thead><tr><th>Run</th><th>Mode</th><th>Seed</th><th>Output</th></tr></thead><tbody>");
        foreach (var run in runs)
        {
            var seed = run.Seed.HasValue ? run.Seed.Value.ToString(CultureInfo.InvariantCulture) : "none";
            sb.AppendLine($"        <tr><td>{H(run.Label)}</td><td class=\"mono\">{H(run.Mode)}</td><td class=\"mono\">{seed}</td><td>{H(run.Output)}</td></tr>");
        }
        sb.AppendLine("        </tbody></table></div>");
    }

    private static void AppendNotes(StringBuilder sb, IReadOnlyList<string> notes)
    {
        sb.AppendLine("    <section class=\"section\">");
        sb.AppendLine("      <h2>What This Teaches</h2>");
        sb.AppendLine("      <div class=\"notes-grid\">");
        foreach (var note in notes)
        {
            sb.AppendLine($"        <article class=\"note-card\">{H(note)}</article>");
        }
        sb.AppendLine("      </div>");
        sb.AppendLine("    </section>");
    }

    private static string DescribeDisabledMechanisms(ModelConfigExport config)
    {
        var disabled = new List<string>();
        if (config.DisableAttention) disabled.Add("attention disabled");
        if (config.DisablePositionEmbeddings) disabled.Add("position embeddings disabled");
        if (config.DisableLayerNorm) disabled.Add("layer norm disabled");
        return disabled.Count == 0 ? "baseline" : string.Join(", ", disabled);
    }

    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string FormatFloat(float value) => value.ToString("0.000", CultureInfo.InvariantCulture);
    private static string FormatAlpha(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string FormatPercent(float value) => value.ToString("P2", CultureInfo.InvariantCulture);
    private static string FormatPercentWidth(float value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
