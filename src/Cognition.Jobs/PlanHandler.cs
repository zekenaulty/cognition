using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;

namespace Cognition.Jobs
{
    public class PlanHandler : IHandleMessages<PlanRequested>
    {
        private readonly CognitionDbContext _db;
        public PlanHandler(CognitionDbContext db)
        {
            _db = db;
        }

        public async Task Handle(PlanRequested message)
        {
            // Call planner, validate plan JSON, save ConversationPlan, publish PlanReady
            await _db.SaveChangesAsync(); // placeholder for planner logic
        }
    }
}
