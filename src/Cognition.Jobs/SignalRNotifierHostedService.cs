using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Cognition.Jobs
{
    public class SignalRNotifierHostedService : IHostedService
    {
        private readonly SignalRNotifier _notifier;
        public SignalRNotifierHostedService(SignalRNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _notifier.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Optionally stop SignalR connection
            return Task.CompletedTask;
        }
    }
}