using MiniGPTCSharp;

namespace MiniGPTCSharp.Web;

public sealed class GptMicroscopeService
{
    private const string DefaultPrompt = "The capital of France is";

    public MicroscopeResponse Inspect(InspectionRequest request)
    {
        var settings = Normalize(request);
        var model = new MiniGptModel(BuildConfig(settings));
        var inspection = model.InspectPrompt(settings.Prompt, settings.TopK, 5);
        var tokenIds = inspection.Tokens.Select(token => token.TokenId).ToList();
        var preview = model.Step(tokenIds, settings.Temperature, settings.TopK, deterministic: settings.Deterministic, seed: settings.Seed);
        var trace = GenerateTrace(model, tokenIds, settings);

        return new MicroscopeResponse
        {
            Settings = settings,
            PromptTokens = inspection.Tokens.Select(token => new TokenDto(token.Position, token.Text, token.TokenId, token.WasAlreadyInVocabulary, token.Embedding.Take(8).Select(Round).ToArray())).ToArray(),
            NextTokenCandidates = ToCandidates(preview.DebugInfo.Candidates),
            AttentionLayers = ToAttentionLayers(inspection),
            Timeline = trace.Steps,
            GeneratedText = trace.GeneratedText,
            Ablations = BuildAblations(settings),
            ReportExportAvailable = true
        };
    }

    public string BuildReportHtml(InspectionRequest request)
    {
        var settings = Normalize(request);
        var report = MiniGptExports.BuildReport(settings.Prompt, settings.TokenCount, dimsToShow: 8);
        return MiniGptHtmlReport.BuildHtml(report);
    }

    private static GenerationTrace GenerateTrace(MiniGptModel model, IReadOnlyList<int> initialTokenIds, MicroscopeSettings settings)
    {
        var tokens = initialTokenIds.ToList();
        var steps = new List<GenerationStepDto>(settings.TokenCount);
        Random? rng = settings.Deterministic ? null : new Random(settings.Seed);

        for (var stepNumber = 1; stepNumber <= settings.TokenCount; stepNumber++)
        {
            var contextText = model.Tokenizer.Decode(tokens);
            var step = model.Step(tokens, settings.Temperature, settings.TopK, deterministic: settings.Deterministic, samplingRandom: rng);
            tokens.Add(step.NextTokenId);
            steps.Add(new GenerationStepDto
            {
                StepNumber = stepNumber,
                ContextText = contextText,
                Chosen = ToCandidate(step.DebugInfo.Chosen),
                TopCandidates = ToCandidates(step.DebugInfo.Candidates),
                TextAfterStep = model.Tokenizer.Decode(tokens),
                TokenIdsAfterStep = tokens.ToArray()
            });
        }

        return new GenerationTrace(model.Tokenizer.Decode(tokens), steps);
    }

    private static IReadOnlyList<AblationRunDto> BuildAblations(MicroscopeSettings settings)
    {
        var variants = new[]
        {
            new AblationVariant("Baseline", false, false, false, Array.Empty<string>()),
            new AblationVariant("No attention", true, false, false, ["attention"]),
            new AblationVariant("No position", false, true, false, ["position embeddings"]),
            new AblationVariant("No layer norm", false, false, true, ["layer norm"])
        };

        return variants.Select(variant =>
        {
            var model = new MiniGptModel(BuildConfig(settings, variant));
            var inspection = model.InspectPrompt(settings.Prompt, settings.TopK, 5);
            var tokenIds = inspection.Tokens.Select(token => token.TokenId).ToList();
            var firstStep = model.Step(tokenIds, settings.Temperature, settings.TopK, deterministic: settings.Deterministic, seed: settings.Seed);
            var trace = GenerateTrace(model, tokenIds, settings);
            return new AblationRunDto
            {
                Label = variant.Label,
                DisabledComponents = variant.DisabledComponents,
                GeneratedText = trace.GeneratedText,
                FirstStepCandidates = ToCandidates(firstStep.DebugInfo.Candidates),
                ChosenTokens = trace.Steps.Select(step => step.Chosen).ToArray()
            };
        }).ToArray();
    }

    private static GptConfig BuildConfig(MicroscopeSettings settings, AblationVariant? variant = null)
    {
        return new GptConfig
        {
            LayerCount = 2,
            Temperature = settings.Temperature,
            TopK = settings.TopK,
            DisableAttention = variant?.DisableAttention ?? settings.DisableAttention,
            DisablePositionEmbeddings = variant?.DisablePositionEmbeddings ?? settings.DisablePositionEmbeddings,
            DisableLayerNorm = variant?.DisableLayerNorm ?? settings.DisableLayerNorm
        };
    }

    private static MicroscopeSettings Normalize(InspectionRequest request)
    {
        var temperature = float.IsNaN(request.Temperature) || float.IsInfinity(request.Temperature) ? 0.8f : Math.Clamp(request.Temperature, 0f, 2f);
        return new MicroscopeSettings
        {
            Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultPrompt : request.Prompt.Trim(),
            Deterministic = request.Deterministic,
            Seed = request.Seed == int.MinValue ? 42 : request.Seed,
            Temperature = temperature,
            TopK = Math.Clamp(request.TopK, 1, 32),
            TokenCount = Math.Clamp(request.TokenCount, 1, 16),
            DisableAttention = request.DisableAttention,
            DisablePositionEmbeddings = request.DisablePositionEmbeddings,
            DisableLayerNorm = request.DisableLayerNorm
        };
    }

    private static IReadOnlyList<CandidateDto> ToCandidates(IEnumerable<Candidate> candidates) => candidates.Select(ToCandidate).ToArray();
    private static CandidateDto ToCandidate(Candidate candidate) => new(candidate.TokenId, candidate.Text, Round(candidate.Logit), Round(candidate.Probability));
    private static IReadOnlyList<LayerAttentionDto> ToAttentionLayers(PromptInspection inspection) => inspection.Layers.Select(layer => new LayerAttentionDto(layer.LayerIndex, layer.AttentionWeights.Select(row => (IReadOnlyList<float>)row.Select(Round).ToArray()).ToArray(), layer.TopTargets.Select(target => new AttentionTargetDto(target.TokenIndex, target.TokenText, Round(target.Weight))).ToArray())).ToArray();
    private static float Round(float value) => MathF.Round(value * 10000f) / 10000f;
    private sealed record GenerationTrace(string GeneratedText, IReadOnlyList<GenerationStepDto> Steps);
    private sealed record AblationVariant(string Label, bool DisableAttention, bool DisablePositionEmbeddings, bool DisableLayerNorm, IReadOnlyList<string> DisabledComponents);
}
