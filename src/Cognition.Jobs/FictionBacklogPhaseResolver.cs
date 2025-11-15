using System;
using System.Linq;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Jobs;

internal static class FictionBacklogPhaseResolver
{
    public static FictionPhase? ResolvePhase(FictionPlanBacklogItem item)
    {
        if (item.Outputs is null || item.Outputs.Length == 0)
        {
            return null;
        }

        if (item.Outputs.Any(o => FictionBacklogTokens.BlueprintOutputs.Contains(o, StringComparer.OrdinalIgnoreCase)))
        {
            return FictionPhase.ChapterArchitect;
        }

        if (item.Outputs.Any(o => FictionBacklogTokens.ScrollOutputs.Contains(o, StringComparer.OrdinalIgnoreCase)))
        {
            return FictionPhase.ScrollRefiner;
        }

        if (item.Outputs.Any(o => FictionBacklogTokens.SceneOutputs.Contains(o, StringComparer.OrdinalIgnoreCase)))
        {
            return FictionPhase.SceneWeaver;
        }

        if (item.Outputs.Any(o => string.Equals(o, FictionBacklogTokens.WorldBible, StringComparison.OrdinalIgnoreCase)))
        {
            return FictionPhase.WorldBibleManager;
        }

        if (item.Outputs.Any(o => string.Equals(o, FictionBacklogTokens.VisionPlan, StringComparison.OrdinalIgnoreCase)))
        {
            return FictionPhase.VisionPlanner;
        }

        if (item.Outputs.Any(o => string.Equals(o, FictionBacklogTokens.IterationPlan, StringComparison.OrdinalIgnoreCase)))
        {
            return FictionPhase.IterativePlanner;
        }

        return null;
    }
}
