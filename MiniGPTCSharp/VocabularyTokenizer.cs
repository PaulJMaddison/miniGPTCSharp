using System.Text.RegularExpressions;

namespace MiniGPTCSharp;

public class VocabularyTokenizer
{
    private readonly Dictionary<string, int> _tokenToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _idToToken = new();

    public VocabularyTokenizer()
    {
        // Keep the vocabulary tiny and human-readable so learners can inspect every token.
        Seed(new[]
        {
            "<pad>", "<unk>", ".", ",", "the", "of", "and", "to", "is", "in", "it",
            "France", "capital", "Paris", "AI", "model", "word", "next", "token", "learning"
        });
    }

    public int UnknownTokenId => 1;

    public IReadOnlyDictionary<int, string> Vocabulary => _idToToken;

    public List<string> SplitTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return Regex.Matches(text, @"[A-Za-z']+|[.,!?;:]")
            .Select(m => m.Value)
            .ToList();
    }

    public List<int> Encode(string text)
    {
        var tokens = SplitTokens(text);
        var ids = new List<int>(tokens.Count);
        foreach (var token in tokens)
        {
            if (!_tokenToId.TryGetValue(token, out var id))
            {
                id = AddToken(token);
            }

            ids.Add(id);
        }

        return ids;
    }

    public string Decode(IEnumerable<int> tokenIds)
    {
        return string.Join(' ', tokenIds.Select(id => _idToToken.TryGetValue(id, out var token) ? token : "<unk>"));
    }

    public string TokenText(int tokenId)
    {
        return _idToToken.TryGetValue(tokenId, out var token) ? token : "<unk>";
    }

    private void Seed(IEnumerable<string> tokens)
    {
        foreach (var token in tokens)
        {
            if (!_tokenToId.ContainsKey(token))
            {
                AddToken(token);
            }
        }
    }

    private int AddToken(string token)
    {
        var id = _tokenToId.Count;
        _tokenToId[token] = id;
        _idToToken[id] = token;
        return id;
    }
}
