using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cognition.Clients.LLM;

public interface ILLMClient
{
    Task<string> GenerateAsync(string prompt, bool track = false);
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false);
}

