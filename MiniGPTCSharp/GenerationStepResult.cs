namespace MiniGPTCSharp;

public sealed class GenerationStepResult
{
    public int NextTokenId { get; init; }

    public string NextTokenText { get; init; } = string.Empty;

    public IReadOnlyList<(int id, string text, float p)> TopK { get; init; } = Array.Empty<(int id, string text, float p)>();

    public float Temperature { get; init; }

    public int TopKValue { get; init; }

    public string DebugText { get; init; } = string.Empty;
}
