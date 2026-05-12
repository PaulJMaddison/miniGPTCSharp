using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniGPTCSharp;

public static class MiniGptJson
{
    public const string SchemaVersion = "1.0";

    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);
}

public sealed class PredictExport
{
    public string SchemaVersion { get; init; } = MiniGptJson.SchemaVersion;
    public string Command { get; init; } = "predict";
    public string Prompt { get; init; } = string.Empty;
    public PredictOptionsExport Options { get; init; } = new();
    public IReadOnlyList<TokenExport> Tokens { get; init; } = [];
    public IReadOnlyList<PredictionExport> Predictions { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class PredictOptionsExport
{
    public int TopN { get; init; }
    public float Temperature { get; init; }
    public int TopKFilter { get; init; }
    public bool Deterministic { get; init; }
}

public sealed class PromptInspectionExport
{
    public string SchemaVersion { get; init; } = MiniGptJson.SchemaVersion;
    public string Command { get; init; } = "inspect";
    public string Topic { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public ModelConfigExport Config { get; init; } = new();
    public int TokenCount { get; init; }
    public IReadOnlyList<TokenExport> Tokens { get; init; } = [];
    public IReadOnlyList<EmbeddingPreviewExport>? Embeddings { get; init; }
    public IReadOnlyList<LayerAttentionExport>? Layers { get; init; }
    public IReadOnlyList<PredictionExport>? Predictions { get; init; }
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class TokenExport
{
    public int Position { get; init; }
    public string Text { get; init; } = string.Empty;
    public int TokenId { get; init; }
    public bool WasAlreadyInVocabulary { get; init; }
    public string VocabularySource { get; init; } = string.Empty;
}

public sealed class EmbeddingPreviewExport
{
    public int Position { get; init; }
    public string TokenText { get; init; } = string.Empty;
    public int TokenId { get; init; }
    public int DimensionsShown { get; init; }
    public IReadOnlyList<float> Values { get; init; } = [];
}

public sealed class LayerAttentionExport
{
    public int LayerIndex { get; init; }
    public IReadOnlyList<AttentionRowExport> AttentionRows { get; init; } = [];
    public IReadOnlyList<AttentionWeightExport> LastTokenWeights { get; init; } = [];
    public IReadOnlyList<AttentionWeightExport> TopTargets { get; init; } = [];
}

public sealed class AttentionRowExport
{
    public int TokenIndex { get; init; }
    public string TokenText { get; init; } = string.Empty;
    public IReadOnlyList<AttentionWeightExport> Weights { get; init; } = [];
}

public sealed class AttentionWeightExport
{
    public int TokenIndex { get; init; }
    public string TokenText { get; init; } = string.Empty;
    public float Weight { get; init; }
}

public sealed class PredictionExport
{
    public int Rank { get; init; }
    public int TokenId { get; init; }
    public string TokenText { get; init; } = string.Empty;
    public float Probability { get; init; }
}

public sealed class SamplingComparisonExport
{
    public string SchemaVersion { get; init; } = MiniGptJson.SchemaVersion;
    public string Command { get; init; } = "compare";
    public string Topic { get; init; } = "sampling";
    public string Prompt { get; init; } = string.Empty;
    public int TokensToGenerate { get; init; }
    public IReadOnlyList<PredictionExport> TopPredictions { get; init; } = [];
    public IReadOnlyList<GenerationRunExport> Runs { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class GenerationRunExport
{
    public string Label { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public bool Deterministic { get; init; }
    public int? Seed { get; init; }
    public string Output { get; init; } = string.Empty;
}

public sealed class AblationComparisonExport
{
    public string SchemaVersion { get; init; } = MiniGptJson.SchemaVersion;
    public string Command { get; init; } = "compare";
    public string Topic { get; init; } = "ablation";
    public string Prompt { get; init; } = string.Empty;
    public int TokensToGenerate { get; init; }
    public IReadOnlyList<AblationRunExport> Runs { get; init; } = [];
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class AblationRunExport
{
    public string Label { get; init; } = string.Empty;
    public ModelConfigExport Config { get; init; } = new();
    public string Output { get; init; } = string.Empty;
}

public sealed class ModelConfigExport
{
    public int VocabularySize { get; init; }
    public int EmbeddingSize { get; init; }
    public int LayerCount { get; init; }
    public int TopK { get; init; }
    public float Temperature { get; init; }
    public bool DisableAttention { get; init; }
    public bool DisablePositionEmbeddings { get; init; }
    public bool DisableLayerNorm { get; init; }
}

public sealed class PromptSummaryExport
{
    public string Text { get; init; } = string.Empty;
    public int CharacterCount { get; init; }
    public int TokenCount { get; init; }
    public int DistinctTokenCount { get; init; }
}

public sealed class GptInternalsReportExport
{
    public string SchemaVersion { get; init; } = MiniGptJson.SchemaVersion;
    public string Command { get; init; } = "report";
    public PromptSummaryExport Summary { get; init; } = new();
    public PromptInspectionExport Pipeline { get; init; } = new();
    public SamplingComparisonExport Sampling { get; init; } = new();
    public AblationComparisonExport Ablation { get; init; } = new();
    public IReadOnlyList<string> Notes { get; init; } = [];
}
