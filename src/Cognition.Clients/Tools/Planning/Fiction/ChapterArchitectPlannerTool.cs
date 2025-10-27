using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cognition.Clients.Tools.Planning.Fiction;

public sealed class ChapterArchitectPlannerParameters : PlannerParameters
{
    public ChapterArchitectPlannerParameters(IDictionary<string, object?> values) : base(values)
    {
    }

    public FictionPlan? Plan => Get<FictionPlan>("plan");
    public Conversation? Conversation => Get<Conversation>("conversation");
    public FictionPhaseExecutionContext? ExecutionContext => Get<FictionPhaseExecutionContext>("executionContext");
    public Guid ProviderId => Get<Guid>("providerId");
    public Guid? ModelId => Get<Guid?>("modelId");
    public IReadOnlyList<ChapterArchitectPlanPassSummary> Passes => Get<IReadOnlyList<ChapterArchitectPlanPassSummary>>("passes") ?? Array.Empty<ChapterArchitectPlanPassSummary>();
    public FictionChapterBlueprint? ExistingBlueprint => Get<FictionChapterBlueprint>("existingBlueprint");

    public static ChapterArchitectPlannerParameters Create(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext executionContext,
        Guid providerId,
        Guid? modelId,
        IReadOnlyList<ChapterArchitectPlanPassSummary> passes,
        FictionChapterBlueprint? existingBlueprint)
    {
        return new ChapterArchitectPlannerParameters(new Dictionary<string, object?>
        {
            ["plan"] = plan,
            ["conversation"] = conversation,
            ["executionContext"] = executionContext,
            ["providerId"] = providerId,
            ["modelId"] = modelId,
            ["passes"] = passes,
            ["existingBlueprint"] = existingBlueprint
        });
    }
}

public sealed record ChapterArchitectPlanPassSummary(int PassIndex, string Title, string? Summary);

[PlannerCapabilities("planning", "fiction", "chapter-architect")]
public sealed class ChapterArchitectPlannerTool : PlannerBase<ChapterArchitectPlannerParameters>
{
    private const string TemplateId = "planner.fiction.chapterArchitect";

    private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
        name: "Chapter Architect",
        description: "Drafts or refreshes chapter blueprints that downstream scroll/scene runners consume.",
        capabilities: new[] { "planning", "fiction", "chapter" },
        steps: new[]
        {
            new PlannerStepDescriptor("chapter-blueprint", "Draft Chapter Blueprint", TemplateId: TemplateId)
        },
        telemetryTags: new Dictionary<string, string>
        {
            ["planner"] = "chapter-architect"
        });

    private readonly IAgentService _agentService;

    public ChapterArchitectPlannerTool(
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

    protected override void ValidateInputs(ChapterArchitectPlannerParameters parameters)
    {
        if (parameters.Plan is null) throw new ArgumentException("Planner parameters must include the FictionPlan.", nameof(parameters));
        if (parameters.Conversation is null) throw new ArgumentException("Planner parameters must include the Conversation.", nameof(parameters));
        if (parameters.ExecutionContext is null) throw new ArgumentException("Planner parameters must include the execution context.", nameof(parameters));
        if (parameters.ProviderId == Guid.Empty) throw new ArgumentException("Planner parameters must include providerId.", nameof(parameters));
    }

    protected override async Task<PlannerResult> ExecutePlanAsync(PlannerContext context, ChapterArchitectPlannerParameters parameters, CancellationToken ct)
    {
        var plan = parameters.Plan!;
        var executionContext = parameters.ExecutionContext!;

        var template = await TemplateRepository.GetTemplateAsync(TemplateId, ct).ConfigureAwait(false);
        var prompt = template is { Length: > 0 } resolvedTemplate
            ? BuildPromptFromTemplate(resolvedTemplate, plan, executionContext, parameters.Passes, parameters.ExistingBlueprint)
            : BuildFallbackPrompt(plan, executionContext, parameters.Passes, parameters.ExistingBlueprint);

        var stopwatch = Stopwatch.StartNew();

        var (reply, messageId) = await _agentService.ChatAsync(
            executionContext.ConversationId,
            executionContext.AgentId,
            parameters.ProviderId,
            parameters.ModelId,
            prompt,
            ct).ConfigureAwait(false);

        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateBlueprintPayload(reply, plan, executionContext);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var artifacts = BuildArtifacts(reply, prompt, validation, parameters.ExistingBlueprint);
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
                ["chapterBlueprintId"] = executionContext.ChapterBlueprintId
            })
        };

        return PlannerResult.Success()
            .AddArtifacts(artifacts)
            .AddStep(new PlannerStepRecord("chapter-blueprint", PlannerStepStatus.Completed, stepOutput, stopwatch.Elapsed))
            .AddTranscript(transcript)
            .AddMetric("latencyMs", stopwatch.Elapsed.TotalMilliseconds)
            .AddDiagnostics("validationSummary", validation.Summary)
            .AddDiagnostics("validationStatus", validation.Status.ToString());
    }

    private static string BuildPromptFromTemplate(
        string template,
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyList<ChapterArchitectPlanPassSummary> passes,
        FictionChapterBlueprint? existingBlueprint)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planName"] = plan.Name,
            ["branch"] = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug,
            ["description"] = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!,
            ["passesSummary"] = passes.Count == 0 ? "No iterative planning passes recorded yet." : BuildPassesSummary(passes),
            ["existingBlueprintSummary"] = BuildExistingBlueprintSummary(existingBlueprint)
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
        IReadOnlyList<ChapterArchitectPlanPassSummary> passes,
        FictionChapterBlueprint? existingBlueprint)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"You are the chapter architect for the fiction project \"{plan.Name}\" on branch \"{(string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug)}\".");
        builder.AppendLine();
        builder.AppendLine("Project description:");
        builder.AppendLine(string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!);
        builder.AppendLine();
        builder.AppendLine("Planning passes:");
        builder.AppendLine(passes.Count == 0 ? "No iterative planning passes recorded yet." : BuildPassesSummary(passes));
        builder.AppendLine();
        builder.AppendLine("Existing blueprint context:");
        builder.AppendLine(BuildExistingBlueprintSummary(existingBlueprint));
        builder.AppendLine();
        builder.AppendLine("Produce minified JSON with the following structure:");
        builder.AppendLine("{");
        builder.AppendLine("  \"title\": \"string\",");
        builder.AppendLine("  \"synopsis\": \"string\",");
        builder.AppendLine("  \"structure\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"slug\": \"string\",");
        builder.AppendLine("      \"summary\": \"string\",");
        builder.AppendLine("      \"goal\": \"string\",");
        builder.AppendLine("      \"obstacle\": \"string\",");
        builder.AppendLine("      \"turn\": \"string\",");
        builder.AppendLine("      \"fallout\": \"string\",");
        builder.AppendLine("      \"carryForward\": [\"string\"]");
        builder.AppendLine("    }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Each structure entry should capture a major beat for the chapter. Respond with JSON only.");
        return builder.ToString();
    }

    private static string BuildPassesSummary(IReadOnlyList<ChapterArchitectPlanPassSummary> passes)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];
            builder.Append("Pass ");
            builder.Append(pass.PassIndex);
            builder.Append(": ");
            builder.Append(pass.Title);
            builder.Append(" - ");
            builder.Append(string.IsNullOrWhiteSpace(pass.Summary) ? "(no summary)" : pass.Summary);
            if (i < passes.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string BuildExistingBlueprintSummary(FictionChapterBlueprint? blueprint)
        => blueprint is null
            ? "No existing blueprint for this chapter."
            : $"Existing blueprint (index {blueprint.ChapterIndex}, slug {blueprint.ChapterSlug}): {blueprint.Title} - {blueprint.Synopsis}";

    private static IReadOnlyDictionary<string, object?> BuildArtifacts(
        string response,
        string prompt,
        FictionResponseValidationResult validation,
        FictionChapterBlueprint? existingBlueprint)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["rawResponse"] = response,
            ["validationStatus"] = validation.Status.ToString(),
            ["validationSummary"] = validation.Summary,
            ["validationDetails"] = validation.Details
        };

        if (existingBlueprint is not null)
        {
            data["previousBlueprint"] = new
            {
                existingBlueprint.Id,
                existingBlueprint.ChapterSlug,
                existingBlueprint.Title,
                existingBlueprint.Synopsis
            };
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
}
