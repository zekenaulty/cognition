using Cognition.Contracts.Tokens;

namespace Cognition.Testing.Tokens;

public sealed class FakeTokenizer : ITokenizer
{
    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        var estimated = (int)Math.Ceiling(text.Length / 4d);
        return Math.Max(1, estimated);
    }
}
