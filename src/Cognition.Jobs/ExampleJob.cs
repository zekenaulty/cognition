using Microsoft.Extensions.Logging;

namespace Cognition.Jobs;

public class ExampleJob
{
    private readonly ILogger<ExampleJob> _logger;

    public ExampleJob(ILogger<ExampleJob> logger)
    {
        _logger = logger;
    }

    public Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("ExampleJob executed at {time}", DateTimeOffset.Now);
        return Task.CompletedTask;
    }
}

