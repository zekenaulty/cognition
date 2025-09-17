using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class PlanReadyHandler : IHandleMessages<PlanReady>
    {
        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        public PlanReadyHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(PlanReady message)
        {
            // Update ConversationWorkflowState, emit ToolExecutionRequested
            await _db.SaveChangesAsync(); // placeholder for workflow logic

            // Log event
            await _logger.LogAsync(message.ConversationId, nameof(PlanReady), JObject.FromObject(message));

            // Publish ToolExecutionRequested
            var toolId = Guid.Empty; // TODO: set actual toolId from plan
            var args = new System.Collections.Generic.Dictionary<string, object?>(); // TODO: set actual args from plan
            var toolRequested = new ToolExecutionRequested(message.ConversationId, message.PersonaId, toolId, args);
            await _bus.Publish(toolRequested);
        }
    }
}
