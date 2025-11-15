namespace Cognition.Jobs;

internal static class FictionBacklogTokens
{
    public const string VisionPlan = "vision-plan";
    public const string WorldBible = "world-bible";
    public const string IterationPlan = "iteration-plan";
    public const string ChapterBlueprint = "chapter-blueprint";
    public const string ChapterScroll = "chapter-scroll";
    public const string SceneDraft = "scene-draft";
    public const string Scene = "scene";

    public static readonly string[] BlueprintOutputs = { ChapterBlueprint };
    public static readonly string[] ScrollOutputs = { ChapterScroll };
    public static readonly string[] SceneOutputs = { SceneDraft, Scene };
}
