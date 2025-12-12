using System.Collections.Generic;
using Cognition.Clients.LLM;

namespace Cognition.Clients.Prompts;

public interface IPromptBuilder
{
    IReadOnlyList<ChatMessage> BuildChatPrompt(ChatPromptContext context);
}
