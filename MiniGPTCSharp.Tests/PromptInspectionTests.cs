using MiniGPTCSharp;

namespace MiniGPTCSharp.Tests;

public static class PromptInspectionTests
{
    public static void RunAll()
    {
        InspectionReturnsTokenEmbeddingAndPredictionData();
        DisabledAttentionBehavesLikeIdentityForLastToken();
    }

    private static void InspectionReturnsTokenEmbeddingAndPredictionData()
    {
        var model = new MiniGptModel(new GptConfig { LayerCount = 2, EmbeddingSize = 16 });
        var inspection = model.InspectPrompt("The capital of France is", predictionTopN: 5, attentionTopN: 3);

        if (inspection.Tokens.Count != 5)
        {
            throw new InvalidOperationException($"Expected 5 tokens, got {inspection.Tokens.Count}.");
        }

        if (inspection.Layers.Count != 2)
        {
            throw new InvalidOperationException($"Expected 2 layers, got {inspection.Layers.Count}.");
        }

        if (inspection.Predictions.Count != 5)
        {
            throw new InvalidOperationException($"Expected 5 predictions, got {inspection.Predictions.Count}.");
        }

        if (inspection.Tokens.Any(token => token.Embedding.Count != 16))
        {
            throw new InvalidOperationException("Expected every token to have an embedding vector of length 16.");
        }

        Console.WriteLine("PASS: Prompt inspection returns token, embedding, layer, and prediction data.");
    }

    private static void DisabledAttentionBehavesLikeIdentityForLastToken()
    {
        var model = new MiniGptModel(new GptConfig { LayerCount = 1, DisableAttention = true });
        var inspection = model.InspectPrompt("AI model learning", predictionTopN: 3, attentionTopN: 3);
        var layer = inspection.Layers.Single();
        var lastTokenIndex = inspection.Tokens.Count - 1;
        var identityWeight = layer.LastTokenWeights[lastTokenIndex];
        var otherWeights = layer.LastTokenWeights.Where((_, index) => index != lastTokenIndex).ToArray();

        if (Math.Abs(identityWeight - 1f) > 0.0001f)
        {
            throw new InvalidOperationException($"Expected last-token self weight to be 1.0, got {identityWeight:0.0000}.");
        }

        if (otherWeights.Any(weight => Math.Abs(weight) > 0.0001f))
        {
            throw new InvalidOperationException("Expected all non-self attention weights to be 0 when attention is disabled.");
        }

        Console.WriteLine("PASS: Disabled attention falls back to identity weights.");
    }
}
