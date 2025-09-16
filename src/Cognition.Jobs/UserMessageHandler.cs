using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;

namespace Cognition.Jobs
{
    public class UserMessageHandler : IHandleMessages<UserMessageAppended>
    {
        private readonly CognitionDbContext _db;
        public UserMessageHandler(CognitionDbContext db)
        {
            _db = db;
        }

        public async Task Handle(UserMessageAppended message)
        {
            // Load context/window, emit PlanRequested
            await _db.SaveChangesAsync(); // placeholder for context logic
            // Publish PlanRequested (bus injected via DI in handler registration)
            // This will be handled by PlanHandler
        }
    }
}
