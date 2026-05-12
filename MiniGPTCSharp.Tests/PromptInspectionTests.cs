using MiniGPTCSharp;
using Xunit;

namespace MiniGPTCSharp.Tests;

public sealed class PromptInspectionTests
{
    [Fact]
    public void InspectionReturnsTokenEmbeddingAndPredictionData()
    {
        var model = new MiniGptModel(new GptConfig { LayerCount = 2, EmbeddingSize = 16 });
        var inspection = model.InspectPrompt("The capital of France is", predictionTopN: 5, attentionTopN: 3);

        Assert.Equal(5, inspection.Tokens.Count);
        Assert.Equal(2, inspection.Layers.Count);
        Assert.Equal(5, inspection.Predictions.Count);
        Assert.All(inspection.Layers, layer => Assert.Equal(inspection.Tokens.Count, layer.AttentionWeights.Count));
        Assert.All(inspection.Tokens, token => Assert.Equal(16, token.Embedding.Count));
    }

    [Fact]
    public void DisabledAttentionBehavesLikeIdentityForLastToken()
    {
        var model = new MiniGptModel(new GptConfig { LayerCount = 1, DisableAttention = true });
        var inspection = model.InspectPrompt("AI model learning", predictionTopN: 3, attentionTopN: 3);
        var layer = inspection.Layers.Single();
        var lastTokenIndex = inspection.Tokens.Count - 1;
        var identityWeight = layer.LastTokenWeights[lastTokenIndex];
        var otherWeights = layer.LastTokenWeights.Where((_, index) => index != lastTokenIndex).ToArray();

        Assert.Equal(1f, identityWeight, precision: 4);
        Assert.All(otherWeights, weight => Assert.Equal(0f, weight, precision: 4));
    }
}
