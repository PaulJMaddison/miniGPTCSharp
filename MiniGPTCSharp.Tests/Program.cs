using MiniGPTCSharp;

var failures = new List<string>();

AssertEqual(0, TokenCounter.CountWords(null!), "Null returns 0", failures);
AssertEqual(0, TokenCounter.CountWords("   \t\n"), "Whitespace returns 0", failures);
AssertEqual(1, TokenCounter.CountWords("hello"), "Single word", failures);
AssertEqual(3, TokenCounter.CountWords("mini GPT CSharp"), "Three words", failures);
AssertEqual(2, TokenCounter.CountWords("  padded   words  "), "Ignores extra spaces", failures);

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
