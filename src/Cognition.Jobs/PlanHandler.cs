using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class PlanHandler : IHandleMessages<PlanRequested>
    {
        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        public PlanHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(PlanRequested message)
        {
            // Call planner, validate plan JSON, save ConversationPlan, publish PlanReady
            await _db.SaveChangesAsync(); // placeholder for planner logic

            // Log event
            await _logger.LogAsync(message.ConversationId, nameof(PlanRequested), JObject.FromObject(message));

            // Publish PlanReady
            var plan = new ToolPlan("{}" /* TODO: replace with actual plan JSON */);
            var planReady = new PlanReady(message.ConversationId, message.PersonaId, plan);
            await _bus.Publish(planReady);
        }
    }
}
