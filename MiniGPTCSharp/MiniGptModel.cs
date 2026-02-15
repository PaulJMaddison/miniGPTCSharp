using System.Text;

namespace MiniGPTCSharp;

public class MiniGptModel
{
    private readonly List<TransformerBlock> _layers;

    public MiniGptModel(GptConfig? config = null, int seed = 1234)
    {
        Config = config ?? new GptConfig();
        Tokenizer = new VocabularyTokenizer();
        _layers = Enumerable.Range(0, Math.Max(0, Config.LayerCount)).Select(i => new TransformerBlock(i)).ToList();
    }

    public GptConfig Config { get; }

    public VocabularyTokenizer Tokenizer { get; }

    public string Generate(
        string prompt,
        bool explain = false,
        int maxNewTokens = 10,
        int? seed = null,
        bool deterministic = false)
    {
        var tokens = Tokenizer.Encode(prompt);
        // Important for reproducible learning demos:
        // create ONE RNG for the full generation run so each token draw advances
        // the same sequence. Re-creating Random inside the loop can cause repeated
        // draws and can make different seeds look identical.
        Random? rng = deterministic ? null : seed.HasValue ? new Random(seed.Value) : new Random();

        if (explain)
        {
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine($"Tokenized Input: [{string.Join(", ", Tokenizer.SplitTokens(prompt))}]");
            Console.WriteLine($"Token IDs: [{string.Join(", ", tokens)}]");

            if (deterministic)
            {
                Console.WriteLine("Generation Mode: Deterministic (Greedy ArgMax)");
            }
            else if (seed.HasValue)
            {
                Console.WriteLine($"Random Seed: {seed.Value}");
            }
        }

        for (var i = 0; i < maxNewTokens; i++)
        {
            var step = Step(tokens, Config.Temperature, Config.TopK, explain, rng, deterministic);
            tokens.Add(step.NextTokenId);

            if (explain)
            {
                Console.WriteLine($"\n--- Generation Step {i + 1} ---");
                Console.Write(step.DebugText);
            }
        }

        return Tokenizer.Decode(tokens);
    }

    public GenerationStepResult Step(
        IList<int> currentTokenIds,
        float temperature = 1.0f,
        int topK = 40,
        bool explain = false,
        int? seed = null,
        bool deterministic = false,
        Random? samplingRandom = null)
    {
        Random? rng = deterministic
            ? null
            : samplingRandom ?? (seed.HasValue ? new Random(seed.Value) : new Random());
        return Step(currentTokenIds, temperature, topK, explain, rng, deterministic);
    }

    private GenerationStepResult Step(
        IList<int> currentTokenIds,
        float temperature,
        int topK,
        bool explain,
        Random? rng,
        bool deterministic)
    {
        // In GPT-style generation we always use all current tokens as context,
        // then only read logits from the final position to predict ONE next token.
        var context = currentTokenIds as IReadOnlyList<int> ?? currentTokenIds.ToList();
        var nextTokenLogits = ComputeNextTokenLogits(context);

        var candidateCount = Math.Clamp(topK, 1, nextTokenLogits.Length);
        var useGreedy = deterministic || temperature <= 0f || candidateCount == 1;
        var filteredLogits = ApplyTopKFilter(nextTokenLogits, candidateCount);
        var probabilities = Softmax(filteredLogits, useGreedy ? 1f : temperature);
        var candidates = BuildTopKCandidates(probabilities, candidateCount);

        var selectedTokenId = useGreedy
            ? candidates[0].id
            : SampleFromDistribution(probabilities, rng ?? throw new InvalidOperationException("Sampling RNG is required in non-deterministic mode."));

        var selectedTokenText = Tokenizer.TokenText(selectedTokenId);

        var debugText = string.Empty;
        if (explain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current length: {context.Count} tokens");
            if (useGreedy)
            {
                sb.AppendLine(deterministic
                    ? "Sampling mode: deterministic argmax"
                    : "Sampling mode: greedy (temperature <= 0 or top-k = 1)");
            }
            else
            {
                sb.AppendLine($"Sampling mode: temperature={temperature:0.###}, top-k={candidateCount}");
            }

            sb.AppendLine("Top-k candidates:");
            foreach (var item in candidates)
            {
                sb.AppendLine($"  id={item.id,-3} text={item.text,-10} p={item.p:P2}");
            }

            sb.AppendLine($"Chosen token: id={selectedTokenId}, text={selectedTokenText}");
            debugText = sb.ToString();
        }

        return new GenerationStepResult
        {
            NextTokenId = selectedTokenId,
            NextTokenText = selectedTokenText,
            TopK = candidates,
            Temperature = temperature,
            TopKValue = candidateCount,
            DebugText = debugText
        };
    }

    public IReadOnlyList<NextTokenPrediction> PredictNextTokens(
        string prompt,
        int topN = 5,
        float temperature = 1.0f,
        int topKFilter = 0)
    {
        var tokens = Tokenizer.Encode(prompt);
        var nextTokenLogits = ComputeNextTokenLogits(tokens);

        var safeTemperature = temperature <= 0f ? 1f : temperature;
        var logitsToUse = nextTokenLogits;

        if (topKFilter > 0)
        {
            var topK = Math.Clamp(topKFilter, 1, logitsToUse.Length);
            logitsToUse = ApplyTopKFilter(logitsToUse, topK);
        }

        var probabilities = Softmax(logitsToUse, safeTemperature);
        var predictionCount = Math.Clamp(topN, 1, probabilities.Length);

        return Enumerable.Range(0, probabilities.Length)
            .Select(tokenId => new NextTokenPrediction
            {
                TokenId = tokenId,
                TokenText = Tokenizer.TokenText(tokenId),
                Probability = probabilities[tokenId]
            })
            .OrderByDescending(prediction => prediction.Probability)
            .Take(predictionCount)
            .ToList();
    }

    private float[] ComputeNextTokenLogits(IReadOnlyList<int> context)
    {
        var embeddings = BuildEmbeddings(context);
        var hidden = embeddings;

        for (var i = 0; i < _layers.Count; i++)
        {
            hidden = _layers[i].Forward(hidden, context, Config);
        }

        var logits = BuildLogits(hidden, context);
        return Enumerable.Range(0, logits.Columns)
            .Select(i => logits[0, i])
            .ToArray();
    }

    private Tensor BuildEmbeddings(IReadOnlyList<int> tokens)
    {
        var tensor = new Tensor(tokens.Count, Config.EmbeddingSize);
        for (var i = 0; i < tokens.Count; i++)
        {
            for (var d = 0; d < Config.EmbeddingSize; d++)
            {
                var tokenSignal = MathF.Sin((tokens[i] + 1) * (d + 1) * 0.07f);
                var posSignal = Config.DisablePositionEmbeddings ? 0f : MathF.Cos((i + 1) * (d + 1) * 0.05f);
                tensor[i, d] = tokenSignal + posSignal;
            }
        }

        return tensor;
    }

    private Tensor BuildLogits(Tensor hidden, IReadOnlyList<int> tokens)
    {
        var logits = new Tensor(1, Tokenizer.Vocabulary.Count);
        var lastRow = Math.Max(0, hidden.Rows - 1);

        for (var vocabId = 0; vocabId < Tokenizer.Vocabulary.Count; vocabId++)
        {
            float score = 0;
            for (var d = 0; d < hidden.Columns; d++)
            {
                var classVector = MathF.Sin((vocabId + 1) * (d + 1) * 0.03f);
                score += hidden[lastRow, d] * classVector;
            }

            // Add a tiny recency effect so repeated words can become more likely.
            score += tokens.Count(t => t == vocabId) * 0.05f;
            logits[0, vocabId] = score;
        }

        return logits;
    }

    private static float[] ApplyTopKFilter(float[] logits, int topK)
    {
        var filtered = Enumerable.Repeat(float.NegativeInfinity, logits.Length).ToArray();
        var top = Enumerable.Range(0, logits.Length)
            .Select(i => (id: i, logit: logits[i]))
            .OrderByDescending(x => x.logit)
            .Take(topK)
            .ToList();

        foreach (var item in top)
        {
            filtered[item.id] = item.logit;
        }

        return filtered;
    }

    private static float[] Softmax(float[] logits, float temperature)
    {
        var temp = temperature <= 0f ? 1f : temperature;
        var scaled = logits.Select(v => v / temp).ToArray();
        var max = scaled.Where(v => !float.IsNegativeInfinity(v)).DefaultIfEmpty(0f).Max();

        var exp = scaled.Select(v => float.IsNegativeInfinity(v) ? 0f : MathF.Exp(v - max)).ToArray();
        var sum = exp.Sum();

        return sum <= 0f
            ? Enumerable.Range(0, logits.Length).Select(i => 1f / logits.Length).ToArray()
            : exp.Select(v => v / sum).ToArray();
    }

    private List<(int id, string text, float p)> BuildTopKCandidates(float[] probabilities, int topK)
    {
        return Enumerable.Range(0, probabilities.Length)
            .Select(i => (id: i, text: Tokenizer.TokenText(i), p: probabilities[i]))
            .Where(x => x.p > 0f)
            .OrderByDescending(x => x.p)
            .Take(topK)
            .ToList();
    }

    private static int SampleFromDistribution(float[] probabilities, Random rng)
    {
        var roll = rng.NextDouble();
        double cumulative = 0;

        for (var tokenId = 0; tokenId < probabilities.Length; tokenId++)
        {
            cumulative += probabilities[tokenId];
            if (roll <= cumulative)
            {
                return tokenId;
            }
        }

        // Guard for tiny floating-point drift when cumulative sum ends just below 1.0.
        for (var tokenId = probabilities.Length - 1; tokenId >= 0; tokenId--)
        {
            if (probabilities[tokenId] > 0f)
            {
                return tokenId;
            }
        }

        return 0;
    }
}
