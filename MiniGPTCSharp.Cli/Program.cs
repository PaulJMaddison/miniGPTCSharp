using MiniGPTCSharp;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

if (args[0].Equals("predict", StringComparison.OrdinalIgnoreCase))
{
    RunPredict(args);
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
    var stepMode = args.Contains("--step", StringComparer.OrdinalIgnoreCase);
    var deterministic = args.Contains("--deterministic", StringComparer.OrdinalIgnoreCase);
    var seed = ParseNullableInt(GetOption(args, "--seed"));

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

    if (deterministic)
    {
        Console.WriteLine("Generation Mode: Deterministic (Greedy ArgMax)");
    }
    else if (seed.HasValue)
    {
        Console.WriteLine($"Random Seed: {seed.Value}");
    }

    if (stepMode)
    {
        RunStepMode(model, prompt, maxNewTokens, explain, seed, deterministic);
        return;
    }

    var output = model.Generate(prompt, explain, maxNewTokens, seed, deterministic);

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(output);
}

static void RunStepMode(
    MiniGptModel model,
    string prompt,
    int maxNewTokens,
    bool explain,
    int? seed,
    bool deterministic)
{
    // Step mode is just the autocomplete loop with one explicit Step(...) call per token.
    var tokens = model.Tokenizer.Encode(prompt);
    Random? rng = deterministic ? null : seed.HasValue ? new Random(seed.Value) : new Random();

    Console.WriteLine("Step mode: generating one token at a time.");
    Console.WriteLine($"Start text: {model.Tokenizer.Decode(tokens)}");

    for (var i = 0; i < maxNewTokens; i++)
    {
        var step = model.Step(
            tokens,
            model.Config.Temperature,
            model.Config.TopK,
            explain,
            deterministic: deterministic,
            samplingRandom: rng);
        tokens.Add(step.NextTokenId);

        if (explain && !string.IsNullOrWhiteSpace(step.DebugText))
        {
            Console.WriteLine($"\n--- Step {i + 1} ---");
            Console.Write(step.DebugText);
        }

        Console.WriteLine($"Text after step {i + 1}: {model.Tokenizer.Decode(tokens)}");
    }

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(model.Tokenizer.Decode(tokens));
}

static void RunPredict(string[] args)
{
    var prompt = GetOption(args, "--prompt");
    if (string.IsNullOrWhiteSpace(prompt) && args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal))
    {
        prompt = args[1];
    }

    prompt ??= "The capital of France is";

    var topN = ParseInt(GetOption(args, "--topn"), 5);
    var temperature = ParseFloat(GetOption(args, "--temp"), 1.0f);
    var topKFilter = ParseInt(GetOption(args, "--topk"), 0);
    var deterministic = args.Contains("--deterministic", StringComparer.OrdinalIgnoreCase);

    var model = new MiniGptModel();
    var predictions = model.PredictNextTokens(prompt, topN, temperature, topKFilter);

    if (deterministic)
    {
        Console.WriteLine("Generation Mode: Deterministic (Greedy ArgMax)");
    }

    Console.WriteLine($"Prompt: \"{prompt}\"");
    Console.WriteLine($"Next-token predictions (top {predictions.Count}):");

    for (var i = 0; i < predictions.Count; i++)
    {
        var prediction = predictions[i];
        Console.WriteLine($"  {i + 1}) \"{prediction.TokenText}\" (id={prediction.TokenId}) p={prediction.Probability:0.00}");
    }

    if (temperature <= 0f)
    {
        Console.WriteLine("Note: --temp must be > 0. Using 1.0 instead.");
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
            var generated = model.Generate(prompt, explain: false, maxNewTokens: 6, seed: 777);
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

static int? ParseNullableInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;

static float ParseFloat(string? value, float fallback) => float.TryParse(value, out var parsed) ? parsed : fallback;

static void PrintHelp()
{
    Console.WriteLine("MiniGPTSharp learning CLI");
    Console.WriteLine("Commands:");
    Console.WriteLine("  predict --prompt \"The capital of France is\" [--topn N] [--temp T] [--topk K] [--deterministic]");
    Console.WriteLine("  learn attention|embeddings|sampling");
    Console.WriteLine("  --demo-sampling [--prompt text]");
    Console.WriteLine("  --prompt text [--step] [--explain] [--temperature n] [--top-k n] [--layers n] [--max-new-tokens n] [--seed n] [--deterministic]");
    Console.WriteLine("Break-the-model flags:");
    Console.WriteLine("  --no-attention --no-position --no-layernorm");
}
