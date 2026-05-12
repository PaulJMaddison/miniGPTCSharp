namespace MiniGPTCSharp.Web;

public sealed class InspectionRequest
{
    public string Prompt { get; init; } = "The capital of France is";
    public bool Deterministic { get; init; } = true;
    public int Seed { get; init; } = 42;
    public float Temperature { get; init; } = 0.8f;
    public int TopK { get; init; } = 10;
    public int TokenCount { get; init; } = 6;
    public bool DisableAttention { get; init; }
    public bool DisablePositionEmbeddings { get; init; }
    public bool DisableLayerNorm { get; init; }
}

public sealed class MicroscopeResponse
{
    public MicroscopeSettings Settings { get; init; } = new();
    public IReadOnlyList<TokenDto> PromptTokens { get; init; } = [];
    public IReadOnlyList<CandidateDto> NextTokenCandidates { get; init; } = [];
    public IReadOnlyList<LayerAttentionDto> AttentionLayers { get; init; } = [];
    public IReadOnlyList<GenerationStepDto> Timeline { get; init; } = [];
    public string GeneratedText { get; init; } = string.Empty;
    public IReadOnlyList<AblationRunDto> Ablations { get; init; } = [];
    public bool ReportExportAvailable { get; init; }
}

public sealed class MicroscopeSettings
{
    public string Prompt { get; init; } = string.Empty;
    public bool Deterministic { get; init; }
    public int Seed { get; init; }
    public float Temperature { get; init; }
    public int TopK { get; init; }
    public int TokenCount { get; init; }
    public bool DisableAttention { get; init; }
    public bool DisablePositionEmbeddings { get; init; }
    public bool DisableLayerNorm { get; init; }
    public string DecisionMode => Deterministic ? "Deterministic argmax" : $"Seeded sampling ({Seed})";
}

public sealed record TokenDto(int Position, string Text, int TokenId, bool WasAlreadyInVocabulary, IReadOnlyList<float> EmbeddingPreview);
public sealed record CandidateDto(int TokenId, string Text, float Logit, float Probability);
public sealed record AttentionTargetDto(int TokenIndex, string TokenText, float Weight);
public sealed record LayerAttentionDto(int LayerIndex, IReadOnlyList<IReadOnlyList<float>> Weights, IReadOnlyList<AttentionTargetDto> TopTargets);

public sealed class GenerationStepDto
{
    public int StepNumber { get; init; }
    public string ContextText { get; init; } = string.Empty;
    public CandidateDto Chosen { get; init; } = new(0, string.Empty, 0, 0);
    public IReadOnlyList<CandidateDto> TopCandidates { get; init; } = [];
    public string TextAfterStep { get; init; } = string.Empty;
    public IReadOnlyList<int> TokenIdsAfterStep { get; init; } = [];
}

public sealed class AblationRunDto
{
    public string Label { get; init; } = string.Empty;
    public IReadOnlyList<string> DisabledComponents { get; init; } = [];
    public string GeneratedText { get; init; } = string.Empty;
    public IReadOnlyList<CandidateDto> FirstStepCandidates { get; init; } = [];
    public IReadOnlyList<CandidateDto> ChosenTokens { get; init; } = [];
}

public sealed record ReportPlaceholderDto(string Status, string Message, string Prompt);
