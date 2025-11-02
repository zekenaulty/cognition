using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Rebus.Handlers;
using Rebus.Extensions;

namespace Cognition.Jobs
{
    public class PlanReadyHandler : IHandleMessages<PlanReady>
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;

        public PlanReadyHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(PlanReady message)
        {
            var branchSlug = string.IsNullOrWhiteSpace(message.BranchSlug) ? "main" : message.BranchSlug.Trim();
            var cancellationToken = Rebus.Pipeline.MessageContext.Current?.GetCancellationToken() ?? CancellationToken.None;

            var conversationPlan = await _db.ConversationPlans
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == message.ConversationPlanId, cancellationToken)
                .ConfigureAwait(false);
            if (conversationPlan is null)
            {
                throw new InvalidOperationException($"Conversation plan {message.ConversationPlanId} not found for conversation {message.ConversationId}.");
            }

            var nextTask = conversationPlan.Tasks
                .OrderBy(t => t.StepNumber)
                .FirstOrDefault(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            if (nextTask is null)
            {
                await _logger.LogAsync(message.ConversationId, nameof(PlanReady), JObject.FromObject(new
                {
                    message.ConversationId,
                    message.AgentId,
                    message.PersonaId,
                    message.ConversationPlanId,
                    message.FictionPlanId,
                    BranchSlug = branchSlug,
                    Note = "No pending tasks remaining"
                })).ConfigureAwait(false);
                return;
            }

            var toolName = nextTask.ToolName;
            if (string.IsNullOrWhiteSpace(toolName))
            {
                throw new InvalidOperationException($"Plan task {nextTask.Id} does not define a tool name.");
            }

            var args = ParseArgs(nextTask.ArgsJson);
            nextTask.Status = "Requested";
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var metadata = BuildMetadata(message.Metadata, conversationPlan.Id, nextTask, branchSlug, args);

            await _logger.LogAsync(message.ConversationId, nameof(PlanReady), JObject.FromObject(new
            {
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.ConversationPlanId,
                message.FictionPlanId,
                BranchSlug = branchSlug,
                TaskId = nextTask.Id,
                nextTask.StepNumber,
                Tool = toolName,
                Args = args
            })).ConfigureAwait(false);

            var toolRequested = new ToolExecutionRequested(
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                toolName,
                args,
                message.ConversationPlanId,
                nextTask.StepNumber,
                message.FictionPlanId,
                branchSlug,
                metadata);

            cancellationToken.ThrowIfCancellationRequested();
            await _bus.Publish(toolRequested).ConfigureAwait(false);
        }

        private static Dictionary<string, object?> ParseArgs(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
                return dictionary ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static Dictionary<string, object?> BuildMetadata(Dictionary<string, object?>? source, Guid conversationPlanId, ConversationTask task, string branchSlug, IReadOnlyDictionary<string, object?> args)
        {
            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["conversationPlanId"] = conversationPlanId,
                ["taskId"] = task.Id,
                ["stepNumber"] = task.StepNumber,
                ["branchSlug"] = branchSlug,
                ["toolName"] = task.ToolName,
                ["goal"] = task.Goal
            };

            if (!string.IsNullOrWhiteSpace(task.Thought))
            {
                metadata["thought"] = task.Thought;
            }

            if (source is not null)
            {
                foreach (var kvp in source)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            TryAddMetadataValue(metadata, args, "backlogItemId");
            TryAddMetadataValue(metadata, args, "worldBibleId");
            TryAddMetadataValue(metadata, args, "iterationIndex");

            return metadata;
        }

        private static void TryAddMetadataValue(IDictionary<string, object?> metadata, IReadOnlyDictionary<string, object?> args, string key)
        {
            if (!args.TryGetValue(key, out var value) || value is null)
            {
                return;
            }

            string? text = value switch
            {
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
                JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetRawText(),
                JsonElement element when element.ValueKind == JsonValueKind.Null => null,
                _ => value.ToString()
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                metadata[key] = text;
            }
        }
    }
}
