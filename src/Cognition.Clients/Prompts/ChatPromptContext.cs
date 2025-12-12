using System.Collections.Generic;
using Cognition.Clients.LLM;

namespace Cognition.Clients.Prompts;

public sealed record ChatPromptContext(
    IEnumerable<ChatMessage> InstructionMessages,
    string? SystemMessage,
    string? Summary,
    IEnumerable<ChatMessage> History,
    string UserInput);
