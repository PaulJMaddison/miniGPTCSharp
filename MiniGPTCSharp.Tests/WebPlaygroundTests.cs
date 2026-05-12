using MiniGPTCSharp.Web;
using Xunit;

namespace MiniGPTCSharp.Tests;

public sealed class WebPlaygroundTests
{
    [Fact]
    public void WebInspectionReturnsTimelineAblationsAndReportAvailability()
    {
        var service = new GptMicroscopeService();
        var response = service.Inspect(new InspectionRequest
        {
            Prompt = "The capital of France is",
            Deterministic = true,
            Temperature = 0.8f,
            TopK = 6,
            TokenCount = 3
        });

        Assert.Equal(5, response.PromptTokens.Count);
        Assert.Equal(6, response.NextTokenCandidates.Count);
        Assert.Equal(2, response.AttentionLayers.Count);
        Assert.All(response.AttentionLayers, layer => Assert.Equal(response.PromptTokens.Count, layer.Weights.Count));
        Assert.Equal(3, response.Timeline.Count);
        Assert.Equal(4, response.Ablations.Count);
        Assert.True(response.ReportExportAvailable);
    }
}
