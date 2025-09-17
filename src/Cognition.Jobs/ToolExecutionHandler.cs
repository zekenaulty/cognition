using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Clients.Tools;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class ToolExecutionHandler : IHandleMessages<ToolExecutionRequested>
    {
        private readonly CognitionDbContext _db;
        private readonly IToolDispatcher _dispatcher;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        public ToolExecutionHandler(CognitionDbContext db, IToolDispatcher dispatcher, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _dispatcher = dispatcher;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(ToolExecutionRequested message)
        {
            // Invoke tool dispatcher, log, publish ToolExecutionCompleted
            await _db.SaveChangesAsync(); // placeholder for tool logic

            // Log event
            await _logger.LogAsync(message.ConversationId, nameof(ToolExecutionRequested), JObject.FromObject(message));

            // Publish ToolExecutionCompleted
            var result = new object(); // TODO: set actual result
            var success = true; // TODO: set actual success
            string? error = null; // TODO: set actual error if any
            var toolCompleted = new ToolExecutionCompleted(message.ConversationId, message.PersonaId, message.ToolId, result, success, error);
            await _bus.Publish(toolCompleted);
        }
    }
}
