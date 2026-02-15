namespace MiniGPTCSharp.Tests.Golden;

public class GoldenGenerationTestCase
{
    public required string Prompt { get; init; }
    public required int Seed { get; init; }
    public required int Tokens { get; init; }
    public required bool Deterministic { get; init; }
    public required string ExpectedOutput { get; init; }
}
