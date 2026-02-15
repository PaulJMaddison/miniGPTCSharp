using System.Text;

namespace MiniGPTCSharp;

public class MiniGptModel
{
    private readonly List<TransformerBlock> _layers;
    private readonly Random _random;

    public MiniGptModel(GptConfig? config = null, int seed = 1234)
    {
        Config = config ?? new GptConfig();
        Tokenizer = new VocabularyTokenizer();
        _layers = Enumerable.Range(0, Math.Max(0, Config.LayerCount)).Select(i => new TransformerBlock(i)).ToList();
        _random = new Random(seed);
    }

    public GptConfig Config { get; }

    public VocabularyTokenizer Tokenizer { get; }

    public string Generate(string prompt, bool explain = false, int maxNewTokens = 10)
    {
        var tokens = Tokenizer.Encode(prompt);
        if (explain)
        {
            Console.WriteLine($"Prompt: {prompt}");
            Console.WriteLine($"Tokenized Input: [{string.Join(", ", Tokenizer.SplitTokens(prompt))}]");
            Console.WriteLine($"Token IDs: [{string.Join(", ", tokens)}]");
        }

        for (var i = 0; i < maxNewTokens; i++)
        {
            var step = Step(tokens, Config.Temperature, Config.TopK, explain);
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
        bool explain = false)
    {
        // In GPT-style generation we always use all current tokens as context,
        // then only read logits from the final position to predict ONE next token.
        var context = currentTokenIds as IReadOnlyList<int> ?? currentTokenIds.ToList();
        var embeddings = BuildEmbeddings(context);
        var hidden = embeddings;

        for (var i = 0; i < _layers.Count; i++)
        {
            hidden = _layers[i].Forward(hidden, context, Config);
        }

        var logits = BuildLogits(hidden, context);
        var nextTokenLogits = Enumerable.Range(0, logits.Columns)
            .Select(i => logits[0, i])
            .ToArray();

        var candidateCount = Math.Clamp(topK, 1, nextTokenLogits.Length);
        var useGreedy = temperature <= 0f || candidateCount == 1;
        var filteredLogits = ApplyTopKFilter(nextTokenLogits, candidateCount);
        var probabilities = Softmax(filteredLogits, useGreedy ? 1f : temperature);
        var candidates = BuildTopKCandidates(probabilities, candidateCount);

        var selectedTokenId = useGreedy
            ? candidates[0].id
            : SampleCategorical(candidates);

        var selectedTokenText = Tokenizer.TokenText(selectedTokenId);

        var debugText = string.Empty;
        if (explain)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current length: {context.Count} tokens");
            if (useGreedy)
            {
                sb.AppendLine("Sampling mode: greedy (temperature <= 0 or top-k = 1)");
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

    public Dictionary<string, float> PredictNextTokens(string prompt, int count = 5)
    {
        var tokens = Tokenizer.Encode(prompt);
        var step = Step(tokens, Config.Temperature, Config.TopK, explain: false);
        return step.TopK
            .Take(count)
            .ToDictionary(x => x.text, x => x.p);
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

    private int SampleCategorical(IReadOnlyList<(int id, string text, float p)> candidates)
    {
        var roll = _random.NextDouble();
        double cumulative = 0;

        foreach (var item in candidates)
        {
            cumulative += item.p;
            if (roll <= cumulative)
            {
                return item.id;
            }
        }

        return candidates[0].id;
    }
}
