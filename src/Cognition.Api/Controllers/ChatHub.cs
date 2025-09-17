using Cognition.Clients.Agents;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Cognition.Api.Controllers
{
    public class ChatHub : Hub
    {
        private readonly IAgentService _agentService;

        public ChatHub(IAgentService agentService)
        {
            _agentService = agentService;
        }

        // Called by server to push assistant messages to clients
        public async Task SendAssistantMessage(string conversationId, string personaId, string content)
        {
            // Send structured event for UI placeholder matching
            var evt = new
            {
                ConversationId = conversationId,
                Content = content,
                PersonaId = personaId,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            await Clients.Group(conversationId).SendAsync("AssistantMessageAppended", evt);
        }

        public async Task AppendUserMessage(string conversationId, string text, string personaId, string providerId, string modelId)
        {
            var response = await _agentService.ChatAsync(
                Guid.Parse(conversationId),
                Guid.Parse(personaId),
                Guid.Parse(providerId),
                Guid.Parse(modelId),
                text,
                true,
                CancellationToken.None);

            await SendAssistantMessage(conversationId, personaId,response);

        }


        // Called by client to join a conversation group
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
