using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cognition.Clients.Tools.Planning.Fiction;

public sealed class VisionPlannerParameters : PlannerParameters
{
    public VisionPlannerParameters(IDictionary<string, object?> values) : base(values)
    {
    }

    public FictionPlan? Plan => Get<FictionPlan>("plan");
    public Conversation? Conversation => Get<Conversation>("conversation");
    public FictionPhaseExecutionContext? ExecutionContext => Get<FictionPhaseExecutionContext>("executionContext");
    public Guid ProviderId => Get<Guid>("providerId");
    public Guid? ModelId => Get<Guid?>("modelId");

    public static VisionPlannerParameters Create(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext executionContext,
        Guid providerId,
        Guid? modelId)
    {
        return new VisionPlannerParameters(new Dictionary<string, object?>
        {
            ["plan"] = plan,
            ["conversation"] = conversation,
            ["executionContext"] = executionContext,
            ["providerId"] = providerId,
            ["modelId"] = modelId
        });
    }
}

[PlannerCapabilities("planning", "fiction", "vision")]
public sealed class VisionPlannerTool : PlannerBase<VisionPlannerParameters>
{
    private const string VisionPlannerTemplateId = "planner.fiction.vision";

    private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
        name: "Vision Planner",
        description: "Generates the initial creative vision summary for a fiction project.",
        capabilities: new[] { "planning", "fiction", "vision" },
        steps: new[]
        {
            new PlannerStepDescriptor("vision-plan", "Draft Vision Plan", TemplateId: VisionPlannerTemplateId)
        },
        telemetryTags: new Dictionary<string, string>
        {
            ["planner"] = "vision"
        });

    private readonly IAgentService _agentService;

    public VisionPlannerTool(
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

    protected override void ValidateInputs(VisionPlannerParameters parameters)
    {
        if (parameters.Plan is null) throw new ArgumentException("Planner parameters must include the FictionPlan.", nameof(parameters));
        if (parameters.Conversation is null) throw new ArgumentException("Planner parameters must include the Conversation.", nameof(parameters));
        if (parameters.ExecutionContext is null) throw new ArgumentException("Planner parameters must include the execution context.", nameof(parameters));
        if (parameters.ProviderId == Guid.Empty) throw new ArgumentException("Planner parameters must include providerId.", nameof(parameters));
    }

    protected override async Task<PlannerResult> ExecutePlanAsync(PlannerContext context, VisionPlannerParameters parameters, CancellationToken ct)
    {
        var plan = parameters.Plan!;
        var executionContext = parameters.ExecutionContext!;

        var template = await TemplateRepository.GetTemplateAsync(VisionPlannerTemplateId, ct).ConfigureAwait(false);
        var prompt = template is { Length: > 0 } resolvedTemplate
            ? BuildVisionPromptFromTemplate(resolvedTemplate, plan, executionContext)
            : BuildVisionPrompt(plan, executionContext);
        var stopwatch = Stopwatch.StartNew();

        var (reply, messageId) = await _agentService.ChatAsync(
            executionContext.ConversationId,
            executionContext.AgentId,
            parameters.ProviderId,
            parameters.ModelId,
            prompt,
            ct).ConfigureAwait(false);

        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateVisionPayload(reply, plan, executionContext);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var backlogItems = ExtractBacklog(validation.ParsedPayload);
        var artifacts = BuildArtifacts(reply, prompt, validation, messageId, backlogItems);
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
                ["validationStatus"] = validation.Status.ToString()
            })
        };

        return PlannerResult.Success()
            .AddArtifacts(artifacts)
            .AddStep(new PlannerStepRecord("vision-plan", PlannerStepStatus.Completed, stepOutput, stopwatch.Elapsed))
            .AddTranscript(transcript)
            .AddMetric("latencyMs", stopwatch.Elapsed.TotalMilliseconds)
            .SetBacklog(backlogItems)
            .AddDiagnostics("validationSummary", validation.Summary);
    }

    private static string BuildVisionPromptFromTemplate(string template, FictionPlan plan, FictionPhaseExecutionContext context)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["projectTitle"] = string.IsNullOrWhiteSpace(plan.FictionProject?.Title) ? plan.Name : plan.FictionProject!.Title,
            ["branch"] = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug,
            ["description"] = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!,
            ["logline"] = string.IsNullOrWhiteSpace(plan.FictionProject?.Logline) ? "(no logline recorded yet)" : plan.FictionProject!.Logline!
        };

        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, object?> BuildArtifacts(
        string response,
        string prompt,
        FictionResponseValidationResult validation,
        Guid? messageId,
        IReadOnlyList<PlannerBacklogItem> backlog)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["prompt"] = prompt,
            ["rawResponse"] = response,
            ["validationStatus"] = validation.Status.ToString(),
            ["validationSummary"] = validation.Summary,
            ["validationDetails"] = validation.Details,
            ["planningBacklog"] = ConvertBacklogForArtifacts(backlog)
        };

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

    private static IReadOnlyList<PlannerBacklogItem> ExtractBacklog(JToken? payload)
    {
        if (payload is not JObject root || root["planningBacklog"] is not JArray backlogArray)
        {
            return Array.Empty<PlannerBacklogItem>();
        }

        var items = new List<PlannerBacklogItem>(backlogArray.Count);
        foreach (var entry in backlogArray.OfType<JObject>())
        {
            var id = entry.Value<string>("id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var description = entry.Value<string>("description");
            if (string.IsNullOrWhiteSpace(description))
            {
                description = id;
            }

            var status = PlannerBacklogStatusExtensions.Parse(entry.Value<string>("status"));
            var inputs = ExtractTokens(entry["inputs"]);
            var outputs = ExtractTokens(entry["outputs"]);

            items.Add(new PlannerBacklogItem(
                Id: id,
                Description: description!,
                Status: status,
                Inputs: inputs,
                Outputs: outputs));
        }

        return items;
    }

    private static IReadOnlyList<Dictionary<string, object?>> ConvertBacklogForArtifacts(IReadOnlyList<PlannerBacklogItem> backlog)
    {
        if (backlog.Count == 0)
        {
            return Array.Empty<Dictionary<string, object?>>();
        }

        var artifacts = new List<Dictionary<string, object?>>(backlog.Count);
        foreach (var item in backlog)
        {
            artifacts.Add(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = item.Id,
                ["description"] = item.Description,
                ["status"] = item.Status.ToSerializedString(),
                ["inputs"] = item.Inputs,
                ["outputs"] = item.Outputs
            });
        }

        return artifacts;
    }

    private static string[] ExtractTokens(JToken? token)
    {
        if (token is not JArray array || array.Count == 0)
        {
            return Array.Empty<string>();
        }

        return array
            .Select(t => t?.ToString())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToArray();
    }

    private static string BuildVisionPrompt(FictionPlan plan, FictionPhaseExecutionContext context)
    {
        var projectTitle = string.IsNullOrWhiteSpace(plan.FictionProject?.Title) ? plan.Name : plan.FictionProject!.Title;
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var logline = string.IsNullOrWhiteSpace(plan.FictionProject?.Logline) ? "(no logline recorded yet)" : plan.FictionProject!.Logline!;

        return $@"You are the lead creative planner for the fiction project ""{projectTitle}"" on branch ""{branch}"".

Project description:
{description}

Project logline:
{logline}

Return minified JSON with the following shape:
{{
  ""authorSummary"": ""string describing the author persona voice, tone, pacing, and stylistic edges"",
  ""bookGoals"": [""goal 1"", ""goal 2"", ""goal 3""],
  ""coreCast"": [
    {{
      ""name"": ""string"",
      ""role"": ""protagonist|antagonist|ally"",
      ""track"": true,
      ""importance"": ""high|medium|low"",
      ""summary"": ""2-3 sentences covering motivation, flaw, stakes, POV"",
      ""motivation"": ""single sentence"",
      ""arcBeats"": [""beat"", ""beat""],
      ""continuityHooks"": [""obligation"", ""callback""],
      ""notes"": ""continuity notes"",
      ""slug"": ""friendly-slug"",
      ""planPassId"": null
    }}
  ],
  ""supportingCast"": [
    {{
      ""name"": ""string"",
      ""role"": ""support"",
      ""track"": true,
      ""importance"": ""medium"",
      ""summary"": ""sentence"",
      ""notes"": ""continuity notes"",
      ""slug"": ""friendly-slug""
    }}
  ],
  ""loreNeeds"": [
    {{
      ""title"": ""string"",
      ""requirementSlug"": ""friendly-slug"",
      ""status"": ""planned|ready|missing"",
      ""description"": ""what canon or system must exist"",
      ""requiredFor"": [""vision-plan"", ""chapter-blueprint"", ""chapter-scroll"", ""chapter-scene""],
      ""notes"": ""continuity notes"",
      ""track"": true
    }}
  ],
  ""planningBacklog"": [
    {{
      ""id"": ""outline-core-conflicts"",
      ""description"": ""Define the headline conflicts and stakes"",
      ""status"": ""pending"",
      ""inputs"": [],
      ""outputs"": []
    }}
  ],
  ""openQuestions"": [""unknown or risky assumptions""],
  ""worldSeeds"": [""worldbuilding seeds to expand later""]
}}

Flag every persona or lore pillar that must persist by setting ""track"": true so downstream lifecycle services promote them.
Ensure backlog entries capture still-needed planner passes rather than a finished story outline.
Respond with JSON only.";
    }
}
