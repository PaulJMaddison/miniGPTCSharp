using System.Text;
using System.Text.Json;
using MiniGPTCSharp;
using Xunit;

namespace MiniGPTCSharp.Tests;

public sealed class ExportReportTests
{
    [Fact]
    public void PredictJsonHasStableShape()
    {
        var export = MiniGptExports.BuildPredict(
            "The capital of France is",
            topN: 3,
            temperature: 1.0f,
            topKFilter: 0,
            deterministic: true);
        var json = MiniGptJson.Serialize(export);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        AssertUsesCamelCase(json);
        AssertString(root, "schemaVersion", "1.0");
        AssertString(root, "command", "predict");
        AssertString(root, "prompt", "The capital of France is");
        AssertNumber(root.GetProperty("options"), "topN", 3);
        AssertArrayCount(root, "tokens", 5);
        AssertArrayCount(root, "predictions", 3);

        var prediction = root.GetProperty("predictions")[0];
        RequireProperty(prediction, "rank");
        RequireProperty(prediction, "tokenId");
        RequireProperty(prediction, "tokenText");
        RequireProperty(prediction, "probability");
    }

    [Fact]
    public void InspectJsonShapesCoverSupportedTopics()
    {
        var config = new GptConfig { LayerCount = 2, EmbeddingSize = 16 };
        foreach (var topic in new[] { "tokens", "embeddings", "attention", "pipeline" })
        {
            var export = MiniGptExports.BuildInspection(
                topic,
                "The capital of France is",
                config,
                predictionTopN: 4,
                attentionTopN: 3,
                dimsToShow: 4);
            var json = MiniGptJson.Serialize(export);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            AssertUsesCamelCase(json);
            AssertString(root, "command", "inspect");
            AssertString(root, "topic", topic);
            AssertArrayCount(root, "tokens", 5);

            if (topic is "embeddings" or "pipeline")
            {
                AssertArrayCount(root, "embeddings", 5);
                var firstEmbedding = root.GetProperty("embeddings")[0];
                AssertNumber(firstEmbedding, "dimensionsShown", 4);
                AssertArrayCount(firstEmbedding, "values", 4);
            }
            else
            {
                AssertMissing(root, "embeddings");
            }

            if (topic is "attention" or "pipeline")
            {
                AssertArrayCount(root, "layers", 2);
                RequireProperty(root.GetProperty("layers")[0], "attentionRows");
                RequireProperty(root.GetProperty("layers")[0], "lastTokenWeights");
                RequireProperty(root.GetProperty("layers")[0], "topTargets");
            }
            else
            {
                AssertMissing(root, "layers");
            }

            if (topic == "pipeline")
            {
                AssertArrayCount(root, "predictions", 4);
            }
            else
            {
                AssertMissing(root, "predictions");
            }
        }
    }

    [Fact]
    public void CompareJsonShapesCoverSupportedTopics()
    {
        var sampling = MiniGptExports.BuildSamplingComparison("The capital of France is", tokensToGenerate: 4);
        var samplingJson = MiniGptJson.Serialize(sampling);
        using var samplingDocument = JsonDocument.Parse(samplingJson);
        var samplingRoot = samplingDocument.RootElement;

        AssertUsesCamelCase(samplingJson);
        AssertString(samplingRoot, "command", "compare");
        AssertString(samplingRoot, "topic", "sampling");
        AssertArrayCount(samplingRoot, "topPredictions", 5);
        AssertArrayCount(samplingRoot, "runs", 3);

        var ablation = MiniGptExports.BuildAblationComparison("The capital of France is", tokensToGenerate: 4);
        var ablationJson = MiniGptJson.Serialize(ablation);
        using var ablationDocument = JsonDocument.Parse(ablationJson);
        var ablationRoot = ablationDocument.RootElement;

        AssertUsesCamelCase(ablationJson);
        AssertString(ablationRoot, "command", "compare");
        AssertString(ablationRoot, "topic", "ablation");
        AssertArrayCount(ablationRoot, "runs", 4);
        RequireProperty(ablationRoot.GetProperty("runs")[0], "config");
    }

    [Fact]
    public void ReportHtmlContainsExpectedSections()
    {
        var report = MiniGptExports.BuildReport("The capital of France is", tokensToGenerate: 4, dimsToShow: 4);
        var html = MiniGptHtmlReport.BuildHtml(report);
        var outputPath = Path.Combine(Path.GetTempPath(), "minigpt-report-test.html");

        File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var written = File.ReadAllText(outputPath);

        AssertContains(written, "<!doctype html>");
        AssertContains(written, "GPT Internals Report");
        AssertContains(written, "Token Table");
        AssertContains(written, "Embedding Preview");
        AssertContains(written, "Attention By Layer");
        AssertContains(written, "Top Next-Token Probability Bars");
        AssertContains(written, "Sampling Comparison");
        AssertContains(written, "Ablation Comparison");
        AssertContains(written, "What This Teaches");
        AssertDoesNotContain(written, "<script");
        AssertDoesNotContain(written, "http://");
        AssertDoesNotContain(written, "https://");

        File.Delete(outputPath);
    }

    private static void AssertUsesCamelCase(string json)
    {
        AssertContains(json, "\"schemaVersion\"");
        AssertDoesNotContain(json, "\"SchemaVersion\"");
    }

    private static void AssertString(JsonElement element, string propertyName, string expected)
    {
        var actual = element.GetProperty(propertyName).GetString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected {propertyName} to be '{expected}', got '{actual}'.");
        }
    }

    private static void AssertNumber(JsonElement element, string propertyName, int expected)
    {
        var actual = element.GetProperty(propertyName).GetInt32();
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {propertyName} to be {expected}, got {actual}.");
        }
    }

    private static void AssertArrayCount(JsonElement element, string propertyName, int expected)
    {
        var actual = element.GetProperty(propertyName).GetArrayLength();
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {propertyName} to contain {expected} item(s), got {actual}.");
        }
    }

    private static void RequireProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out _))
        {
            throw new InvalidOperationException($"Expected JSON property '{propertyName}' to exist.");
        }
    }

    private static void AssertMissing(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out _))
        {
            throw new InvalidOperationException($"Expected JSON property '{propertyName}' to be omitted.");
        }
    }

    private static void AssertContains(string text, string expected)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected text to contain '{expected}'.");
        }
    }

    private static void AssertDoesNotContain(string text, string unexpected)
    {
        if (text.Contains(unexpected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected text not to contain '{unexpected}'.");
        }
    }
}
