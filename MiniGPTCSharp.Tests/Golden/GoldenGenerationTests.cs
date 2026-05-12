using MiniGPTCSharp;
using Xunit;

namespace MiniGPTCSharp.Tests.Golden;

public sealed class GoldenGenerationTests
{
    public static TheoryData<GoldenGenerationTestCase> Cases =>
        new()
        {
            new GoldenGenerationTestCase
            {
                Prompt = "Hello my name is",
                Seed = 42,
                Tokens = 12,
                Deterministic = true,
                ExpectedOutput = "Hello my name is name France Paris word next token learning name name name name it"
            },
            new GoldenGenerationTestCase
            {
                Prompt = "The capital of France is",
                Seed = 42,
                Tokens = 12,
                Deterministic = true,
                ExpectedOutput = "the capital of France is capital Paris model next token learning learning learning to next capital AI"
            },
            new GoldenGenerationTestCase
            {
                Prompt = "Once upon a time",
                Seed = 42,
                Tokens = 12,
                Deterministic = true,
                ExpectedOutput = "Once upon a time France France capital model token token learning a time time time France"
            }
        };

    [Theory]
    [MemberData(nameof(Cases))]
    public void GenerateMatchesGoldenOutput(GoldenGenerationTestCase testCase)
    {
        var model = new MiniGptModel();

        var actual = model.Generate(
            testCase.Prompt,
            explain: false,
            maxNewTokens: testCase.Tokens,
            seed: testCase.Seed,
            deterministic: testCase.Deterministic);

        Assert.Equal(testCase.ExpectedOutput, actual);
    }
}
