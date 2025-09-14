using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cognition.Jobs;

public class RecurringJobsRegistrar : IHostedService
{
    private readonly ILogger<RecurringJobsRegistrar> _logger;

    public RecurringJobsRegistrar(ILogger<RecurringJobsRegistrar> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register recurring jobs on startup
        RecurringJob.AddOrUpdate<ExampleJob>(
            "example-job",
            job => job.RunAsync(CancellationToken.None),
            () => Cron.Minutely(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        _logger.LogInformation("Recurring jobs registered.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
