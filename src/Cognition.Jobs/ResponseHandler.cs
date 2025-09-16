using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;

namespace Cognition.Jobs
{
    public class ResponseHandler : IHandleMessages<ToolExecutionCompleted>
    {
        private readonly CognitionDbContext _db;
        private readonly SignalRNotifier _notifier;
        public ResponseHandler(CognitionDbContext db, SignalRNotifier notifier)
        {
            _db = db;
            _notifier = notifier;
        }

        public async Task Handle(ToolExecutionCompleted message)
        {
            // Compose assistant reply, persist ConversationMessage, publish AssistantMessageAppended
            // ...existing logic to persist message...
            await _db.SaveChangesAsync();

            // Notify SignalR hub
            await _notifier.NotifyAssistantMessageAsync(message.ConversationId, message.Result?.ToString() ?? "");
        }
    }
}
