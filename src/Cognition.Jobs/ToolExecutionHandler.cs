using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Rebus.Handlers;

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

        public ToolExecutionHandler(
            CognitionDbContext db,
            IToolDispatcher dispatcher,
            IFictionWeaverJobClient weaverJobs,
            IServiceProvider services,
            Rebus.Bus.IBus bus,
            WorkflowEventLogger logger)
        {
            _db = db;
            _dispatcher = dispatcher;
            _weaverJobs = weaverJobs;
            _services = services;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(ToolExecutionRequested message)
        {
            var branchSlug = string.IsNullOrWhiteSpace(message.BranchSlug) ? "main" : message.BranchSlug.Trim();
            var args = message.Args ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            ConversationTask? task = null;
            if (message.ConversationPlanId.HasValue)
            {
                task = await _db.ConversationTasks
                    .FirstOrDefaultAsync(t => t.ConversationPlanId == message.ConversationPlanId.Value && t.StepNumber == message.StepNumber)
                    .ConfigureAwait(false);
                if (task is not null)
                {
                    task.Status = "Running";
                    await _db.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            var metadata = BuildMetadata(message.Metadata, task, branchSlug);

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
                        await _db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    var outcome = await ExecuteToolAsync(message, args).ConfigureAwait(false);
                    success = outcome.Success;
                    error = outcome.Error;
                    result = outcome.Result;

                    if (task is not null)
                    {
                        task.Status = success ? "Completed" : "Failed";
                        task.Observation = success ? TruncateObservation(outcome.Result) : error;
                        task.Error = error;
                        await _db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
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
                    await _db.SaveChangesAsync().ConfigureAwait(false);
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

        private async Task<(bool Success, object? Result, string? Error)> ExecuteToolAsync(ToolExecutionRequested message, IDictionary<string, object?> args)
        {
            if (!Guid.TryParse(message.Tool, out var toolId))
            {
                throw new InvalidOperationException($"Unable to resolve tool identifier '{message.Tool}'.");
            }

            var context = new ToolContext(message.AgentId, message.ConversationId, message.PersonaId, _services, CancellationToken.None);
            var (ok, result, error) = await _dispatcher.ExecuteAsync(toolId, context, args, log: true).ConfigureAwait(false);

            return (ok, result, error);
        }

        private static Dictionary<string, object?> BuildMetadata(Dictionary<string, object?>? source, ConversationTask? task, string branchSlug)
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
