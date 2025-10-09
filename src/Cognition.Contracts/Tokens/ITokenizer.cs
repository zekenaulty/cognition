namespace Cognition.Contracts.Tokens;

/// <summary>
/// Exposes a minimal contract for estimating token counts across LLM providers.
/// </summary>
public interface ITokenizer
{
    int CountTokens(string text);
}
