using System.Threading;
using System.Threading.Tasks;

namespace Cognition.Clients.Tools.Fiction.Lifecycle;

public interface IFictionLifecycleTelemetry
{
    Task LifecycleProcessedAsync(
        CharacterLifecycleRequest request,
        CharacterLifecycleResult result,
        CancellationToken cancellationToken);
}
