using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class ResponseHandler : IHandleMessages<ToolExecutionCompleted>
    {
        private readonly CognitionDbContext _db;
        private readonly SignalRNotifier _notifier;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        public ResponseHandler(CognitionDbContext db, SignalRNotifier notifier, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _notifier = notifier;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(ToolExecutionCompleted message)
        {
            // Compose assistant reply, persist ConversationMessage, publish AssistantMessageAppended
            // ...existing logic to persist message...
            await _db.SaveChangesAsync();

            // Log event
            await _logger.LogAsync(message.ConversationId, nameof(ToolExecutionCompleted), JObject.FromObject(message));

            // Publish AssistantMessageAppended
            var assistantAppended = new AssistantMessageAppended(message.ConversationId, message.PersonaId, message.Result?.ToString() ?? "");
            await _bus.Publish(assistantAppended);

            // Notify SignalR hub
            await _notifier.NotifyAssistantMessageAsync(message.ConversationId, message.Result?.ToString() ?? "");
        }
    }
}
