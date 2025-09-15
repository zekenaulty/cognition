using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cognition.Clients.LLM;

public record ChatMessage(string Role, string Content);

public interface ILLMClient
{
    // Legacy single-prompt APIs
    Task<string> GenerateAsync(string prompt, bool track = false);
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false);

    // Chat APIs with role-aware messages
    Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false);
    IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false);
}
