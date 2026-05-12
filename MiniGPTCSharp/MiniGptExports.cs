namespace MiniGPTCSharp;

public static class MiniGptExports
{
    public static PredictExport BuildPredict(string prompt, int topN = 5, float temperature = 1.0f, int topKFilter = 0, bool deterministic = false)
    {
        var model = new MiniGptModel();
        var tokens = BuildTokenExports(model.Tokenizer, prompt);
        var predictions = model.PredictNextTokens(prompt, topN, temperature, topKFilter);
        var notes = new List<string>();

        if (temperature <= 0f)
        {
            notes.Add("--temp must be > 0. The model used 1.0 for probability calculation.");
        }

        if (deterministic)
        {
            notes.Add("Deterministic mode would choose the highest-probability token.");
        }

        return new PredictExport
        {
            Prompt = prompt,
            Options = new PredictOptionsExport
            {
                TopN = Math.Max(1, topN),
                Temperature = Round(temperature),
                TopKFilter = Math.Max(0, topKFilter),
                Deterministic = deterministic
            },
            Tokens = tokens,
            Predictions = BuildPredictions(predictions),
            Notes = notes
        };
    }

    public static PromptInspectionExport BuildInspection(string topic, string prompt, GptConfig config, int predictionTopN = 5, int attentionTopN = 5, int dimsToShow = 8)
    {
        var normalizedTopic = NormalizeInspectionTopic(topic);
        var model = new MiniGptModel(config);
        var inspection = model.InspectPrompt(prompt, predictionTopN, attentionTopN);
        var includeEmbeddings = normalizedTopic is "embeddings" or "pipeline";
        var includeAttention = normalizedTopic is "attention" or "pipeline";
        var includePredictions = normalizedTopic is "pipeline";

        return new PromptInspectionExport
        {
            Topic = normalizedTopic,
            Prompt = prompt,
            Config = BuildConfigExport(config),
            TokenCount = inspection.Tokens.Count,
            Tokens = BuildTokenExports(inspection),
            Embeddings = includeEmbeddings ? BuildEmbeddingPreviews(inspection, dimsToShow) : null,
            Layers = includeAttention ? BuildLayerAttentionExports(inspection) : null,
            Predictions = includePredictions ? BuildPredictions(inspection.Predictions) : null,
            Notes = BuildInspectionNotes(normalizedTopic)
        };
    }

    public static SamplingComparisonExport BuildSamplingComparison(string prompt, int tokensToGenerate = 8)
    {
        var tokenCount = Math.Max(1, tokensToGenerate);
        var model = new MiniGptModel();

        return new SamplingComparisonExport
        {
            Prompt = prompt,
            TokensToGenerate = tokenCount,
            TopPredictions = BuildPredictions(model.PredictNextTokens(prompt, topN: 5, temperature: 1.0f, topKFilter: 0)),
            Runs =
            [
                new GenerationRunExport
                {
                    Label = "Deterministic argmax",
                    Mode = "argmax",
                    Deterministic = true,
                    Output = model.Generate(prompt, maxNewTokens: tokenCount, deterministic: true)
                },
                new GenerationRunExport
                {
                    Label = "Seeded sampling (seed=42)",
                    Mode = "sample",
                    Seed = 42,
                    Output = model.Generate(prompt, maxNewTokens: tokenCount, seed: 42)
                },
                new GenerationRunExport
                {
                    Label = "Seeded sampling (seed=7)",
                    Mode = "sample",
                    Seed = 7,
                    Output = model.Generate(prompt, maxNewTokens: tokenCount, seed: 7)
                }
            ],
            Notes =
            [
                "Predict shows the belief distribution.",
                "Deterministic generation always follows the highest-probability path.",
                "Sampling can choose different but still plausible continuations."
            ]
        };
    }

    public static AblationComparisonExport BuildAblationComparison(string prompt, int tokensToGenerate = 8)
    {
        var tokenCount = Math.Max(1, tokensToGenerate);
        var runs = new[]
        {
            new { Label = "Baseline", Config = new GptConfig { LayerCount = 2, TopK = 10, Temperature = 0.8f } },
            new { Label = "No attention", Config = new GptConfig { LayerCount = 2, TopK = 10, Temperature = 0.8f, DisableAttention = true } },
            new { Label = "No position embeddings", Config = new GptConfig { LayerCount = 2, TopK = 10, Temperature = 0.8f, DisablePositionEmbeddings = true } },
            new { Label = "No layer norm", Config = new GptConfig { LayerCount = 2, TopK = 10, Temperature = 0.8f, DisableLayerNorm = true } }
        };

        return new AblationComparisonExport
        {
            Prompt = prompt,
            TokensToGenerate = tokenCount,
            Runs = runs.Select(run => new AblationRunExport
                {
                    Label = run.Label,
                    Config = BuildConfigExport(run.Config),
                    Output = new MiniGptModel(run.Config).Generate(prompt, maxNewTokens: tokenCount, deterministic: true)
                })
                .ToList(),
            Notes =
            [
                "Attention helps the model reuse earlier tokens as context.",
                "Position embeddings help it distinguish order.",
                "Layer norm stabilizes the internal signal across layers."
            ]
        };
    }

    public static GptInternalsReportExport BuildReport(string prompt, int tokensToGenerate = 8, int dimsToShow = 8)
    {
        var pipeline = BuildInspection(
            topic: "pipeline",
            prompt: prompt,
            config: new GptConfig { LayerCount = 2, TopK = 10, Temperature = 0.8f },
            predictionTopN: 5,
            attentionTopN: 5,
            dimsToShow: dimsToShow);

        return new GptInternalsReportExport
        {
            Summary = new PromptSummaryExport
            {
                Text = prompt,
                CharacterCount = prompt.Length,
                TokenCount = pipeline.TokenCount,
                DistinctTokenCount = pipeline.Tokens.Select(token => token.TokenId).Distinct().Count()
            },
            Pipeline = pipeline,
            Sampling = BuildSamplingComparison(prompt, tokensToGenerate),
            Ablation = BuildAblationComparison(prompt, tokensToGenerate),
            Notes =
            [
                "Token tables show the exact discrete context the model sees.",
                "Embedding previews turn each token into a compact numeric vector.",
                "Attention weights show which prior tokens influence the final-token state.",
                "Sampling comparisons separate the model's belief distribution from the choice policy.",
                "Ablations make architecture choices visible by removing one mechanism at a time."
            ]
        };
    }

    public static ModelConfigExport BuildConfigExport(GptConfig config)
        => new()
        {
            VocabularySize = config.VocabularySize,
            EmbeddingSize = config.EmbeddingSize,
            LayerCount = config.LayerCount,
            TopK = config.TopK,
            Temperature = Round(config.Temperature),
            DisableAttention = config.DisableAttention,
            DisablePositionEmbeddings = config.DisablePositionEmbeddings,
            DisableLayerNorm = config.DisableLayerNorm
        };

    private static string NormalizeInspectionTopic(string topic)
        => topic.ToLowerInvariant() switch
        {
            "tokens" => "tokens",
            "embeddings" => "embeddings",
            "attention" => "attention",
            "pipeline" => "pipeline",
            _ => throw new ArgumentException($"Unknown inspect topic: {topic}", nameof(topic))
        };

    private static IReadOnlyList<string> BuildInspectionNotes(string topic)
        => topic switch
        {
            "tokens" => ["Tokens are the discrete pieces the model receives before any numeric work begins."],
            "embeddings" => ["Embedding previews show the first dimensions of each token vector."],
            "attention" => ["Attention weights show how the final token distributes focus across earlier tokens."],
            "pipeline" => ["Pipeline output combines tokens, embeddings, attention, and next-token beliefs."],
            _ => []
        };

    private static IReadOnlyList<TokenExport> BuildTokenExports(VocabularyTokenizer tokenizer, string prompt)
    {
        var pieces = tokenizer.SplitTokens(prompt);
        var knownFlags = pieces.Select(tokenizer.HasToken).ToArray();
        var tokenIds = tokenizer.Encode(prompt);

        return tokenIds.Select((tokenId, index) => new TokenExport
            {
                Position = index,
                Text = pieces[index],
                TokenId = tokenId,
                WasAlreadyInVocabulary = knownFlags[index],
                VocabularySource = knownFlags[index] ? "seeded vocabulary" : "added at runtime"
            })
            .ToList();
    }

    private static IReadOnlyList<TokenExport> BuildTokenExports(PromptInspection inspection)
        => inspection.Tokens.Select(token => new TokenExport
            {
                Position = token.Position,
                Text = token.Text,
                TokenId = token.TokenId,
                WasAlreadyInVocabulary = token.WasAlreadyInVocabulary,
                VocabularySource = token.WasAlreadyInVocabulary ? "seeded vocabulary" : "added at runtime"
            })
            .ToList();

    private static IReadOnlyList<EmbeddingPreviewExport> BuildEmbeddingPreviews(PromptInspection inspection, int dimsToShow)
        => inspection.Tokens.Select(token =>
            {
                var dimensions = Math.Clamp(dimsToShow, 1, Math.Max(1, token.Embedding.Count));
                return new EmbeddingPreviewExport
                {
                    Position = token.Position,
                    TokenText = token.Text,
                    TokenId = token.TokenId,
                    DimensionsShown = Math.Min(dimensions, token.Embedding.Count),
                    Values = token.Embedding.Take(dimensions).Select(Round).ToList()
                };
            })
            .ToList();

    private static IReadOnlyList<LayerAttentionExport> BuildLayerAttentionExports(PromptInspection inspection)
        => inspection.Layers.Select(layer => new LayerAttentionExport
            {
                LayerIndex = layer.LayerIndex,
                AttentionRows = layer.AttentionWeights.Select((row, rowIndex) => new AttentionRowExport
                    {
                        TokenIndex = rowIndex,
                        TokenText = rowIndex < inspection.Tokens.Count ? inspection.Tokens[rowIndex].Text : "<out-of-range>",
                        Weights = row.Select((weight, columnIndex) => new AttentionWeightExport
                            {
                                TokenIndex = columnIndex,
                                TokenText = columnIndex < inspection.Tokens.Count ? inspection.Tokens[columnIndex].Text : "<out-of-range>",
                                Weight = Round(weight)
                            })
                            .ToList()
                    })
                    .ToList(),
                LastTokenWeights = layer.LastTokenWeights.Select((weight, index) => new AttentionWeightExport
                    {
                        TokenIndex = index,
                        TokenText = index < inspection.Tokens.Count ? inspection.Tokens[index].Text : "<out-of-range>",
                        Weight = Round(weight)
                    })
                    .ToList(),
                TopTargets = layer.TopTargets.Select(target => new AttentionWeightExport
                    {
                        TokenIndex = target.TokenIndex,
                        TokenText = target.TokenText,
                        Weight = Round(target.Weight)
                    })
                    .ToList()
            })
            .ToList();

    private static IReadOnlyList<PredictionExport> BuildPredictions(IReadOnlyList<NextTokenPrediction> predictions)
        => predictions.Select((prediction, index) => new PredictionExport
            {
                Rank = index + 1,
                TokenId = prediction.TokenId,
                TokenText = prediction.TokenText,
                Probability = Round(prediction.Probability)
            })
            .ToList();

    private static float Round(float value) => (float)Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
