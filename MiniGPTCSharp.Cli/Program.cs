using MiniGPTCSharp;
using System.Text;

var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "predict",
    "generate",
    "step",
    "learn",
    "inspect",
    "compare",
    "report"
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

switch (parsedArgs[0].ToLowerInvariant())
{
    case "predict":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintPredictHelp();
            return;
        }

        RunPredict(parsedArgs);
        return;

    case "generate":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintGenerateHelp();
            return;
        }

        RunGeneration(parsedArgs[1..]);
        return;

    case "step":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintStepHelp();
            return;
        }

        RunGeneration(["--step", .. parsedArgs[1..]]);
        return;

    case "learn":
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

    case "inspect":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintInspectHelp();
            return;
        }

        if (parsedArgs.Length <= 1)
        {
            PrintInspectHelp();
            Environment.ExitCode = 1;
            return;
        }

        RunInspectMode(parsedArgs[1], parsedArgs[2..]);
        return;

    case "compare":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintCompareHelp();
            return;
        }

        if (parsedArgs.Length <= 1)
        {
            PrintCompareHelp();
            Environment.ExitCode = 1;
            return;
        }

        RunCompareMode(parsedArgs[1], parsedArgs[2..]);
        return;

    case "report":
        if (HasHelpFlag(parsedArgs[1..]))
        {
            PrintReportHelp();
            return;
        }

        RunReportMode(parsedArgs[1..]);
        return;
}

static void RunGeneration(string[] args)
{
    var prompt = GetPrompt(args);
    var explain = args.Contains("--explain", StringComparer.OrdinalIgnoreCase);
    var stepMode = args.Contains("--step", StringComparer.OrdinalIgnoreCase);
    var showLogits = args.Contains("--show-logits", StringComparer.OrdinalIgnoreCase);
    var logitsTopN = ParseInt(GetOption(args, "--logits-topn"), 10);
    var logitsFormat = ParseLogitsFormat(GetOption(args, "--logits-format"));
    var deterministic = args.Contains("--deterministic", StringComparer.OrdinalIgnoreCase);
    var seed = ParseNullableInt(GetOption(args, "--seed"));
    var tokensRequested = ParseInt(GetOption(args, "--tokens") ?? GetOption(args, "--max-new-tokens"), 8);
    var model = new MiniGptModel(BuildConfig(args));

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
    var tokens = model.Tokenizer.Encode(prompt);
    Random? rng = deterministic ? null : seed.HasValue ? new Random(seed.Value) : new Random();

    Console.WriteLine("Step mode: generating one token at a time.");
    Console.WriteLine($"Start text: {model.Tokenizer.Decode(tokens)}");

    if (explain)
    {
        PrintPipelineIntro(prompt, tokens, model.Tokenizer, deterministic, seed);
    }

    for (var generated = 0; generated < tokensRequested; generated++)
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
    }

    Console.WriteLine("\n=== Final Output ===");
    Console.WriteLine(model.Tokenizer.Decode(tokens));
}

static void RunPredict(string[] args)
{
    var prompt = GetPrompt(args);
    var topN = ParseInt(GetOption(args, "--topn"), 5);
    var temperature = ParseFloat(GetOption(args, "--temp"), 1.0f);
    var topKFilter = ParseInt(GetOption(args, "--topk"), 0);
    var deterministic = args.Contains("--deterministic", StringComparer.OrdinalIgnoreCase);
    var explain = args.Contains("--explain", StringComparer.OrdinalIgnoreCase);
    var json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);

    if (json)
    {
        var export = MiniGptExports.BuildPredict(prompt, topN, temperature, topKFilter, deterministic);
        Console.WriteLine(MiniGptJson.Serialize(export));
        return;
    }

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

static void RunInspectMode(string topic, string[] args)
{
    var prompt = GetPrompt(args);
    var config = BuildConfig(args);
    var predictionTopN = ParseInt(GetOption(args, "--topn"), 5);
    var attentionTopN = ParseInt(GetOption(args, "--attention-topn"), 5);
    var dimsToShow = ParseInt(GetOption(args, "--dims"), Math.Min(8, config.EmbeddingSize));
    var json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
    var normalizedTopic = topic.ToLowerInvariant();

    if (normalizedTopic is not ("tokens" or "embeddings" or "attention" or "pipeline"))
    {
        Console.WriteLine($"Unknown inspect topic: {topic}");
        Console.WriteLine("Try: tokens, embeddings, attention, pipeline");
        Environment.ExitCode = 1;
        return;
    }

    if (json)
    {
        var export = MiniGptExports.BuildInspection(normalizedTopic, prompt, config, predictionTopN, attentionTopN, dimsToShow);
        Console.WriteLine(MiniGptJson.Serialize(export));
        return;
    }

    var model = new MiniGptModel(config);
    var inspection = model.InspectPrompt(prompt, predictionTopN, attentionTopN);

    switch (normalizedTopic)
    {
        case "tokens":
            PrintTokenInspection(inspection);
            return;
        case "embeddings":
            PrintTokenInspection(inspection);
            Console.WriteLine();
            PrintEmbeddingInspection(inspection, dimsToShow);
            return;
        case "attention":
            PrintTokenInspection(inspection);
            Console.WriteLine();
            PrintAttentionInspection(inspection);
            return;
        case "pipeline":
            PrintPipelineInspection(inspection, dimsToShow);
            return;
        default:
            Console.WriteLine($"Unknown inspect topic: {normalizedTopic}");
            Console.WriteLine("Try: tokens, embeddings, attention, pipeline");
            Environment.ExitCode = 1;
            return;
    }
}

static void RunCompareMode(string topic, string[] args)
{
    var prompt = GetPrompt(args);
    var tokensToGenerate = ParseInt(GetOption(args, "--tokens"), 8);
    var json = args.Contains("--json", StringComparer.OrdinalIgnoreCase);

    switch (topic.ToLowerInvariant())
    {
        case "sampling":
            RunSamplingComparison(prompt, tokensToGenerate, json);
            return;
        case "ablation":
            RunAblationComparison(prompt, tokensToGenerate, json);
            return;
        default:
            Console.WriteLine($"Unknown compare topic: {topic}");
            Console.WriteLine("Try: sampling, ablation");
            Environment.ExitCode = 1;
            return;
    }
}

static void RunSamplingComparison(string prompt, int tokensToGenerate, bool json = false)
{
    var comparison = MiniGptExports.BuildSamplingComparison(prompt, tokensToGenerate);

    if (json)
    {
        Console.WriteLine(MiniGptJson.Serialize(comparison));
        return;
    }

    Console.WriteLine("Comparison mode: sampling");
    Console.WriteLine($"Prompt: \"{prompt}\"");
    Console.WriteLine();
    Console.WriteLine("Top next-token beliefs:");
    for (var i = 0; i < comparison.TopPredictions.Count; i++)
    {
        Console.WriteLine($"  {i + 1}) {comparison.TopPredictions[i].TokenText,-10} p={comparison.TopPredictions[i].Probability:P2}");
    }

    Console.WriteLine();
    Console.WriteLine("Deterministic argmax:");
    Console.WriteLine($"  {comparison.Runs[0].Output}");

    Console.WriteLine("Seeded sampling (seed=42):");
    Console.WriteLine($"  {comparison.Runs[1].Output}");

    Console.WriteLine("Seeded sampling (seed=7):");
    Console.WriteLine($"  {comparison.Runs[2].Output}");

    Console.WriteLine();
    Console.WriteLine("What this teaches:");
    foreach (var note in comparison.Notes)
    {
        Console.WriteLine($"- {note}");
    }
}

static void RunAblationComparison(string prompt, int tokensToGenerate, bool json = false)
{
    var comparison = MiniGptExports.BuildAblationComparison(prompt, tokensToGenerate);

    if (json)
    {
        Console.WriteLine(MiniGptJson.Serialize(comparison));
        return;
    }

    Console.WriteLine("Comparison mode: ablation");
    Console.WriteLine($"Prompt: \"{prompt}\"");
    Console.WriteLine();

    foreach (var run in comparison.Runs)
    {
        Console.WriteLine($"{run.Label}:");
        Console.WriteLine($"  {run.Output}");
    }

    Console.WriteLine();
    Console.WriteLine("What this teaches:");
    foreach (var note in comparison.Notes)
    {
        Console.WriteLine($"- {note}");
    }
}

static void RunReportMode(string[] args)
{
    var prompt = GetPrompt(args);
    var outPath = GetOption(args, "--out") ?? "report.html";
    var tokensToGenerate = ParseInt(GetOption(args, "--tokens"), 8);
    var dimsToShow = ParseInt(GetOption(args, "--dims"), 8);
    var fullPath = Path.GetFullPath(outPath);
    var directory = Path.GetDirectoryName(fullPath);

    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    var report = MiniGptExports.BuildReport(prompt, tokensToGenerate, dimsToShow);
    var html = MiniGptHtmlReport.BuildHtml(report);
    File.WriteAllText(fullPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine($"Report written: {fullPath}");
}

static void RunLearnMode(string topic)
{
    switch (topic.ToLowerInvariant())
    {
        case "tokenization":
            Console.WriteLine("Learning mode: tokenization");
            RunInspectMode("tokens", ["--prompt", "The capital of France is Paris."]);
            return;

        case "embeddings":
            Console.WriteLine("Learning mode: embeddings");
            RunInspectMode("embeddings", ["--prompt", "AI model learning", "--layers", "0", "--dims", "8"]);
            return;

        case "attention":
            Console.WriteLine("Learning mode: attention");
            RunInspectMode("attention", ["--prompt", "The capital of France is Paris", "--attention-topn", "5"]);
            return;

        case "sampling":
            Console.WriteLine("Learning mode: sampling");
            RunCompareMode("sampling", ["--prompt", "The capital of France is", "--tokens", "8"]);
            return;

        case "ablation":
            Console.WriteLine("Learning mode: ablation");
            RunCompareMode("ablation", ["--prompt", "The capital of France is", "--tokens", "8"]);
            return;

        default:
            Console.WriteLine($"Unknown learning topic: {topic}");
            Console.WriteLine("Try: tokenization, embeddings, attention, sampling, ablation");
            Environment.ExitCode = 1;
            return;
    }
}

static void PrintTokenInspection(PromptInspection inspection)
{
    Console.WriteLine($"Prompt: \"{inspection.Prompt}\"");
    Console.WriteLine($"Token count: {inspection.Tokens.Count}");
    Console.WriteLine("Tokens:");
    foreach (var token in inspection.Tokens)
    {
        var source = token.WasAlreadyInVocabulary ? "seeded vocab" : "added at runtime";
        Console.WriteLine($"  pos={token.Position,-2} text={token.Text,-12} id={token.TokenId,-3} source={source}");
    }
}

static void PrintEmbeddingInspection(PromptInspection inspection, int dimsToShow)
{
    Console.WriteLine("Embedding preview:");
    foreach (var token in inspection.Tokens)
    {
        var preview = token.Embedding
            .Take(Math.Clamp(dimsToShow, 1, token.Embedding.Count))
            .Select(value => value.ToString("0.000"))
            .ToArray();
        Console.WriteLine($"  pos={token.Position,-2} text={token.Text,-12} [{string.Join(", ", preview)}]");
    }
}

static void PrintAttentionInspection(PromptInspection inspection)
{
    if (inspection.Layers.Count == 0)
    {
        Console.WriteLine("No transformer layers are active, so there are no attention weights to inspect.");
        return;
    }

    Console.WriteLine("Last-token attention by layer:");
    foreach (var layer in inspection.Layers)
    {
        Console.WriteLine($"Layer {layer.LayerIndex}:");
        foreach (var target in layer.TopTargets)
        {
            Console.WriteLine($"  token[{target.TokenIndex}]={target.TokenText,-12} weight={target.Weight:0.000}");
        }
    }
}

static void PrintPipelineInspection(PromptInspection inspection, int dimsToShow)
{
    PrintTokenInspection(inspection);
    Console.WriteLine();
    PrintEmbeddingInspection(inspection, dimsToShow);
    Console.WriteLine();
    PrintAttentionInspection(inspection);
    Console.WriteLine();
    Console.WriteLine("Top next-token predictions:");
    for (var i = 0; i < inspection.Predictions.Count; i++)
    {
        Console.WriteLine($"  {i + 1}) {inspection.Predictions[i].TokenText,-10} p={inspection.Predictions[i].Probability:P2}");
    }
}

static GptConfig BuildConfig(string[] args)
{
    return new GptConfig
    {
        LayerCount = ParseInt(GetOption(args, "--layers"), 2),
        TopK = ParseInt(GetOption(args, "--top-k"), 10),
        Temperature = ParseFloat(GetOption(args, "--temperature"), 0.8f),
        DisableAttention = args.Contains("--no-attention", StringComparer.OrdinalIgnoreCase),
        DisablePositionEmbeddings = args.Contains("--no-position", StringComparer.OrdinalIgnoreCase),
        DisableLayerNorm = args.Contains("--no-layernorm", StringComparer.OrdinalIgnoreCase)
    };
}

static string GetPrompt(string[] args)
{
    var prompt = GetOption(args, "--prompt");
    if (string.IsNullOrWhiteSpace(prompt))
    {
        var startIndex = args.Length > 0 && IsCommandToken(args[0]) ? 1 : 0;
        var firstValue = Array.FindIndex(args, startIndex, a => !a.StartsWith("--", StringComparison.Ordinal));
        if (firstValue >= 0)
        {
            prompt = args[firstValue];
        }
    }

    return string.IsNullOrWhiteSpace(prompt) ? "The capital of France is" : prompt;
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

static bool IsCommandToken(string arg)
    => arg.Equals("predict", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("generate", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("step", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("learn", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("inspect", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("compare", StringComparison.OrdinalIgnoreCase)
       || arg.Equals("report", StringComparison.OrdinalIgnoreCase);

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
    Console.WriteLine("  step --prompt text [--tokens n] [--temperature n] [--top-k n] [--layers n] [--seed n] [--deterministic] [--explain] [--show-logits]");
    Console.WriteLine("  predict --prompt text [--topn N] [--temp T] [--topk K] [--deterministic] [--explain]");
    Console.WriteLine("  inspect tokens|embeddings|attention|pipeline --prompt text [--json]");
    Console.WriteLine("  compare sampling|ablation --prompt text [--tokens n] [--json]");
    Console.WriteLine("  report --prompt text --out report.html");
    Console.WriteLine("  learn tokenization|embeddings|attention|sampling|ablation");
    Console.WriteLine("Use --help or -h with any command for command-specific help.");
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
    Console.WriteLine("predict --prompt text [--topn N] [--temp T] [--topk K] [--deterministic] [--explain] [--json]");
}

static void PrintInspectHelp()
{
    Console.WriteLine("inspect tokens|embeddings|attention|pipeline --prompt text [--dims N] [--topn N] [--attention-topn N] [--layers n] [--json]");
}

static void PrintCompareHelp()
{
    Console.WriteLine("compare sampling|ablation --prompt text [--tokens n] [--json]");
}

static void PrintReportHelp()
{
    Console.WriteLine("report --prompt text --out report.html [--tokens n] [--dims n]");
}

static void PrintLearnHelp()
{
    Console.WriteLine("learn tokenization|embeddings|attention|sampling|ablation");
}

enum LogitsDisplayFormat
{
    Raw,
    Centered,
    Scaled
}
