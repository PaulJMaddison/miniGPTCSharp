using MiniGPTCSharp;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

if (args[0].Equals("predict", StringComparison.OrdinalIgnoreCase))
{
    var prompt = args.Length > 1 ? args[1] : "The capital of France is";
    RunPredict(prompt);
    return;
}

if (args[0].Equals("learn", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
{
    RunLearnMode(args[1]);
    return;
}

if (args.Contains("--demo-sampling", StringComparer.OrdinalIgnoreCase))
{
    var prompt = GetOption(args, "--prompt") ?? "The capital of France is";
    RunSamplingDemo(prompt);
    return;
}

RunGeneration(args);

static void RunGeneration(string[] args)
{
    var prompt = GetOption(args, "--prompt") ?? "The capital of France is";
    var explain = args.Contains("--explain", StringComparer.OrdinalIgnoreCase);

    var config = new GptConfig
    {
        LayerCount = ParseInt(GetOption(args, "--layers"), 2),
        TopK = ParseInt(GetOption(args, "--top-k"), 10),
        Temperature = ParseFloat(GetOption(args, "--temperature"), 0.8f),
        DisableAttention = args.Contains("--no-attention", StringComparer.OrdinalIgnoreCase),
        DisablePositionEmbeddings = args.Contains("--no-position", StringComparer.OrdinalIgnoreCase),
        DisableLayerNorm = args.Contains("--no-layernorm", StringComparer.OrdinalIgnoreCase)
    };

    var maxNewTokens = ParseInt(GetOption(args, "--max-new-tokens"), 8);
    var model = new MiniGptModel(config);
    var output = model.Generate(prompt, explain, maxNewTokens);

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(output);
}

static void RunPredict(string prompt)
{
    var model = new MiniGptModel();
    var predictions = model.PredictNextTokens(prompt, 5);

    Console.WriteLine($"Prompt: {prompt}");
    Console.WriteLine("Top 5 next-token predictions:");

    foreach (var item in predictions)
    {
        Console.WriteLine($"- {item.Key,-15} {item.Value:P2}");
    }
}

static void RunSamplingDemo(string prompt)
{
    var temperatures = new[] { 0.2f, 0.8f, 1.5f };
    var topKs = new[] { 1, 10, 50 };

    Console.WriteLine($"Sampling demo prompt: {prompt}\n");

    foreach (var temp in temperatures)
    {
        foreach (var topK in topKs)
        {
            var model = new MiniGptModel(new GptConfig { Temperature = temp, TopK = topK, LayerCount = 2 }, seed: 777);
            var generated = model.Generate(prompt, explain: false, maxNewTokens: 6);
            Console.WriteLine($"temperature={temp:0.0}, top-k={topK,-2} => {generated}");
        }

        Console.WriteLine();
    }
}

static void RunLearnMode(string topic)
{
    switch (topic.ToLowerInvariant())
    {
        case "attention":
            Console.WriteLine("Learning mode: attention");
            Console.WriteLine("We will run one generation step and show which earlier tokens the model focuses on.");
            RunGeneration(new[] { "--prompt", "The capital of France is", "--explain", "--max-new-tokens", "1" });
            break;

        case "embeddings":
            Console.WriteLine("Learning mode: embeddings");
            Console.WriteLine("Embeddings convert token IDs into vectors that the model can compare mathematically.");
            RunGeneration(new[] { "--prompt", "AI model learning", "--explain", "--layers", "0", "--max-new-tokens", "1" });
            break;

        case "sampling":
            Console.WriteLine("Learning mode: sampling");
            Console.WriteLine("Sampling chooses the next token from probabilities instead of always picking the top one.");
            RunSamplingDemo("The capital of France is");
            break;

        default:
            Console.WriteLine($"Unknown learning topic: {topic}");
            Console.WriteLine("Try: attention, embeddings, sampling");
            break;
    }
}

static string? GetOption(string[] args, string option)
{
    var index = Array.FindIndex(args, a => a.Equals(option, StringComparison.OrdinalIgnoreCase));
    if (index >= 0 && index + 1 < args.Length)
    {
        return args[index + 1];
    }

    return null;
}

static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

static float ParseFloat(string? value, float fallback) => float.TryParse(value, out var parsed) ? parsed : fallback;

static void PrintHelp()
{
    Console.WriteLine("MiniGPTSharp learning CLI");
    Console.WriteLine("Commands:");
    Console.WriteLine("  predict \"The capital of France is\"");
    Console.WriteLine("  learn attention|embeddings|sampling");
    Console.WriteLine("  --demo-sampling [--prompt text]");
    Console.WriteLine("  --prompt text [--explain] [--temperature n] [--top-k n] [--layers n] [--max-new-tokens n]");
    Console.WriteLine("Break-the-model flags:");
    Console.WriteLine("  --no-attention --no-position --no-layernorm");
}
