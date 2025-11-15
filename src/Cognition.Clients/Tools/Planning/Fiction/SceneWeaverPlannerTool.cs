using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Planning.Fiction;

public sealed class SceneWeaverPlannerParameters : PlannerParameters
{
    public SceneWeaverPlannerParameters(IDictionary<string, object?> values) : base(values)
    {
    }

    public FictionPlan? Plan => Get<FictionPlan>("plan");
    public Conversation? Conversation => Get<Conversation>("conversation");
    public FictionPhaseExecutionContext? ExecutionContext => Get<FictionPhaseExecutionContext>("executionContext");
    public Guid ProviderId => Get<Guid>("providerId");
    public Guid? ModelId => Get<Guid?>("modelId");
    public FictionChapterScene? Scene => Get<FictionChapterScene>("scene");

    public static SceneWeaverPlannerParameters Create(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext executionContext,
        Guid providerId,
        Guid? modelId,
        FictionChapterScene scene)
    {
        return new SceneWeaverPlannerParameters(new Dictionary<string, object?>
        {
            ["plan"] = plan,
            ["conversation"] = conversation,
            ["executionContext"] = executionContext,
            ["providerId"] = providerId,
            ["modelId"] = modelId,
            ["scene"] = scene
        });
    }
}

[PlannerCapabilities("planning", "fiction", "scene")]
public sealed class SceneWeaverPlannerTool : PlannerBase<SceneWeaverPlannerParameters>
{
    private const string TemplateId = "planner.fiction.sceneWeaver";

    private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
        name: "Scene Weaver",
        description: "Drafts a full narrative scene using the scroll and blueprint context produced by upstream planners.",
        capabilities: new[] { "planning", "fiction", "scene" },
        steps: new[]
        {
            new PlannerStepDescriptor("scene-draft", "Draft Scene", TemplateId: TemplateId)
        },
        telemetryTags: new Dictionary<string, string>
        {
            ["planner"] = "scene-weaver"
        });

    private readonly IAgentService _agentService;

    public SceneWeaverPlannerTool(
        IAgentService agentService,
        ILoggerFactory loggerFactory,
        IPlannerTelemetry telemetry,
        IPlannerTranscriptStore transcriptStore,
        IPlannerTemplateRepository templateRepository,
        IOptions<PlannerCritiqueOptions> critiqueOptions,
        IScopePathBuilder scopePathBuilder)
        : base(loggerFactory, telemetry, transcriptStore, templateRepository, critiqueOptions, scopePathBuilder)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
    }

    public override PlannerMetadata Metadata => MetadataDefinition;

    protected override void ValidateInputs(SceneWeaverPlannerParameters parameters)
    {
        if (parameters.Plan is null) throw new ArgumentException("Planner parameters must include the FictionPlan.", nameof(parameters));
        if (parameters.Conversation is null) throw new ArgumentException("Planner parameters must include the Conversation.", nameof(parameters));
        if (parameters.ExecutionContext is null) throw new ArgumentException("Planner parameters must include the execution context.", nameof(parameters));
        if (parameters.ProviderId == Guid.Empty) throw new ArgumentException("Planner parameters must include providerId.", nameof(parameters));
        if (parameters.Scene is null) throw new ArgumentException("Planner parameters must include the scene context.", nameof(parameters));
    }

    protected override async Task<PlannerResult> ExecutePlanAsync(PlannerContext context, SceneWeaverPlannerParameters parameters, CancellationToken ct)
    {
        var plan = parameters.Plan!;
        var executionContext = parameters.ExecutionContext!;
        var scene = parameters.Scene!;
        var section = scene.FictionChapterSection;
        var scroll = section?.FictionChapterScroll;
        var blueprint = scroll?.FictionChapterBlueprint;

        var template = await TemplateRepository.GetTemplateAsync(TemplateId, ct).ConfigureAwait(false);
        var authorContext = AuthorPersonaPromptContext.FromConversationState(context.ConversationState);
        var prompt = template is { Length: > 0 } resolvedTemplate
            ? BuildPromptFromTemplate(resolvedTemplate, plan, executionContext, scene, section, scroll, blueprint, authorContext)
            : BuildFallbackPrompt(plan, executionContext, scene, section, scroll, blueprint, authorContext);

        var stopwatch = Stopwatch.StartNew();

        var (reply, messageId) = await _agentService.ChatAsync(
            executionContext.ConversationId,
            executionContext.AgentId,
            parameters.ProviderId,
            parameters.ModelId,
            prompt,
            ct).ConfigureAwait(false);

        stopwatch.Stop();

        var artifacts = BuildArtifacts(reply, prompt, scene, section, scroll, blueprint);
        var stepOutput = new Dictionary<string, object?>
        {
            ["prompt"] = prompt,
            ["response"] = reply,
            ["messageId"] = messageId,
            ["chapterSceneId"] = scene.Id,
            ["chapterSectionId"] = section?.Id,
            ["chapterScrollId"] = scroll?.Id,
            ["chapterBlueprintId"] = blueprint?.Id
        };

        var transcriptEntries = new[]
        {
            new PlannerTranscriptEntry(DateTime.UtcNow, "system", prompt),
            new PlannerTranscriptEntry(DateTime.UtcNow, "assistant", reply, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["chapterSceneId"] = scene.Id,
                ["chapterSectionId"] = section?.Id,
                ["chapterScrollId"] = scroll?.Id,
                ["chapterBlueprintId"] = blueprint?.Id,
                ["sceneSlug"] = scene.SceneSlug
            })
        };

        return PlannerResult.Success()
            .AddArtifacts(artifacts)
            .AddStep(new PlannerStepRecord("scene-draft", PlannerStepStatus.Completed, stepOutput, stopwatch.Elapsed))
            .AddTranscript(transcriptEntries)
            .AddMetric("latencyMs", stopwatch.Elapsed.TotalMilliseconds)
            .AddDiagnostics("summary", "Scene weaving response recorded.")
            .AddDiagnostics("sceneSlug", scene.SceneSlug)
            .AddDiagnostics("responseLength", reply.Length.ToString(CultureInfo.InvariantCulture));
    }

    private static string BuildPromptFromTemplate(
        string template,
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterScene scene,
        FictionChapterSection? section,
        FictionChapterScroll? scroll,
        FictionChapterBlueprint? blueprint,
        AuthorPersonaPromptContext authorContext)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planName"] = plan.Name,
            ["branch"] = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug!,
            ["sceneTitle"] = scene.Title,
            ["sceneSlug"] = scene.SceneSlug,
            ["sceneDescription"] = string.IsNullOrWhiteSpace(scene.Description) ? "(no description captured)" : scene.Description!,
            ["sceneMetadata"] = SerializeJson(scene.Metadata) ?? "(none)",
            ["sectionSummary"] = BuildSectionSummary(section),
            ["scrollSynopsis"] = BuildScrollSynopsis(scroll),
            ["blueprintStructure"] = SerializeJson(blueprint?.Structure) ?? "(blueprint structure unavailable)",
            ["authorPersonaName"] = authorContext.PersonaName ?? "(author persona not set)",
            ["authorPersonaSummary"] = authorContext.SummaryText,
            ["authorPersonaMemories"] = authorContext.MemoriesText,
            ["authorWorldNotes"] = authorContext.WorldNotesText
        };

        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string BuildFallbackPrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterScene scene,
        FictionChapterSection? section,
        FictionChapterScroll? scroll,
        FictionChapterBlueprint? blueprint,
        AuthorPersonaPromptContext authorContext)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug!;
        var builder = new StringBuilder();
        builder.AppendLine($"You are writing the full narrative scene \"{scene.Title}\" (slug {scene.SceneSlug}) for branch \"{branch}\" in project \"{plan.Name}\".");
        builder.AppendLine();
        builder.AppendLine("Author persona summary:");
        builder.AppendLine(authorContext.SummaryText);
        builder.AppendLine();
        builder.AppendLine("Recent persona memories to honor:");
        builder.AppendLine(authorContext.MemoriesText);
        builder.AppendLine();
        builder.AppendLine("World-bible obligations:");
        builder.AppendLine(authorContext.WorldNotesText);
        builder.AppendLine();
        builder.AppendLine("Scene description:");
        builder.AppendLine(string.IsNullOrWhiteSpace(scene.Description) ? "(no description captured)" : scene.Description!);
        builder.AppendLine();
        builder.AppendLine("Section context:");
        builder.AppendLine(BuildSectionSummary(section));
        builder.AppendLine();
        builder.AppendLine("Scroll synopsis:");
        builder.AppendLine(BuildScrollSynopsis(scroll));
        builder.AppendLine();
        builder.AppendLine("Blueprint structure (JSON):");
        builder.AppendLine(SerializeJson(blueprint?.Structure) ?? "(blueprint structure unavailable)");
        builder.AppendLine();
        builder.AppendLine("Scene metadata (JSON):");
        builder.AppendLine(SerializeJson(scene.Metadata) ?? "(none)");
        builder.AppendLine();
        builder.AppendLine("Write the complete scene in rich Markdown. Use immersive prose aligned with the project goals. Include dialogue, action, and interiority. Target 900-1300 words. Return Markdown only; do not add JSON or commentary.");
        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, object?> BuildArtifacts(
        string response,
        string prompt,
        FictionChapterScene scene,
        FictionChapterSection? section,
        FictionChapterScroll? scroll,
        FictionChapterBlueprint? blueprint)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["rawResponse"] = response,
            ["sceneId"] = scene.Id,
            ["sceneSlug"] = scene.SceneSlug,
            ["sceneTitle"] = scene.Title
        };

        if (!string.IsNullOrWhiteSpace(scene.Description))
        {
            data["sceneDescription"] = scene.Description;
        }

        if (scene.Metadata is not null && scene.Metadata.Count > 0)
        {
            data["sceneMetadata"] = scene.Metadata;
        }

        if (section is not null)
        {
            data["sectionContext"] = new
            {
                section.Id,
                section.SectionSlug,
                section.Title,
                section.Description,
                section.SectionIndex
            };
        }

        if (scroll is not null)
        {
            data["scrollContext"] = new
            {
                scroll.Id,
                scroll.ScrollSlug,
                scroll.Title,
                scroll.Synopsis
            };
        }

        if (blueprint is not null)
        {
            data["blueprintContext"] = new
            {
                blueprint.Id,
                blueprint.ChapterSlug,
                blueprint.Title,
                blueprint.Synopsis
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            data["parsed"] = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            data["parseError"] = ex.Message;
        }

        return data;
    }

    private static string BuildSectionSummary(FictionChapterSection? section)
    {
        if (section is null)
        {
            return "(section context unavailable)";
        }

        var description = string.IsNullOrWhiteSpace(section.Description) ? "(no description)" : section.Description!;
        return $"Section {section.SectionIndex} ({section.SectionSlug}): {section.Title} — {description}";
    }

    private static string BuildScrollSynopsis(FictionChapterScroll? scroll)
    {
        if (scroll is null)
        {
            return "(scroll synopsis unavailable)";
        }

        var synopsis = string.IsNullOrWhiteSpace(scroll.Synopsis) ? "(no synopsis)" : scroll.Synopsis!;
        return $"{scroll.Title} ({scroll.ScrollSlug}): {synopsis}";
    }

    private static string? SerializeJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
