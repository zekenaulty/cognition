using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;

namespace Cognition.Jobs
{
    public class HubForwarderHandler : IHandleMessages<AssistantMessageAppended>
    {
        private readonly SignalRNotifier _notifier;
        public HubForwarderHandler(SignalRNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task Handle(AssistantMessageAppended message)
        {
            // Forward AssistantMessageAppended to SignalR hub
            await _notifier.NotifyAssistantMessageAsync(message.ConversationId, message.PersonaId, message.Content);
        }
    }
}
