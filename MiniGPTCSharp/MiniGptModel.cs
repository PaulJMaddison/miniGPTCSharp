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
            var step = Step(tokens);
            tokens.Add(step.SelectedTokenId);

            if (explain)
            {
                Console.WriteLine($"\n--- Generation Step {i + 1} ---");
                Console.WriteLine(step.DebugInfo);
                Console.WriteLine($"Selected Token: {step.SelectedTokenText} (ID: {step.SelectedTokenId})");
                Console.WriteLine($"Updated Sequence: {Tokenizer.Decode(tokens)}");
            }
        }

        return Tokenizer.Decode(tokens);
    }

    public GenerationStepResult Step(List<int> currentTokens)
    {
        var debug = new StringBuilder();
        var embeddings = BuildEmbeddings(currentTokens);
        debug.AppendLine($"Embedding Tensor Shape: {embeddings.Shape}");

        var hidden = embeddings;
        Tensor attention = new Tensor(currentTokens.Count, currentTokens.Count);

        if (_layers.Count == 0)
        {
            debug.AppendLine("Transformer Layers: bypassed (LayerCount = 0). Using embeddings directly.");
        }

        for (var i = 0; i < _layers.Count; i++)
        {
            debug.AppendLine($"Running Transformer Block {i + 1}/{_layers.Count}...");
            hidden = _layers[i].Forward(hidden, currentTokens, Config);
            attention = _layers[i].Attention.LastAttentionWeights.Clone();

            var focusTargets = _layers[i].Attention.GetTopAttentionTargets(currentTokens.Count - 1);
            var focusText = string.Join(", ", focusTargets.Select(t => $"token[{t.TokenIndex}]={Tokenizer.TokenText(currentTokens[t.TokenIndex])} ({t.Weight:F3})"));
            debug.AppendLine($"Top attention targets for newest token: {focusText}");
        }

        var logits = BuildLogits(hidden, currentTokens);
        debug.AppendLine($"Logits Tensor Shape: {logits.Shape}");

        var probs = Softmax(logits, Config.Temperature);
        var topK = probs
            .OrderByDescending(x => x.Value)
            .Take(Math.Min(Config.TopK, probs.Count))
            .ToDictionary(x => Tokenizer.TokenText(x.Key), x => x.Value);

        debug.AppendLine("Top-K Probabilities:");
        foreach (var item in topK)
        {
            debug.AppendLine($"  {item.Key}: {item.Value:P2}");
        }

        var selected = SampleTopK(probs, Math.Min(Config.TopK, probs.Count));

        return new GenerationStepResult
        {
            SelectedTokenId = selected,
            SelectedTokenText = Tokenizer.TokenText(selected),
            TopKProbabilities = topK,
            Logits = logits,
            AttentionWeights = attention,
            DebugInfo = debug.ToString()
        };
    }

    public Dictionary<string, float> PredictNextTokens(string prompt, int count = 5)
    {
        var tokens = Tokenizer.Encode(prompt);
        var step = Step(tokens);
        return step.TopKProbabilities.Take(count).ToDictionary(x => x.Key, x => x.Value);
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

    private Dictionary<int, float> Softmax(Tensor logits, float temperature)
    {
        var temp = temperature <= 0 ? 1f : temperature;
        var values = Enumerable.Range(0, logits.Columns).Select(i => logits[0, i] / temp).ToArray();
        var max = values.Max();
        var exp = values.Select(v => MathF.Exp(v - max)).ToArray();
        var sum = exp.Sum();

        return Enumerable.Range(0, logits.Columns)
            .ToDictionary(i => i, i => exp[i] / sum);
    }

    private int SampleTopK(Dictionary<int, float> probs, int topK)
    {
        var candidates = probs.OrderByDescending(x => x.Value).Take(topK).ToList();
        var roll = _random.NextDouble();
        double cumulative = 0;

        foreach (var item in candidates)
        {
            cumulative += item.Value;
            if (roll <= cumulative)
            {
                return item.Key;
            }
        }

        return candidates[0].Key;
    }
}
