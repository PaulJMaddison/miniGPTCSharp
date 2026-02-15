namespace MiniGPTCSharp;

public sealed class NextTokenPrediction
{
    public int TokenId { get; init; }

    public string TokenText { get; init; } = string.Empty;

    public float Probability { get; init; }
}
