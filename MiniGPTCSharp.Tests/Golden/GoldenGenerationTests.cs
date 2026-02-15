using MiniGPTCSharp;

namespace MiniGPTCSharp.Tests.Golden;

public static class GoldenGenerationTests
{
    public static IReadOnlyList<GoldenGenerationTestCase> Cases =>
    [
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
    ];

    public static void RunAll()
    {
        foreach (var testCase in Cases)
        {
            RunGoldenTest(testCase);
        }
    }

    private static void RunGoldenTest(GoldenGenerationTestCase testCase)
    {
        var model = new MiniGptModel();

        var actual = model.Generate(
            testCase.Prompt,
            explain: false,
            maxNewTokens: testCase.Tokens,
            seed: testCase.Seed,
            deterministic: testCase.Deterministic);

        if (string.Equals(testCase.ExpectedOutput, actual, StringComparison.Ordinal))
        {
            Console.WriteLine($"PASS: {testCase.Prompt}");
            return;
        }

        var message =
            $"FAIL: Golden output mismatch for prompt '{testCase.Prompt}'.{Environment.NewLine}" +
            $"Expected: {testCase.ExpectedOutput}{Environment.NewLine}" +
            $"Actual:   {actual}";

        throw new InvalidOperationException(message);
    }
}
