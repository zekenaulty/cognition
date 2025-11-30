using Cognition.Api.Infrastructure.Security;
using Cognition.Clients.Agents;
using Cognition.Data.Relational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Cognition.Api.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
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
        public async Task SendAssistantMessage(string conversationId, string? agentId, string? personaId, string content, string messageId)
        {
            // Send structured event for UI placeholder matching
            var evt = new
            {
                ConversationId = conversationId,
                Content = content,
                AgentId = agentId,
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
        public async Task SendAssistantDelta(string conversationId, string? agentId, string? personaId, string delta)
        {
            var evt = new
            {
                conversationId = conversationId,
                agentId = agentId,
                personaId = personaId,
                delta = delta,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            await Clients.Group(conversationId).SendAsync("AssistantTokenDelta", evt);
        }

        public async Task SendPlanProgress(string conversationId, object payload)
        {
            await Clients.Group(conversationId).SendAsync("FictionPhaseProgressed", payload);
        }

        public async Task AppendUserMessage(string conversationId, string text, string? agentId, string? personaId, string providerId, string? modelId)
        {
            if (!Guid.TryParse(conversationId, out var conversationGuid))
            {
                throw new HubException("invalid_conversation_id");
            }

            // Resolve agentId from conversation when not supplied
            Guid resolvedAgentId;
            if (Guid.TryParse(agentId, out var parsedAgent))
            {
                resolvedAgentId = parsedAgent;
            }
            else
            {
                var convoAgent = await _db.Conversations.AsNoTracking()
                    .Where(c => c.Id == conversationGuid)
                    .Select(c => c.AgentId)
                    .FirstOrDefaultAsync();
                if (convoAgent == Guid.Empty)
                {
                    throw new HubException("conversation_not_found");
                }
                resolvedAgentId = convoAgent;
            }

            // Resolve persona from agent when not provided (persona is optional for agent-first flows)
            Guid? resolvedPersonaId = null;
            if (Guid.TryParse(personaId, out var parsedPersona) && parsedPersona != Guid.Empty)
            {
                resolvedPersonaId = parsedPersona;
            }
            else
            {
                resolvedPersonaId = await _db.Agents.AsNoTracking()
                    .Where(a => a.Id == resolvedAgentId)
                    .Select(a => (Guid?)a.PersonaId)
                    .FirstOrDefaultAsync();
            }

            Guid? modelGuid = null;
            if (!string.IsNullOrWhiteSpace(modelId) && Guid.TryParse(modelId, out var tmp))
            {
                modelGuid = tmp;
            }

            var response = await _agentService.ChatAsync(
                conversationGuid,
                resolvedAgentId,
                Guid.Parse(providerId),
                modelGuid,
                text,
                CancellationToken.None);

            await SendAssistantMessage(conversationId, resolvedAgentId.ToString(), resolvedPersonaId?.ToString(), response.Reply, response.MessageId.ToString());
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

