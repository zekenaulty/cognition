using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Cognition.Api.Controllers
{
    public class ChatHub : Hub
    {
        // Called by server to push assistant messages to clients
        public async Task SendAssistantMessage(string conversationId, string content)
        {
            // Send structured event for UI placeholder matching
            var evt = new {
                ConversationId = conversationId,
                Content = content,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            await Clients.Group(conversationId).SendAsync("AssistantMessageAppended", evt);
        }

        // Called by client to join a conversation group
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
    }
}
