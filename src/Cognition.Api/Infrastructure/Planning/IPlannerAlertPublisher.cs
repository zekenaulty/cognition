namespace Cognition.Api.Infrastructure.Planning;

public interface IPlannerAlertPublisher
{
    Task PublishAsync(IReadOnlyList<PlannerHealthAlert> alerts, CancellationToken ct);
}
