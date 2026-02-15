namespace MiniGPTCSharp;

public class GptConfig
{
    public int VocabularySize { get; set; } = 128;

    public int EmbeddingSize { get; set; } = 16;

    public int LayerCount { get; set; } = 2;

    public int TopK { get; set; } = 10;

    public float Temperature { get; set; } = 0.8f;

    public bool DisableAttention { get; set; }

    public bool DisablePositionEmbeddings { get; set; }

    public bool DisableLayerNorm { get; set; }
}
