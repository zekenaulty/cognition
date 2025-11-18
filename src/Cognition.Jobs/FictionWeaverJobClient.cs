using System.Collections.Generic;
using System.Threading;
using Hangfire;
namespace Cognition.Jobs;

public class FictionWeaverJobClient : IFictionWeaverJobClient
{
    private readonly IBackgroundJobClient _jobs;

    public FictionWeaverJobClient(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public string EnqueueVisionPlanner(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunVisionPlannerAsync(planId, agentId, conversationId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueWorldBibleManager(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunWorldBibleManagerAsync(planId, agentId, conversationId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueIterativePlanner(Guid planId, Guid agentId, Guid conversationId, int iterationIndex, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunIterativePlannerAsync(planId, agentId, conversationId, iterationIndex, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueChapterArchitect(Guid planId, Guid agentId, Guid conversationId, Guid blueprintId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunChapterArchitectAsync(planId, agentId, conversationId, blueprintId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueScrollRefiner(Guid planId, Guid agentId, Guid conversationId, Guid scrollId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunScrollRefinerAsync(planId, agentId, conversationId, scrollId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueSceneWeaver(Guid planId, Guid agentId, Guid conversationId, Guid sceneId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunSceneWeaverAsync(planId, agentId, conversationId, sceneId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    public string EnqueueLoreFulfillment(Guid planId, Guid requirementId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null)
        => _jobs.Enqueue<FictionWeaverJobs>(w => w.RunLoreFulfillmentAsync(planId, requirementId, agentId, conversationId, providerId, modelId, branchSlug, Copy(metadata), CancellationToken.None));

    private static IReadOnlyDictionary<string, string>? Copy(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null) return null;
        return new Dictionary<string, string>(metadata);
    }
}
