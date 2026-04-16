namespace MiniGPTCSharp;

public sealed class PromptInspection
{
    public string Prompt { get; init; } = string.Empty;

    public IReadOnlyList<TokenInspection> Tokens { get; init; } = Array.Empty<TokenInspection>();

    public IReadOnlyList<LayerAttentionInspection> Layers { get; init; } = Array.Empty<LayerAttentionInspection>();

    public IReadOnlyList<NextTokenPrediction> Predictions { get; init; } = Array.Empty<NextTokenPrediction>();
}

public sealed class TokenInspection
{
    public int Position { get; init; }

    public string Text { get; init; } = string.Empty;

    public int TokenId { get; init; }

    public bool WasAlreadyInVocabulary { get; init; }

    public IReadOnlyList<float> Embedding { get; init; } = Array.Empty<float>();
}

public sealed class LayerAttentionInspection
{
    public int LayerIndex { get; init; }

    public IReadOnlyList<float> LastTokenWeights { get; init; } = Array.Empty<float>();

    public IReadOnlyList<AttentionTargetInspection> TopTargets { get; init; } = Array.Empty<AttentionTargetInspection>();
}

public sealed class AttentionTargetInspection
{
    public int TokenIndex { get; init; }

    public string TokenText { get; init; } = string.Empty;

    public float Weight { get; init; }
}
