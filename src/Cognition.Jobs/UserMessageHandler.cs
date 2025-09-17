using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class UserMessageHandler : IHandleMessages<UserMessageAppended>
    {
        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        public UserMessageHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(UserMessageAppended message)
        {
            // Load context/window, emit PlanRequested
            await _db.SaveChangesAsync(); // placeholder for context logic

            // Log event
            await _logger.LogAsync(message.ConversationId, nameof(UserMessageAppended), JObject.FromObject(message));

            // Publish PlanRequested
            var planRequested = new PlanRequested(message.ConversationId, message.PersonaId, "(plan args)");
            await _bus.Publish(planRequested);
        }
    }
}
