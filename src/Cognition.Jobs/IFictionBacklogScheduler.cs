using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Jobs;

public interface IFictionBacklogScheduler
{
    Task ScheduleAsync(
        FictionPlan plan,
        FictionPhase completedPhase,
        FictionPhaseResult result,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken);
}
