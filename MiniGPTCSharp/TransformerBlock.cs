namespace MiniGPTCSharp;

public class TransformerBlock
{
    private readonly SelfAttention _attention = new();

    public int LayerIndex { get; }

    public SelfAttention Attention => _attention;

    public TransformerBlock(int layerIndex)
    {
        LayerIndex = layerIndex;
    }

    public Tensor Forward(Tensor embeddings, IReadOnlyList<int> tokenIds, GptConfig config)
    {
        var attended = _attention.Forward(embeddings, tokenIds, config.DisableAttention);
        var output = new Tensor(attended.Rows, attended.Columns);

        for (var r = 0; r < attended.Rows; r++)
        {
            for (var c = 0; c < attended.Columns; c++)
            {
                // Residual-style blend: keep original signal and add context from attention.
                output[r, c] = embeddings[r, c] + 0.6f * attended[r, c];
            }
        }

        if (!config.DisableLayerNorm)
        {
            ApplySimpleLayerNorm(output);
        }

        return output;
    }

    private static void ApplySimpleLayerNorm(Tensor tensor)
    {
        for (var row = 0; row < tensor.Rows; row++)
        {
            float mean = 0;
            for (var col = 0; col < tensor.Columns; col++)
            {
                mean += tensor[row, col];
            }

            mean /= tensor.Columns;

            float variance = 0;
            for (var col = 0; col < tensor.Columns; col++)
            {
                var centered = tensor[row, col] - mean;
                variance += centered * centered;
            }

            variance /= tensor.Columns;
            var std = MathF.Sqrt(variance + 1e-5f);

            for (var col = 0; col < tensor.Columns; col++)
            {
                tensor[row, col] = (tensor[row, col] - mean) / std;
            }
        }
    }
}
