using MiniGPTCSharp;

var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "predict",
    "generate",
    "step",
    "learn"
};

var parsedArgs = StripDirectives(args);

if (parsedArgs.Length == 0 || IsRootHelp(parsedArgs))
{
    PrintHelp();
    Environment.ExitCode = 0;
    return;
}

if (!commands.Contains(parsedArgs[0]))
{
    PrintHelp();
    Console.Error.WriteLine($"Unknown command: {parsedArgs[0]}");
    Environment.ExitCode = 1;
    return;
}

if (parsedArgs[0].Equals("predict", StringComparison.OrdinalIgnoreCase))
{
    if (HasHelpFlag(parsedArgs[1..]))
    {
        PrintPredictHelp();
        return;
    }

    RunPredict(parsedArgs);
    return;
}

if (parsedArgs[0].Equals("generate", StringComparison.OrdinalIgnoreCase))
{
    if (HasHelpFlag(parsedArgs[1..]))
    {
        PrintGenerateHelp();
        return;
    }

    RunGeneration(parsedArgs[1..]);
    return;
}

if (parsedArgs[0].Equals("step", StringComparison.OrdinalIgnoreCase))
{
    if (HasHelpFlag(parsedArgs[1..]))
    {
        PrintStepHelp();
        return;
    }

    RunGeneration(["--step", .. parsedArgs[1..]]);
    return;
}

if (parsedArgs[0].Equals("learn", StringComparison.OrdinalIgnoreCase))
{
    if (HasHelpFlag(parsedArgs[1..]))
    {
        PrintLearnHelp();
        return;
    }

    if (parsedArgs.Length <= 1)
    {
        PrintLearnHelp();
        Environment.ExitCode = 1;
        return;
    }

    RunLearnMode(parsedArgs[1]);
    return;
}

static void RunGeneration(string[] args)
{
    var prompt = GetOption(args, "--prompt") ?? "The capital of France is";
    var explain = args.Contains("--explain", StringComparer.OrdinalIgnoreCase);
    var stepMode = args.Contains("--step", StringComparer.OrdinalIgnoreCase);
    var showLogits = args.Contains("--show-logits", StringComparer.OrdinalIgnoreCase);
    var logitsTopN = ParseInt(GetOption(args, "--logits-topn"), 10);
    var logitsFormat = ParseLogitsFormat(GetOption(args, "--logits-format"));
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

    var tokensRequested = ParseInt(GetOption(args, "--tokens") ?? GetOption(args, "--max-new-tokens"), 8);
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
        RunStepMode(model, prompt, tokensRequested, explain, seed, deterministic, showLogits, logitsTopN, logitsFormat);
        return;
    }

    if (explain)
    {
        RunNarratedGeneration(model, prompt, tokensRequested, seed, deterministic, showLogits, logitsTopN, logitsFormat);
        return;
    }

    var output = model.Generate(prompt, explain, tokensRequested, seed, deterministic);

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(output);
}

static void RunNarratedGeneration(
    MiniGptModel model,
    string prompt,
    int tokensRequested,
    int? seed,
    bool deterministic,
    bool showLogits,
    int logitsTopN,
    LogitsDisplayFormat logitsFormat)
{
    var tokens = model.Tokenizer.Encode(prompt);
    Random? rng = deterministic ? null : seed.HasValue ? new Random(seed.Value) : new Random();

    PrintPipelineIntro(prompt, tokens, model.Tokenizer, deterministic, seed);

    for (var generated = 0; generated < tokensRequested; generated++)
    {
        var step = model.Step(
            tokens,
            model.Config.Temperature,
            model.Config.TopK,
            explain: true,
            deterministic: deterministic,
            samplingRandom: rng);

        Console.WriteLine($"\n--- Generation Step {generated + 1} ---");
        PrintNarratedStep(step.DebugInfo, deterministic, commandName: "generate");

        if (showLogits)
        {
            PrintLogitsSection(step.DebugInfo, logitsTopN, logitsFormat);
        }

        tokens.Add(step.NextTokenId);
        Console.WriteLine($"Text after step {generated + 1}: {model.Tokenizer.Decode(tokens)}");
    }

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(model.Tokenizer.Decode(tokens));
}

static void RunStepMode(
    MiniGptModel model,
    string prompt,
    int tokensRequested,
    bool explain,
    int? seed,
    bool deterministic,
    bool showLogits,
    int logitsTopN,
    LogitsDisplayFormat logitsFormat)
{
    // Step mode is just the autocomplete loop with one explicit Step(...) call per token.
    var tokens = model.Tokenizer.Encode(prompt);
    Random? rng = deterministic ? null : seed.HasValue ? new Random(seed.Value) : new Random();

    Console.WriteLine("Step mode: generating one token at a time.");
    Console.WriteLine($"Start text: {model.Tokenizer.Decode(tokens)}");

    if (explain)
    {
        PrintPipelineIntro(prompt, tokens, model.Tokenizer, deterministic, seed);
    }

    var newTokensToGenerate = tokensRequested;
    var generated = 0;

    while (generated < newTokensToGenerate)
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
            Console.WriteLine($"\n--- Generation Step {generated + 1} ---");
            PrintNarratedStep(step.DebugInfo, deterministic, commandName: "step");
            if (showLogits)
            {
                PrintLogitsSection(step.DebugInfo, logitsTopN, logitsFormat);
            }
        }
        else if (showLogits)
        {
            Console.WriteLine($"\n--- Generation Step {generated + 1} ---");
            PrintLogitsSection(step.DebugInfo, logitsTopN, logitsFormat);
        }

        Console.WriteLine($"Text after step {generated + 1}: {model.Tokenizer.Decode(tokens)}");
        generated++;
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
    var explain = args.Contains("--explain", StringComparer.OrdinalIgnoreCase);

    var model = new MiniGptModel();
    var predictions = model.PredictNextTokens(prompt, topN, temperature, topKFilter);
    var tokens = model.Tokenizer.Encode(prompt);

    if (explain)
    {
        PrintPipelineIntro(prompt, tokens, model.Tokenizer, deterministic, seed: null);

        var explainTopK = topKFilter > 0 ? topKFilter : model.Tokenizer.Vocabulary.Count;
        var decisionView = model.Step(tokens, temperature, explainTopK, explain: true, deterministic: deterministic, seed: 1234);
        PrintNarratedStep(decisionView.DebugInfo, deterministic, commandName: "predict");
    }

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
            RunGeneration(new[] { "--prompt", "The capital of France is", "--explain", "--tokens", "1" });
            break;

        case "embeddings":
            Console.WriteLine("Learning mode: embeddings");
            Console.WriteLine("Embeddings convert token IDs into vectors that the model can compare mathematically.");
            RunGeneration(new[] { "--prompt", "AI model learning", "--explain", "--layers", "0", "--tokens", "1" });
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

static bool HasHelpFlag(string[] args)
    => args.Contains("--help", StringComparer.OrdinalIgnoreCase)
       || args.Contains("-h", StringComparer.OrdinalIgnoreCase);

static bool IsDirectiveToken(string arg)
    => arg.StartsWith("[", StringComparison.Ordinal) && arg.EndsWith("]", StringComparison.Ordinal);

static string[] StripDirectives(string[] args)
    => args.Where(arg => !IsDirectiveToken(arg)).ToArray();

static bool IsRootHelp(string[] args)
    => args.Length > 0
       && (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase)
           || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase)
           || args[0].Equals("help", StringComparison.OrdinalIgnoreCase));

static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

static int? ParseNullableInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;

static float ParseFloat(string? value, float fallback) => float.TryParse(value, out var parsed) ? parsed : fallback;

static LogitsDisplayFormat ParseLogitsFormat(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return LogitsDisplayFormat.Raw;
    }

    return value.ToLowerInvariant() switch
    {
        "raw" => LogitsDisplayFormat.Raw,
        "centered" => LogitsDisplayFormat.Centered,
        "scaled" => LogitsDisplayFormat.Scaled,
        _ => LogitsDisplayFormat.Raw
    };
}



static void PrintPipelineIntro(string prompt, IReadOnlyList<int> tokens, VocabularyTokenizer tokenizer, bool deterministic, int? seed)
{
    Console.WriteLine("\n1) Input received");
    Console.WriteLine($"The command received this prompt: \"{prompt}\".");
    Console.WriteLine($"The current context contains {tokens.Count} token(s).");

    Console.WriteLine("\n2) Tokenization");
    Console.WriteLine($"Token pieces: [{string.Join(", ", tokenizer.SplitTokens(prompt))}]");
    Console.WriteLine($"Token IDs: [{string.Join(", ", tokens)}]");
    Console.WriteLine("-----------------------------------------");
    Console.WriteLine("The model cannot process text directly.");
    Console.WriteLine();
    Console.WriteLine("Your prompt is split into smaller pieces");
    Console.WriteLine("called tokens (words or sub-words).");
    Console.WriteLine();
    Console.WriteLine("Each token is converted into a number ID");
    Console.WriteLine("that represents it in the vocabulary.");
    Console.WriteLine();
    Console.WriteLine("These IDs are what the model uses as input.");
    Console.WriteLine("-----------------------------------------");

    if (deterministic)
    {
        Console.WriteLine("Decision mode: deterministic argmax (no randomness).");
    }
    else if (seed.HasValue)
    {
        Console.WriteLine($"Decision mode: probabilistic sampling with random seed {seed.Value}.");
    }
    else
    {
        Console.WriteLine("Decision mode: probabilistic sampling with a fresh random seed.");
    }
}

static void PrintNarratedStep(StepDebugInfo debugInfo, bool deterministic, string commandName)
{
    Console.WriteLine("3) Model forward pass");
    Console.WriteLine("The model runs the token IDs through embeddings and transformer layers to build a context-aware internal state.");

    Console.WriteLine("\n4) Logits produced");
    Console.WriteLine("-----------------------------------------");
    Console.WriteLine("The model has now produced a score for");
    Console.WriteLine("every possible next token.");
    Console.WriteLine();
    Console.WriteLine("These scores are called logits.");
    Console.WriteLine("Higher logits mean the model currently prefers that token more.");
    Console.WriteLine("-----------------------------------------");

    Console.WriteLine("\n5) Softmax → probabilities");
    Console.WriteLine($"Softmax converts logits into probabilities that sum to 1.0 (temperature={debugInfo.Temperature:0.###}, top-k={debugInfo.TopK}).");

    Console.WriteLine("\nTop candidates:");
    foreach (var candidate in debugInfo.Candidates)
    {
        Console.WriteLine($"  id={candidate.TokenId,-3} text={candidate.Text,-10} logit={candidate.Logit,8:0.0000}  p={candidate.Probability,7:P2}");
    }

    if (commandName.Equals("predict", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("\n6) Decision step: none (predict only reports probabilities)");
        Console.WriteLine("No token is chosen or appended in predict mode");
        return;
    }

    Console.WriteLine("\n6) Sampling or argmax decision");
    if (deterministic)
    {
        Console.WriteLine("Deterministic mode chooses the highest-probability token every time (argmax).");
    }
    else
    {
        Console.WriteLine("Sampling mode rolls randomness using the probability distribution.");
        Console.WriteLine("Higher-probability tokens are more likely, but lower-probability tokens can still be chosen.");
    }

    Console.WriteLine($"Chosen token: id={debugInfo.Chosen.TokenId}, text={debugInfo.Chosen.Text}");

    Console.WriteLine("\n7) Token appended");
    Console.WriteLine("The chosen token is appended to the context so the model can use it on the next step.");

    Console.WriteLine("\n8) Repeat");
    Console.WriteLine($"The {commandName} command repeats this loop until it reaches the requested token count.");
}

static void PrintLogitsSection(StepDebugInfo debugInfo, int logitsTopN, LogitsDisplayFormat logitsFormat)
{
    var candidatesToShow = debugInfo.Candidates.Take(Math.Clamp(logitsTopN, 1, debugInfo.Candidates.Count)).ToList();
    var maxLogit = candidatesToShow.Max(c => c.Logit);
    Console.WriteLine();
    Console.WriteLine($"Logits (pre-softmax) for top {candidatesToShow.Count}:");

    foreach (var candidate in candidatesToShow)
    {
        var displayedLogit = logitsFormat switch
        {
            LogitsDisplayFormat.Raw => candidate.Logit,
            LogitsDisplayFormat.Centered => candidate.Logit - maxLogit,
            LogitsDisplayFormat.Scaled => (candidate.Logit - maxLogit) / SafeTemperature(debugInfo.Temperature),
            _ => candidate.Logit
        };

        Console.WriteLine($"  id={candidate.TokenId,-3} text={candidate.Text,-10} logit={displayedLogit,8:0.0000}");
    }

    if (logitsFormat == LogitsDisplayFormat.Centered)
    {
        Console.WriteLine("logit_centered = logit - max_logit (so best token is 0)");
    }
    else if (logitsFormat == LogitsDisplayFormat.Scaled)
    {
        Console.WriteLine("logit_scaled = (logit - max_logit) / temperature");
    }
}

static float SafeTemperature(float temperature) => temperature <= 0f ? 1f : temperature;

static void PrintHelp()
{
    Console.WriteLine("MiniGPTSharp learning CLI");
    Console.WriteLine("Commands:");
    Console.WriteLine("  generate --prompt text [--tokens n] [--temperature n] [--top-k n] [--layers n] [--seed n] [--deterministic] [--explain]");
    Console.WriteLine("  step --prompt text [--tokens n] [--temperature n] [--top-k n] [--layers n] [--seed n] [--deterministic] [--explain] [--show-logits] [--logits-topn n] [--logits-format raw|centered|scaled]");
    Console.WriteLine("  predict --prompt \"The capital of France is\" [--topn N] [--temp T] [--topk K] [--deterministic] [--explain]");
    Console.WriteLine("  learn attention|embeddings|sampling");
    Console.WriteLine("Use --help or -h with any command for command-specific help.");
    Console.WriteLine("System.CommandLine-style directives such as [diagram] are accepted.");
    Console.WriteLine("Break-the-model flags:");
    Console.WriteLine("  --no-attention --no-position --no-layernorm");
}

static void PrintGenerateHelp()
{
    Console.WriteLine("generate --prompt text [--tokens n|--max-new-tokens n] [--temperature n] [--top-k n] [--layers n] [--seed n] [--deterministic] [--explain]");
    Console.WriteLine("         [--show-logits] [--logits-topn n] [--logits-format raw|centered|scaled] [--no-attention] [--no-position] [--no-layernorm]");
}

static void PrintStepHelp()
{
    Console.WriteLine("step --prompt text [--tokens n|--max-new-tokens n] [--temperature n] [--top-k n] [--layers n] [--seed n] [--deterministic] [--explain]");
    Console.WriteLine("     [--show-logits] [--logits-topn n] [--logits-format raw|centered|scaled] [--no-attention] [--no-position] [--no-layernorm]");
}

static void PrintPredictHelp()
{
    Console.WriteLine("predict --prompt text [--topn N] [--temp T] [--topk K] [--deterministic] [--explain]");
}

static void PrintLearnHelp()
{
    Console.WriteLine("learn attention|embeddings|sampling");
}

enum LogitsDisplayFormat
{
    Raw,
    Centered,
    Scaled
}
