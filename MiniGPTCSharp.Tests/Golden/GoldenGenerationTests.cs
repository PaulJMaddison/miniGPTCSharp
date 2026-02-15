using MiniGPTCSharp;
using Xunit.Abstractions;

namespace MiniGPTCSharp.Tests.Golden;

public class GoldenGenerationTests
{
    private readonly ITestOutputHelper _output;

    public GoldenGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> Cases =>
    [
        [new GoldenGenerationTestCase
        {
            Prompt = "Hello my name is",
            Seed = 42,
            Tokens = 12,
            Deterministic = true,
            ExpectedOutput = "Hello my name is name France Paris word next token learning name name name name it"
        }],
        [new GoldenGenerationTestCase
        {
            Prompt = "The capital of France is",
            Seed = 42,
            Tokens = 12,
            Deterministic = true,
            ExpectedOutput = "the capital of France is capital Paris model next token learning learning learning to next capital AI"
        }],
        [new GoldenGenerationTestCase
        {
            Prompt = "Once upon a time",
            Seed = 42,
            Tokens = 12,
            Deterministic = true,
            ExpectedOutput = "Once upon a time France France capital model token token learning a time time time France"
        }]
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void Generate_DeterministicGoldenOutput_MatchesBaseline(GoldenGenerationTestCase testCase)
    {
        RunGoldenTest(testCase);
    }

    private void RunGoldenTest(GoldenGenerationTestCase testCase)
    {
        var model = new MiniGptModel();

        var actual = model.Generate(
            testCase.Prompt,
            explain: false,
            maxNewTokens: testCase.Tokens,
            seed: testCase.Seed,
            deterministic: testCase.Deterministic);

        if (!string.Equals(testCase.ExpectedOutput, actual, StringComparison.Ordinal))
        {
            _output.WriteLine($"Prompt: {testCase.Prompt}");
            _output.WriteLine($"Expected: {testCase.ExpectedOutput}");
            _output.WriteLine($"Actual:   {actual}");
        }

        Assert.Equal(testCase.ExpectedOutput, actual);
    }
}
