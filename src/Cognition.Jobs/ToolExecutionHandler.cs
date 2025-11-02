using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Rebus.Handlers;
using Rebus.Extensions;

namespace Cognition.Jobs
{
    public class ToolExecutionHandler : IHandleMessages<ToolExecutionRequested>
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        private readonly CognitionDbContext _db;
        private readonly IToolDispatcher _dispatcher;
        private readonly IFictionWeaverJobClient _weaverJobs;
        private readonly IServiceProvider _services;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;
        private readonly IPlannerQuotaService _plannerQuotas;
        private readonly IPlannerTelemetry _telemetry;

        public ToolExecutionHandler(
            CognitionDbContext db,
            IToolDispatcher dispatcher,
            IFictionWeaverJobClient weaverJobs,
            IServiceProvider services,
            Rebus.Bus.IBus bus,
            WorkflowEventLogger logger,
            IPlannerQuotaService plannerQuotas,
            IPlannerTelemetry telemetry)
        {
            _db = db;
            _dispatcher = dispatcher;
            _weaverJobs = weaverJobs;
            _services = services;
            _bus = bus;
            _logger = logger;
            _plannerQuotas = plannerQuotas ?? throw new ArgumentNullException(nameof(plannerQuotas));
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }

        public async Task Handle(ToolExecutionRequested message)
        {
            var branchSlug = string.IsNullOrWhiteSpace(message.BranchSlug) ? "main" : message.BranchSlug.Trim();
            var args = message.Args ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var cancellationToken = Rebus.Pipeline.MessageContext.Current?.GetCancellationToken() ?? CancellationToken.None;

            ConversationTask? task = null;
            if (message.ConversationPlanId.HasValue)
            {
                task = await _db.ConversationTasks
                    .FirstOrDefaultAsync(t => t.ConversationPlanId == message.ConversationPlanId.Value && t.StepNumber == message.StepNumber, cancellationToken)
                    .ConfigureAwait(false);
            }

            var quotaDecision = EvaluatePlannerQuota(message, args);
            if (!quotaDecision.IsAllowed)
            {
                var quotaMetadata = BuildMetadata(message.Metadata, task, branchSlug, args);
                await HandlePlannerQuotaExceededAsync(message, args, branchSlug, task, quotaMetadata, quotaDecision, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (task is not null)
            {
                task.Status = "Running";
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var metadata = BuildMetadata(message.Metadata, task, branchSlug, args);

            bool success;
            string? error;
            object? result;

            try
            {
                if (IsWeaverTool(message.Tool))
                {
                    result = ExecuteWeaverJob(message, args, branchSlug, metadata);
                    success = true;
                    error = null;

                    if (task is not null)
                    {
                        task.Status = "Queued";
                        task.Observation = result is IDictionary<string, object?> dict && dict.TryGetValue("jobId", out var jobObj) ? jobObj?.ToString() : null;
                        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var outcome = await ExecuteToolAsync(message, args, cancellationToken).ConfigureAwait(false);
                    success = outcome.Success;
                    error = outcome.Error;
                    result = outcome.Result;

                    if (task is not null)
                    {
                        task.Status = success ? "Completed" : "Failed";
                        task.Observation = success ? TruncateObservation(outcome.Result) : error;
                        task.Error = error;
                        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                success = false;
                error = ex.Message;
                result = null;

                if (task is not null)
                {
                    task.Status = "Failed";
                    task.Error = ex.Message;
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await _logger.LogAsync(message.ConversationId, nameof(ToolExecutionRequested), JObject.FromObject(new
            {
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.Tool,
                message.ConversationPlanId,
                message.StepNumber,
                message.FictionPlanId,
                BranchSlug = branchSlug,
                Success = success,
                Error = error,
                Args = args
            })).ConfigureAwait(false);

            var toolCompleted = new ToolExecutionCompleted(
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.Tool,
                result,
                success,
                error,
                message.ConversationPlanId,
                message.StepNumber,
                message.FictionPlanId,
                branchSlug,
                metadata);

            cancellationToken.ThrowIfCancellationRequested();
            await _bus.Publish(toolCompleted).ConfigureAwait(false);
        }

        private static bool IsWeaverTool(string tool) => tool.StartsWith("fiction.weaver.", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, object?> ExecuteWeaverJob(ToolExecutionRequested message, IDictionary<string, object?> args, string branchSlug, IDictionary<string, object?> metadata)
        {
            var planId = ReadGuid(args, "planId");
            var providerId = ReadGuid(args, "providerId");
            var agentId = message.AgentId;
            var conversationId = message.ConversationId;
            var modelId = ReadNullableGuid(args, "modelId");
            var metaStrings = ToStringDictionary(metadata);

            string jobId = message.Tool switch
            {
                "fiction.weaver.visionPlanner" => _weaverJobs.EnqueueVisionPlanner(planId, agentId, conversationId, providerId, modelId, branchSlug, metaStrings),
                "fiction.weaver.worldBibleManager" => _weaverJobs.EnqueueWorldBibleManager(planId, agentId, conversationId, providerId, modelId, branchSlug, metaStrings),
                "fiction.weaver.iterativePlanner" => _weaverJobs.EnqueueIterativePlanner(planId, agentId, conversationId, ReadInt(args, "iterationIndex"), providerId, modelId, branchSlug, metaStrings),
                "fiction.weaver.chapterArchitect" => _weaverJobs.EnqueueChapterArchitect(planId, agentId, conversationId, ReadGuid(args, "chapterBlueprintId"), providerId, modelId, branchSlug, metaStrings),
                "fiction.weaver.scrollRefiner" => _weaverJobs.EnqueueScrollRefiner(planId, agentId, conversationId, ReadGuid(args, "chapterScrollId"), providerId, modelId, branchSlug, metaStrings),
                "fiction.weaver.sceneWeaver" => _weaverJobs.EnqueueSceneWeaver(planId, agentId, conversationId, ReadGuid(args, "chapterSceneId"), providerId, modelId, branchSlug, metaStrings),
                _ => throw new InvalidOperationException($"Unknown fiction weaver tool '{message.Tool}'.")
            };

            return new Dictionary<string, object?>
            {
                ["jobId"] = jobId,
                ["tool"] = message.Tool,
                ["planId"] = planId,
                ["branchSlug"] = branchSlug
            };
        }

        private async Task<(bool Success, object? Result, string? Error)> ExecuteToolAsync(ToolExecutionRequested message, IDictionary<string, object?> args, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(message.Tool, out var toolId))
            {
                throw new InvalidOperationException($"Unable to resolve tool identifier '{message.Tool}'.");
            }

            var context = new ToolContext(message.AgentId, message.ConversationId, message.PersonaId, _services, cancellationToken);
            var (ok, result, error) = await _dispatcher.ExecuteAsync(toolId, context, args, log: true).ConfigureAwait(false);

            return (ok, result, error);
        }

        private PlannerQuotaDecision EvaluatePlannerQuota(ToolExecutionRequested message, IDictionary<string, object?> args)
        {
            if (!IsWeaverTool(message.Tool))
            {
                return PlannerQuotaDecision.Allowed();
            }

            var iterationIndex = TryReadIterationIndex(args);
            var context = new PlannerQuotaContext(IterationIndex: iterationIndex);
            return _plannerQuotas.Evaluate(message.Tool, context, message.PersonaId);
        }

        private async Task HandlePlannerQuotaExceededAsync(
            ToolExecutionRequested message,
            IDictionary<string, object?> args,
            string branchSlug,
            ConversationTask? task,
            Dictionary<string, object?> metadata,
            PlannerQuotaDecision decision,
            CancellationToken cancellationToken)
        {
            var plannerKey = message.Tool;
            var errorMessage = decision.Reason ?? $"Planner quota '{decision.Limit}' exceeded.";

        var saveToken = cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken;

        if (task is not null)
        {
            task.Status = decision.Limit == PlannerQuotaLimit.MaxIterations ? "Throttled" : "Rejected";
            task.Error = errorMessage;
            await _db.SaveChangesAsync(saveToken).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        AddQuotaMetadata(metadata, decision);
        var telemetryTags = BuildTelemetryTags(metadata, plannerKey, decision);
        var telemetryContext = BuildTelemetryContext(message, plannerKey, telemetryTags);

            if (decision.Limit == PlannerQuotaLimit.MaxIterations)
            {
                await _telemetry.PlanThrottledAsync(telemetryContext, decision, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _telemetry.PlanRejectedAsync(telemetryContext, decision, cancellationToken).ConfigureAwait(false);
            }

            await _logger.LogAsync(message.ConversationId, nameof(ToolExecutionRequested), JObject.FromObject(new
            {
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.Tool,
                message.ConversationPlanId,
                message.StepNumber,
                message.FictionPlanId,
                BranchSlug = branchSlug,
                Success = false,
                Error = errorMessage,
                Args = args,
                Quota = new
                {
                    decision.Limit,
                    decision.LimitValue,
                    decision.Reason
                }
            })).ConfigureAwait(false);

            var resultPayload = new
            {
                code = "planner_quota_exceeded",
                planner = plannerKey,
                limit = decision.Limit?.ToString(),
                limitValue = decision.LimitValue,
                reason = decision.Reason
            };

            var toolCompleted = new ToolExecutionCompleted(
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.Tool,
                resultPayload,
                false,
                errorMessage,
                message.ConversationPlanId,
                message.StepNumber,
                message.FictionPlanId,
                branchSlug,
                metadata);

            cancellationToken.ThrowIfCancellationRequested();
            await _bus.Publish(toolCompleted).ConfigureAwait(false);
        }

        private static int? TryReadIterationIndex(IDictionary<string, object?> args)
        {
            if (!args.TryGetValue("iterationIndex", out var value) || value is null)
            {
                return null;
            }

            return value switch
            {
                int direct => direct,
                long longValue => (int)longValue,
                JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) => number,
                JsonElement element when element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed) => parsed,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => null
            };
        }

        private static PlannerTelemetryContext BuildTelemetryContext(ToolExecutionRequested message, string plannerKey, IReadOnlyDictionary<string, string>? tags)
        {
            return new PlannerTelemetryContext(
                ToolId: null,
                PlannerName: plannerKey,
                Capabilities: Array.Empty<string>(),
                AgentId: message.AgentId,
                ConversationId: message.ConversationId,
                PrimaryAgentId: message.AgentId,
                Environment: null,
                ScopePath: null,
                SupportsSelfCritique: false,
                TelemetryTags: tags);
        }

        private static IReadOnlyDictionary<string, string>? BuildTelemetryTags(Dictionary<string, object?> metadata, string plannerKey, PlannerQuotaDecision decision)
        {
            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["plannerKey"] = plannerKey
            };

            if (metadata.TryGetValue("branchSlug", out var branch) && branch is not null)
            {
                tags["branch"] = branch.ToString() ?? string.Empty;
            }
            if (metadata.TryGetValue("conversationPlanId", out var planId) && planId is not null)
            {
                tags["conversationPlanId"] = planId.ToString() ?? string.Empty;
            }
            if (metadata.TryGetValue("taskId", out var taskId) && taskId is not null)
            {
                tags["taskId"] = taskId.ToString() ?? string.Empty;
            }
            if (decision.Limit.HasValue)
            {
                tags["quotaLimit"] = decision.Limit.Value.ToString();
            }
            if (decision.LimitValue.HasValue)
            {
                tags["quotaLimitValue"] = decision.LimitValue.Value.ToString();
            }

            return tags.Count == 0 ? null : tags;
        }

        private static void AddQuotaMetadata(Dictionary<string, object?> metadata, PlannerQuotaDecision decision)
        {
            metadata["quotaLimit"] = decision.Limit?.ToString();
            if (decision.LimitValue.HasValue)
            {
                metadata["quotaLimitValue"] = decision.LimitValue.Value;
            }
            if (!string.IsNullOrWhiteSpace(decision.Reason))
            {
                metadata["quotaReason"] = decision.Reason;
            }
        }

        private static Dictionary<string, object?> BuildMetadata(Dictionary<string, object?>? source, ConversationTask? task, string branchSlug, IDictionary<string, object?> args)
        {
            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["branchSlug"] = branchSlug
            };

            if (task is not null)
            {
                metadata["taskId"] = task.Id;
                metadata["stepNumber"] = task.StepNumber;
                metadata["conversationPlanId"] = task.ConversationPlanId;
                metadata["toolName"] = task.ToolName;
                metadata["goal"] = task.Goal;
            }

            if (source is not null)
            {
                foreach (var kvp in source)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            if (args.TryGetValue("backlogItemId", out var backlogId) && backlogId is not null)
            {
                metadata["backlogItemId"] = backlogId.ToString();
            }

            return metadata;
        }

        private static Guid ReadGuid(IDictionary<string, object?> args, string key)
        {
            var value = ReadString(args, key);
            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }
            throw new InvalidOperationException($"Argument '{key}' is required and must be a valid Guid.");
        }

        private static Guid? ReadNullableGuid(IDictionary<string, object?> args, string key)
        {
            var value = ReadString(args, key);
            return Guid.TryParse(value, out var guid) ? guid : null;
        }

        private static int ReadInt(IDictionary<string, object?> args, string key)
        {
            var value = args.TryGetValue(key, out var raw) ? raw : null;
            if (value is null)
            {
                throw new InvalidOperationException($"Argument '{key}' is required and must be an integer.");
            }

            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
                {
                    return number;
                }
                if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
            else if (value is int direct)
            {
                return direct;
            }
            else if (int.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Argument '{key}' must be an integer.");
        }

        private static string? ReadString(IDictionary<string, object?> args, string key)
        {
            if (!args.TryGetValue(key, out var value) || value is null)
            {
                return null;
            }

            return value switch
            {
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
                JsonElement element when element.ValueKind == JsonValueKind.Null => null,
                _ => value.ToString()
            };
        }

        private static IReadOnlyDictionary<string, string>? ToStringDictionary(IDictionary<string, object?>? metadata)
        {
            if (metadata is null || metadata.Count == 0)
            {
                return null;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in metadata)
            {
                if (kvp.Value is null)
                {
                    continue;
                }
                dict[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
            return dict;
        }

        private static string? TruncateObservation(object? result)
        {
            if (result is null)
            {
                return null;
            }

            var text = JsonSerializer.Serialize(result, JsonOptions);
            return text.Length <= 512 ? text : text.Substring(0, 512);
        }
    }
}
