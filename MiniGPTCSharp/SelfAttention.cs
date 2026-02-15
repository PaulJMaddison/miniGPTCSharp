namespace MiniGPTCSharp;

public class SelfAttention
{
    public Tensor LastAttentionWeights { get; private set; } = new Tensor(1, 1);

    public Tensor Forward(Tensor embeddings, IReadOnlyList<int> tokenIds, bool disabled)
    {
        if (disabled)
        {
            LastAttentionWeights = IdentityWeights(tokenIds.Count);
            return embeddings.Clone();
        }

        var seqLen = tokenIds.Count;
        var dim = embeddings.Columns;
        var scores = new float[seqLen, seqLen];

        // Compare every token with every previous token to estimate relevance.
        for (var i = 0; i < seqLen; i++)
        {
            for (var j = 0; j < seqLen; j++)
            {
                if (j > i)
                {
                    scores[i, j] = float.NegativeInfinity;
                    continue;
                }

                float dot = 0;
                for (var k = 0; k < dim; k++)
                {
                    dot += embeddings[i, k] * embeddings[j, k];
                }

                scores[i, j] = dot / MathF.Sqrt(dim);
            }
        }

        var weights = SoftmaxRows(scores);
        LastAttentionWeights = new Tensor(weights);

        var output = new Tensor(seqLen, dim);
        for (var i = 0; i < seqLen; i++)
        {
            for (var d = 0; d < dim; d++)
            {
                float blended = 0;
                for (var j = 0; j < seqLen; j++)
                {
                    blended += weights[i, j] * embeddings[j, d];
                }

                output[i, d] = blended;
            }
        }

        return output;
    }

    public List<(int TokenIndex, float Weight)> GetTopAttentionTargets(int tokenIndex, int topN = 3)
    {
        if (tokenIndex < 0 || tokenIndex >= LastAttentionWeights.Rows)
        {
            return new List<(int TokenIndex, float Weight)>();
        }

        return Enumerable.Range(0, LastAttentionWeights.Columns)
            .Select(i => (TokenIndex: i, Weight: LastAttentionWeights[tokenIndex, i]))
            .OrderByDescending(x => x.Weight)
            .Take(topN)
            .ToList();
    }

    private static Tensor IdentityWeights(int size)
    {
        var tensor = new Tensor(size, size);
        for (var i = 0; i < size; i++)
        {
            tensor[i, i] = 1f;
        }

        return tensor;
    }

    private static float[,] SoftmaxRows(float[,] scores)
    {
        var rows = scores.GetLength(0);
        var cols = scores.GetLength(1);
        var result = new float[rows, cols];

        for (var r = 0; r < rows; r++)
        {
            var max = float.NegativeInfinity;
            for (var c = 0; c < cols; c++)
            {
                if (scores[r, c] > max)
                {
                    max = scores[r, c];
                }
            }

            float sum = 0;
            for (var c = 0; c < cols; c++)
            {
                if (float.IsNegativeInfinity(scores[r, c]))
                {
                    result[r, c] = 0;
                    continue;
                }

                var e = MathF.Exp(scores[r, c] - max);
                result[r, c] = e;
                sum += e;
            }

            for (var c = 0; c < cols; c++)
            {
                result[r, c] = sum == 0 ? 0 : result[r, c] / sum;
            }
        }

        return result;
    }
}
