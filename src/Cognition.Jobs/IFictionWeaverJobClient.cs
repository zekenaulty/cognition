using System;
using System.Collections.Generic;

namespace Cognition.Jobs;

public interface IFictionWeaverJobClient
{
    string EnqueueVisionPlanner(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
    string EnqueueWorldBibleManager(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
    string EnqueueIterativePlanner(Guid planId, Guid agentId, Guid conversationId, int iterationIndex, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
    string EnqueueChapterArchitect(Guid planId, Guid agentId, Guid conversationId, Guid blueprintId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
    string EnqueueScrollRefiner(Guid planId, Guid agentId, Guid conversationId, Guid scrollId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
    string EnqueueSceneWeaver(Guid planId, Guid agentId, Guid conversationId, Guid sceneId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null);
}