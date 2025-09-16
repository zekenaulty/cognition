using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Rebus.Handlers;
using Cognition.Data.Relational;
using Cognition.Clients.Tools;

namespace Cognition.Jobs
{
    public class ToolExecutionHandler : IHandleMessages<ToolExecutionRequested>
    {
        private readonly CognitionDbContext _db;
        private readonly IToolDispatcher _dispatcher;
        public ToolExecutionHandler(CognitionDbContext db, IToolDispatcher dispatcher)
        {
            _db = db;
            _dispatcher = dispatcher;
        }

        public async Task Handle(ToolExecutionRequested message)
        {
            // Invoke tool dispatcher, log, publish ToolExecutionCompleted
            await _db.SaveChangesAsync(); // placeholder for tool logic
        }
    }
}
