namespace Cognition.Clients.Tools.Fiction.Weaver;

public enum FictionPhase
{
    VisionPlanner,
    WorldBibleManager,
    IterativePlanner,
    ChapterArchitect,
    ScrollRefiner,
    SceneWeaver
}

public enum FictionPhaseStatus
{
    Pending,
    Completed,
    Skipped,
    Failed,
    NotImplemented,
    Blocked,
    Cancelled
}
