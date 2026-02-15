namespace MiniGPTCSharp;

public sealed class GenerationStepResult
{
    public int NextTokenId { get; init; }

    public string NextTokenText { get; init; } = string.Empty;

    public IReadOnlyList<(int id, string text, float p)> TopK { get; init; } = Array.Empty<(int id, string text, float p)>();

    public StepDebugInfo DebugInfo { get; init; } = new();

    public float Temperature { get; init; }

    public int TopKValue { get; init; }

    public string DebugText { get; init; } = string.Empty;
}

public sealed class StepDebugInfo
{
    public int StepIndex { get; init; }

    public int[] ContextTokenIds { get; init; } = Array.Empty<int>();

    public float Temperature { get; init; }

    public int TopK { get; init; }

    public IReadOnlyList<Candidate> Candidates { get; init; } = Array.Empty<Candidate>();

    public Candidate Chosen { get; init; } = new();
}

public sealed class Candidate
{
    public int TokenId { get; init; }

    public string Text { get; init; } = string.Empty;

    public float Logit { get; init; }

    public float Probability { get; init; }
}
