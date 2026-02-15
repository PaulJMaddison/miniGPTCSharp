using MiniGPTCSharp;

var failures = new List<string>();

AssertEqual(0, TokenCounter.CountWords(null!), "Null returns 0", failures);
AssertEqual(0, TokenCounter.CountWords("   \t\n"), "Whitespace returns 0", failures);
AssertEqual(1, TokenCounter.CountWords("hello"), "Single word", failures);
AssertEqual(3, TokenCounter.CountWords("mini GPT CSharp"), "Three words", failures);
AssertEqual(2, TokenCounter.CountWords("  padded   words  "), "Ignores extra spaces", failures);

var model = new MiniGptModel(new GptConfig { LayerCount = 0, TopK = 5, Temperature = 0.7f });
var tokens = model.Tokenizer.Encode("The capital of France is");
var step = model.Step(tokens, temperature: 0.7f, topK: 5, explain: true);

AssertTrue(step.NextTokenId >= 0, "Step returns selected token", failures);
AssertTrue(step.TopK.Count > 0, "Step returns top-k probabilities", failures);
AssertTrue(step.DebugText.Contains("Current length", StringComparison.Ordinal), "Step explain text appears", failures);
AssertEqual(5, step.TopKValue, "Step top-k value stored", failures);

var greedyStep = model.Step(tokens, temperature: 0f, topK: 5, explain: true);
AssertTrue(greedyStep.DebugText.Contains("greedy", StringComparison.OrdinalIgnoreCase), "Greedy explanation appears", failures);

var predictions = model.PredictNextTokens("The capital of France is", topN: 5, temperature: 1.0f, topKFilter: 0);
AssertEqual(5, predictions.Count, "Predict returns top 5", failures);
AssertTrue(predictions.All(p => p.Probability >= 0f), "Predict probabilities are non-negative", failures);
AssertTrue(predictions.Zip(predictions.Skip(1), (a, b) => a.Probability >= b.Probability).All(x => x), "Predict list is sorted descending", failures);

var filteredPredictions = model.PredictNextTokens("The capital of France is", topN: 3, temperature: 0f, topKFilter: 2);
AssertEqual(3, filteredPredictions.Count, "Predict topN works", failures);
AssertTrue(filteredPredictions.Count(p => p.Probability > 0f) <= 2, "Predict top-k filter masks non-top tokens", failures);

if (failures.Count == 0)
{
    Console.WriteLine("All tests passed.");
    return;
}

Console.Error.WriteLine("Tests failed:");
foreach (var failure in failures)
{
    Console.Error.WriteLine($"- {failure}");
}

Environment.ExitCode = 1;

static void AssertEqual(int expected, int actual, string name, List<string> failures)
{
    if (expected != actual)
    {
        failures.Add($"{name}: expected {expected}, got {actual}");
    }
}

static void AssertTrue(bool condition, string name, List<string> failures)
{
    if (!condition)
    {
        failures.Add($"{name}: expected true");
    }
}
