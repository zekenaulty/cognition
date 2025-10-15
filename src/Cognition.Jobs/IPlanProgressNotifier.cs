using System;
using System.Threading.Tasks;

namespace Cognition.Jobs;

public interface IPlanProgressNotifier
{
    Task NotifyPlanProgressAsync(Guid conversationId, object payload);
}
