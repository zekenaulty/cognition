using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;

namespace Cognition.Jobs
{
    public class PlanReadyHandler : IHandleMessages<PlanReady>
    {
        private readonly CognitionDbContext _db;
        public PlanReadyHandler(CognitionDbContext db)
        {
            _db = db;
        }

        public async Task Handle(PlanReady message)
        {
            // Update ConversationWorkflowState, emit ToolExecutionRequested
            await _db.SaveChangesAsync(); // placeholder for workflow logic
        }
    }
}
