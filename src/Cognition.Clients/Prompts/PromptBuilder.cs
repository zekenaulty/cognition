using System.Collections.Generic;
using System.Linq;
using Cognition.Clients.LLM;

namespace Cognition.Clients.Prompts;

public sealed class PromptBuilder : IPromptBuilder
{
    public IReadOnlyList<ChatMessage> BuildChatPrompt(ChatPromptContext context)
    {
        var messages = new List<ChatMessage>();

        if (context.InstructionMessages is not null)
        {
            messages.AddRange(context.InstructionMessages);
        }

        if (!string.IsNullOrWhiteSpace(context.SystemMessage))
        {
            messages.Add(new ChatMessage("system", context.SystemMessage!));
        }

        if (!string.IsNullOrWhiteSpace(context.Summary))
        {
            messages.Add(new ChatMessage("system", "Conversation Summary: " + context.Summary));
        }

        if (context.History is not null)
        {
            messages.AddRange(context.History);
        }

        messages.Add(new ChatMessage("user", context.UserInput));

        return messages;
    }
}
