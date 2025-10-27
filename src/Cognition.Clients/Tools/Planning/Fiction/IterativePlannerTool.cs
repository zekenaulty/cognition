using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Planning.Fiction;

public sealed class IterativePlannerParameters : PlannerParameters
{
    public IterativePlannerParameters(IDictionary<string, object?> values) : base(values)
    {
    }

    public FictionPlan? Plan => Get<FictionPlan>("plan");
    public FictionPhaseExecutionContext? ExecutionContext => Get<FictionPhaseExecutionContext>("executionContext");
    public Guid ProviderId => Get<Guid>("providerId");
    public Guid? ModelId => Get<Guid?>("modelId");
    public IReadOnlyList<IterativePlanPassSummary> ExistingPasses => Get<IReadOnlyList<IterativePlanPassSummary>>("existingPasses") ?? Array.Empty<IterativePlanPassSummary>();

    public static IterativePlannerParameters Create(
        FictionPlan plan,
        FictionPhaseExecutionContext executionContext,
        Guid providerId,
        Guid? modelId,
        IReadOnlyList<IterativePlanPassSummary> existingPasses)
    {
        return new IterativePlannerParameters(new Dictionary<string, object?>
        {
            ["plan"] = plan,
            ["executionContext"] = executionContext,
            ["providerId"] = providerId,
            ["modelId"] = modelId,
            ["existingPasses"] = existingPasses
        });
    }
}

public sealed record IterativePlanPassSummary(int PassIndex, string Title, string? Summary);

[PlannerCapabilities("planning", "fiction", "iteration")]
public sealed class IterativePlannerTool : PlannerBase<IterativePlannerParameters>
{
    private const string IterativePlannerTemplateId = "planner.fiction.iterative";

    private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
        name: "Iterative Planner",
        description: "Produces iteration planning adjustments for ongoing fiction passes.",
        capabilities: new[] { "planning", "fiction", "iteration" },
        steps: new[]
        {
            new PlannerStepDescriptor("iterative-plan", "Draft Iterative Adjustments", TemplateId: IterativePlannerTemplateId)
        },
        telemetryTags: new Dictionary<string, string>
        {
            ["planner"] = "iterative"
        });

    private readonly IAgentService _agentService;

    public IterativePlannerTool(
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

    protected override void ValidateInputs(IterativePlannerParameters parameters)
    {
        if (parameters.Plan is null) throw new ArgumentException("Planner parameters must include the FictionPlan.", nameof(parameters));
        if (parameters.ExecutionContext is null) throw new ArgumentException("Planner parameters must include the execution context.", nameof(parameters));
        if (parameters.ProviderId == Guid.Empty) throw new ArgumentException("Planner parameters must include providerId.", nameof(parameters));
    }

    protected override async Task<PlannerResult> ExecutePlanAsync(PlannerContext context, IterativePlannerParameters parameters, CancellationToken ct)
    {
        var plan = parameters.Plan!;
        var executionContext = parameters.ExecutionContext!;

        var template = await TemplateRepository.GetTemplateAsync(IterativePlannerTemplateId, ct).ConfigureAwait(false);
        var prompt = template is { Length: > 0 } resolvedTemplate
            ? BuildIterativePromptFromTemplate(resolvedTemplate, plan, executionContext, parameters.ExistingPasses)
            : BuildIterativePrompt(plan, executionContext, parameters.ExistingPasses);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await _agentService.ChatAsync(
            executionContext.ConversationId,
            executionContext.AgentId,
            parameters.ProviderId,
            parameters.ModelId,
            prompt,
            ct).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateIterativePayload(reply, plan, executionContext);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var artifacts = BuildArtifacts(reply, prompt, validation, parameters.ExistingPasses);
        var stepOutput = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["response"] = reply,
            ["messageId"] = messageId,
            ["iterationIndex"] = executionContext.IterationIndex
        };

        if (parameters.ExistingPasses.Count > 0)
        {
            stepOutput["existingPasses"] = parameters.ExistingPasses
                .Select(p => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["index"] = p.PassIndex,
                    ["title"] = p.Title,
                    ["summary"] = p.Summary
                })
                .ToList();
        }

        return PlannerResult.Success()
            .AddArtifacts(artifacts)
            .AddStep(new PlannerStepRecord("iterative-plan", PlannerStepStatus.Completed, stepOutput, stopwatch.Elapsed))
            .AddTranscript(new[]
            {
                new PlannerTranscriptEntry(DateTime.UtcNow, "system", prompt, new Dictionary<string, object?>
                {
                    ["iterationIndex"] = executionContext.IterationIndex
                }),
                new PlannerTranscriptEntry(DateTime.UtcNow, "assistant", reply, new Dictionary<string, object?>
                {
                    ["validationStatus"] = validation.Status.ToString(),
                    ["iterationIndex"] = executionContext.IterationIndex
                })
            })
            .AddMetric("latencyMs", stopwatch.Elapsed.TotalMilliseconds)
            .AddDiagnostics("validationSummary", validation.Summary)
            .AddDiagnostics("iterationStatus", validation.Status.ToString());
    }

    private static IReadOnlyDictionary<string, object?> BuildArtifacts(
        string response,
        string prompt,
        FictionResponseValidationResult validation,
        IReadOnlyList<IterativePlanPassSummary> existingPasses)
    {
        var artifacts = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["rawResponse"] = response,
            ["validationStatus"] = validation.Status.ToString(),
            ["validationSummary"] = validation.Summary,
            ["validationDetails"] = validation.Details
        };

        if (existingPasses.Count > 0)
        {
            artifacts["existingPasses"] = existingPasses
                .Select(p => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["index"] = p.PassIndex,
                    ["title"] = p.Title,
                    ["summary"] = p.Summary
                })
                .ToArray();
        }

        try
        {
            using var doc = JsonDocument.Parse(response);
            artifacts["parsed"] = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            artifacts["parseError"] = ex.Message;
        }

        return artifacts;
    }

    private static string BuildIterativePrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyList<IterativePlanPassSummary> existingPasses)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var iterationLabel = context.IterationIndex.HasValue ? context.IterationIndex.Value.ToString() : "(unspecified)";
        var passesSummary = existingPasses.Count == 0
            ? "No previous planning passes recorded yet."
            : BuildPassesSummary(existingPasses);

        return $@"You are running an iterative planning pass (iteration {iterationLabel}) for the fiction project ""{plan.Name}"" on branch ""{branch}"".

Project description:
{description}

Existing planning passes:
{passesSummary}

Produce minified JSON with keys:
{{
  ""storyAdjustments"": [""string""],
  ""characterPriorities"": [""string""],
  ""locationNotes"": [""string""],
  ""systemsConsiderations"": [""string""],
  ""risks"": [""string""]
}}

Each array should contain concrete, actionable items. Respond with JSON only.";
    }

    private static string BuildIterativePromptFromTemplate(
        string template,
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyList<IterativePlanPassSummary> existingPasses)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["planName"] = plan.Name,
            ["branch"] = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug,
            ["description"] = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!,
            ["iterationIndex"] = context.IterationIndex?.ToString() ?? "(unspecified)",
            ["existingPasses"] = existingPasses.Count == 0 ? "No previous planning passes recorded yet." : BuildPassesSummary(existingPasses)
        };

        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string BuildPassesSummary(IReadOnlyList<IterativePlanPassSummary> existingPasses)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < existingPasses.Count; i++)
        {
            var pass = existingPasses[i];
            builder.Append("Pass ");
            builder.Append(pass.PassIndex);
            builder.Append(": ");
            builder.Append(pass.Title);
            builder.Append(" - ");
            builder.Append(string.IsNullOrWhiteSpace(pass.Summary) ? "(no summary)" : pass.Summary);
            if (i < existingPasses.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}


