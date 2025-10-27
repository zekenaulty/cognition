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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cognition.Clients.Tools.Planning.Fiction;

public sealed class ScrollRefinerPlannerParameters : PlannerParameters
{
    public ScrollRefinerPlannerParameters(IDictionary<string, object?> values) : base(values)
    {
    }

    public FictionPlan? Plan => Get<FictionPlan>("plan");
    public Conversation? Conversation => Get<Conversation>("conversation");
    public FictionPhaseExecutionContext? ExecutionContext => Get<FictionPhaseExecutionContext>("executionContext");
    public Guid ProviderId => Get<Guid>("providerId");
    public Guid? ModelId => Get<Guid?>("modelId");
    public FictionChapterBlueprint? Blueprint => Get<FictionChapterBlueprint>("blueprint");
    public FictionChapterScroll? ExistingScroll => Get<FictionChapterScroll>("existingScroll");

    public static ScrollRefinerPlannerParameters Create(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext executionContext,
        Guid providerId,
        Guid? modelId,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        return new ScrollRefinerPlannerParameters(new Dictionary<string, object?>
        {
            ["plan"] = plan,
            ["conversation"] = conversation,
            ["executionContext"] = executionContext,
            ["providerId"] = providerId,
            ["modelId"] = modelId,
            ["blueprint"] = blueprint,
            ["existingScroll"] = existingScroll
        });
    }
}

[PlannerCapabilities("planning", "fiction", "scroll")]
public sealed class ScrollRefinerPlannerTool : PlannerBase<ScrollRefinerPlannerParameters>
{
    private const string TemplateId = "planner.fiction.scrollRefiner";

    private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
        name: "Scroll Refiner",
        description: "Expands a chapter blueprint into a scroll structure that downstream scene planners consume.",
        capabilities: new[] { "planning", "fiction", "scroll" },
        steps: new[]
        {
            new PlannerStepDescriptor("scroll-refinement", "Draft Chapter Scroll", TemplateId: TemplateId)
        },
        telemetryTags: new Dictionary<string, string>
        {
            ["planner"] = "scroll-refiner"
        });

    private readonly IAgentService _agentService;

    public ScrollRefinerPlannerTool(
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

    protected override void ValidateInputs(ScrollRefinerPlannerParameters parameters)
    {
        if (parameters.Plan is null) throw new ArgumentException("Planner parameters must include the FictionPlan.", nameof(parameters));
        if (parameters.Conversation is null) throw new ArgumentException("Planner parameters must include the Conversation.", nameof(parameters));
        if (parameters.ExecutionContext is null) throw new ArgumentException("Planner parameters must include the execution context.", nameof(parameters));
        if (parameters.ProviderId == Guid.Empty) throw new ArgumentException("Planner parameters must include providerId.", nameof(parameters));
    }

    protected override async Task<PlannerResult> ExecutePlanAsync(PlannerContext context, ScrollRefinerPlannerParameters parameters, CancellationToken ct)
    {
        var plan = parameters.Plan!;
        var executionContext = parameters.ExecutionContext!;

        var template = await TemplateRepository.GetTemplateAsync(TemplateId, ct).ConfigureAwait(false);
        var prompt = template is { Length: > 0 } resolvedTemplate
            ? BuildPromptFromTemplate(resolvedTemplate, plan, executionContext, parameters.Blueprint, parameters.ExistingScroll)
            : BuildFallbackPrompt(plan, executionContext, parameters.Blueprint, parameters.ExistingScroll);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (reply, messageId) = await _agentService.ChatAsync(
            executionContext.ConversationId,
            executionContext.AgentId,
            parameters.ProviderId,
            parameters.ModelId,
            prompt,
            ct).ConfigureAwait(false);

        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateScrollPayload(reply, plan, executionContext);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var artifacts = BuildArtifacts(reply, prompt, validation, parameters.Blueprint, parameters.ExistingScroll);
        var stepOutput = new Dictionary<string, object?>
        {
            ["prompt"] = prompt,
            ["response"] = reply,
            ["messageId"] = messageId
        };

        var transcript = new[]
        {
            new PlannerTranscriptEntry(DateTime.UtcNow, "system", prompt),
            new PlannerTranscriptEntry(DateTime.UtcNow, "assistant", reply, new Dictionary<string, object?>
            {
                ["validationStatus"] = validation.Status.ToString(),
                ["chapterBlueprintId"] = executionContext.ChapterBlueprintId,
                ["chapterScrollId"] = executionContext.ChapterScrollId
            })
        };

        return PlannerResult.Success()
            .AddArtifacts(artifacts)
            .AddStep(new PlannerStepRecord("scroll-refinement", PlannerStepStatus.Completed, stepOutput, stopwatch.Elapsed))
            .AddTranscript(transcript)
            .AddMetric("latencyMs", stopwatch.Elapsed.TotalMilliseconds)
            .AddDiagnostics("validationSummary", validation.Summary)
            .AddDiagnostics("validationStatus", validation.Status.ToString());
    }

    private static string BuildPromptFromTemplate(
        string template,
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planName"] = plan.Name,
            ["branch"] = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug,
            ["description"] = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!,
            ["blueprintSynopsis"] = BuildBlueprintSynopsis(blueprint),
            ["blueprintStructure"] = BuildBlueprintStructure(blueprint),
            ["scrollSummary"] = BuildExistingScrollSummary(existingScroll)
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
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"You are refining the chapter scroll for project \"{plan.Name}\" on branch \"{(string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug)}\".");
        builder.AppendLine();
        builder.AppendLine("Blueprint synopsis:");
        builder.AppendLine(BuildBlueprintSynopsis(blueprint));
        builder.AppendLine();
        builder.AppendLine("Blueprint structure (JSON):");
        builder.AppendLine(BuildBlueprintStructure(blueprint));
        builder.AppendLine();
        builder.AppendLine("Existing scroll snapshot:");
        builder.AppendLine(BuildExistingScrollSummary(existingScroll));
        builder.AppendLine();
        builder.AppendLine("Produce minified JSON with the following structure:");
        builder.AppendLine("{");
        builder.AppendLine("  \"scrollSlug\": \"string\",");
        builder.AppendLine("  \"title\": \"string\",");
        builder.AppendLine("  \"synopsis\": \"string\",");
        builder.AppendLine("  \"sections\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"sectionSlug\": \"string\",");
        builder.AppendLine("      \"title\": \"string\",");
        builder.AppendLine("      \"summary\": \"string\",");
        builder.AppendLine("      \"transitions\": [\"string\"],");
        builder.AppendLine("      \"scenes\": [");
        builder.AppendLine("        {");
        builder.AppendLine("          \"sceneSlug\": \"string\",");
        builder.AppendLine("          \"title\": \"string\",");
        builder.AppendLine("          \"goal\": \"string\",");
        builder.AppendLine("          \"conflict\": \"string\",");
        builder.AppendLine("          \"turn\": \"string\",");
        builder.AppendLine("          \"fallout\": \"string\",");
        builder.AppendLine("          \"carryForward\": [\"string\"]");
        builder.AppendLine("        }");
        builder.AppendLine("      ]");
        builder.AppendLine("    }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Ensure the scroll aligns with the blueprint beats and that continuity notes flag canonical obligations. Respond with JSON only.");
        return builder.ToString();
    }

    private static IReadOnlyDictionary<string, object?> BuildArtifacts(
        string response,
        string prompt,
        FictionResponseValidationResult validation,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["rawResponse"] = response,
            ["validationStatus"] = validation.Status.ToString(),
            ["validationSummary"] = validation.Summary,
            ["validationDetails"] = validation.Details
        };

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

        if (existingScroll is not null)
        {
            data["existingScrollId"] = existingScroll.Id;
        }

        try
        {
            if (validation.ParsedPayload is JToken token)
            {
                using var doc = JsonDocument.Parse(token.ToString(Formatting.None));
                data["parsed"] = doc.RootElement.Clone();
            }
            else
            {
                using var doc = JsonDocument.Parse(response);
                data["parsed"] = doc.RootElement.Clone();
            }
        }
        catch (JsonReaderException ex)
        {
            data["parseError"] = ex.Message;
        }
        catch (System.Text.Json.JsonException ex)
        {
            data["parseError"] = ex.Message;
        }

        return data;
    }

    private static string BuildBlueprintSynopsis(FictionChapterBlueprint? blueprint)
    {
        if (blueprint is null)
        {
            return "No chapter blueprint exists yet.";
        }

        return $"Blueprint {blueprint.ChapterIndex} ({blueprint.ChapterSlug}): {blueprint.Title} — {blueprint.Synopsis}";
    }

    private static string BuildBlueprintStructure(FictionChapterBlueprint? blueprint)
    {
        if (blueprint?.Structure is null)
        {
            return "(structure not captured yet)";
        }

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(blueprint.Structure);
        }
        catch (Exception)
        {
            return "(unable to serialise blueprint structure)";
        }
    }

    private static string BuildExistingScrollSummary(FictionChapterScroll? existingScroll)
    {
        if (existingScroll is null || existingScroll.Sections is null || existingScroll.Sections.Count == 0)
        {
            return "No prior scroll revisions exist for this chapter.";
        }

        var builder = new StringBuilder();
        foreach (var section in existingScroll.Sections.OrderBy(s => s.SectionIndex))
        {
            builder.AppendLine($"Section {section.SectionIndex}: {section.Title} — {section.Description ?? "(no description)"}");
            if (section.Scenes is null || section.Scenes.Count == 0)
            {
                builder.AppendLine("  (no scenes)");
                continue;
            }

            foreach (var scene in section.Scenes.OrderBy(s => s.SceneIndex))
            {
                builder.AppendLine($"  Scene {scene.SceneIndex}: {scene.Title} — {scene.Description ?? "(no description)"}");
            }
        }

        return builder.ToString();
    }
}
