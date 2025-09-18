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
        public async Task SendAssistantMessage(string conversationId, string personaId, string content, string messageId)
        {
            // Send structured event for UI placeholder matching
            var evt = new
            {
                ConversationId = conversationId,
                Content = content,
                PersonaId = personaId,
                Timestamp = DateTime.UtcNow.ToString("o"),
                MessageId = messageId
            };
            await Clients.Group(conversationId).SendAsync("AssistantMessageAppended", evt);
        }

        // Overload to support callers that don't have a messageId yet
        //public async Task SendAssistantMessage(string conversationId, string personaId, string content)
        //{
        //    var evt = new
        //    {
        //        ConversationId = conversationId,
        //        Content = content,
        //        PersonaId = personaId,
        //        Timestamp = DateTime.UtcNow.ToString("o"),
        //        MessageId = (string?)null
        //    };
        //    await Clients.Group(conversationId).SendAsync("AssistantMessageAppended", evt);
        //}

        // Streaming token delta from backend during generation
        public async Task SendAssistantDelta(string conversationId, string personaId, string delta)
        {
            var evt = new
            {
                conversationId = conversationId,
                personaId = personaId,
                delta = delta,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            await Clients.Group(conversationId).SendAsync("AssistantTokenDelta", evt);
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

            await SendAssistantMessage(conversationId, personaId, response.Reply, response.MessageId.ToString());

        }


        // Called by client to join a conversation group
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
