using Hangfire;
using Hangfire.Storage;

namespace Cognition.Api.Infrastructure.Hangfire;

public interface IHangfireRunner
{
    Task<bool> WaitForCompletionAsync(string jobId, TimeSpan timeout, TimeSpan pollInterval, CancellationToken ct = default);
}

public class HangfireRunner : IHangfireRunner
{
    private readonly JobStorage _storage;
    public HangfireRunner(JobStorage storage)
    {
        _storage = storage;
    }

    public async Task<bool> WaitForCompletionAsync(string jobId, TimeSpan timeout, TimeSpan pollInterval, CancellationToken ct = default)
    {
        var api = _storage.GetMonitoringApi();
        var stop = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < stop && !ct.IsCancellationRequested)
        {
            var details = api.JobDetails(jobId);
            var state = details?.History?.FirstOrDefault()?.StateName;
            if (string.Equals(state, "Succeeded", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(state, "Deleted", StringComparison.OrdinalIgnoreCase) || string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase)) return false;
            await Task.Delay(pollInterval, ct);
        }
        return false; // timeout treated as failure to keep controller semantics clear
    }
}
