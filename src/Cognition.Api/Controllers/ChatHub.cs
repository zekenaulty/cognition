using Cognition.Clients.Agents;
using Cognition.Data.Relational;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Cognition.Api.Controllers
{
    public class ChatHub : Hub
    {
        private readonly IAgentService _agentService;
        private readonly CognitionDbContext _db;

        public ChatHub(IAgentService agentService, CognitionDbContext db)
        {
            _agentService = agentService;
            _db = db;
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

        public async Task AppendUserMessage(string conversationId, string text, string personaId, string providerId, string? modelId)
        {
            Guid? modelGuid = null;
            if (!string.IsNullOrWhiteSpace(modelId) && Guid.TryParse(modelId, out var tmp))
            {
                modelGuid = tmp;
            }

            var response = await _agentService.ChatAsync(
                Guid.Parse(conversationId),
                Guid.Parse(personaId),
                Guid.Parse(providerId),
                modelGuid,
                text,
                CancellationToken.None);

            await SendAssistantMessage(conversationId, personaId, response.Reply, response.MessageId.ToString());
            // Try to broadcast conversation title if available (AgentService may have just set it)
            try
            {
                var cid = Guid.Parse(conversationId);
                var convo = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cid);
                if (convo != null && !string.IsNullOrWhiteSpace(convo.Title))
                {
                    await Clients.Group(conversationId).SendAsync("ConversationUpdated", new { ConversationId = conversationId, Title = convo.Title });
                }
            }
            catch { }
        }


        // Called by client to join a conversation group
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
