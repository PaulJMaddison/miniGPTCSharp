namespace MiniGPTCSharp;

public class GenerationStepResult
{
    public int SelectedTokenId;

    public string SelectedTokenText = string.Empty;

    public Dictionary<string, float> TopKProbabilities = new();

    public Tensor Logits = new Tensor(1, 1);

    public Tensor AttentionWeights = new Tensor(1, 1);

    public string DebugInfo = string.Empty;
}
