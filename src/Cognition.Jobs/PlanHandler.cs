using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Rebus.Handlers;
using Rebus.Extensions;

namespace Cognition.Jobs
{
    public class PlanHandler : IHandleMessages<PlanRequested>
    {
        private static readonly JsonSerializerOptions PlanSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;

        public PlanHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(PlanRequested message)
        {
            var branchSlug = string.IsNullOrWhiteSpace(message.BranchSlug) ? "main" : message.BranchSlug.Trim();
            var cancellationToken = Rebus.Pipeline.MessageContext.Current?.GetCancellationToken() ?? CancellationToken.None;

            var conversation = await _db.Conversations
                .Include(c => c.Agent)
                .FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken)
                .ConfigureAwait(false);
            if (conversation is null)
            {
                throw new InvalidOperationException($"Conversation {message.ConversationId} not found for plan request.");
            }

            if (conversation.AgentId != message.AgentId)
            {
                throw new InvalidOperationException($"Plan request agent mismatch for conversation {message.ConversationId} (expected {conversation.AgentId}, received {message.AgentId}).");
            }

            var fictionPlan = await _db.Set<FictionPlan>()
                .Include(p => p.FictionProject)
                .FirstOrDefaultAsync(p => p.Id == message.FictionPlanId, cancellationToken)
                .ConfigureAwait(false);
            if (fictionPlan is null)
            {
                throw new InvalidOperationException($"Fiction plan {message.FictionPlanId} was not found.");
            }

            var planSteps = BuildPlanSteps(message, branchSlug);
            var planJson = JsonSerializer.Serialize(new { Sequence = planSteps }, PlanSerializerOptions);

            var conversationPlan = new ConversationPlan
            {
                ConversationId = message.ConversationId,
                PersonaId = message.PersonaId,
                Title = string.IsNullOrWhiteSpace(fictionPlan.Name) ? $"Fiction plan {branchSlug}" : fictionPlan.Name!,
                Description = $"FictionWeaver pipeline for branch {branchSlug}",
                OutlineJson = planJson,
                CreatedAt = DateTime.UtcNow,
                Tasks = new List<ConversationTask>()
            };

            var taskSummaries = new List<object>();
            var stepNumber = 1;
            foreach (var step in planSteps)
            {
                var argsJson = JsonSerializer.Serialize(step.Args, PlanSerializerOptions);
                var thoughtPayload = JsonSerializer.Serialize(new
                {
                    step.ToolName,
                    step.BacklogItemId,
                    step.Goal
                }, PlanSerializerOptions);

                conversationPlan.Tasks.Add(new ConversationTask
                {
                    StepNumber = stepNumber,
                    Thought = thoughtPayload,
                    Goal = step.Goal,
                    ToolName = step.ToolName,
                    ArgsJson = argsJson,
                    Status = "Pending",
                    BacklogItemId = step.BacklogItemId,
                    CreatedAt = DateTime.UtcNow
                });

                taskSummaries.Add(new
                {
                    stepNumber,
                    tool = step.ToolName,
                    backlogItemId = step.BacklogItemId,
                    args = step.Args
                });

                stepNumber++;
            }

            _db.ConversationPlans.Add(conversationPlan);
            fictionPlan.CurrentConversationPlanId = conversationPlan.Id;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _logger.LogAsync(message.ConversationId, nameof(PlanRequested), JObject.FromObject(new
            {
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.ProviderId,
                message.ModelId,
                message.FictionPlanId,
                BranchSlug = branchSlug,
                conversationPlanId = conversationPlan.Id,
                Steps = taskSummaries
            })).ConfigureAwait(false);

            var metadata = MergeMetadata(message.Metadata, conversationPlan.Id, branchSlug, taskSummaries);

            var planReady = new PlanReady(
                message.ConversationId,
                message.AgentId,
                message.PersonaId,
                message.ProviderId,
                message.ModelId,
                new ToolPlan(planJson),
                conversationPlan.Id,
                message.FictionPlanId,
                branchSlug,
                metadata);

            cancellationToken.ThrowIfCancellationRequested();
            await _bus.Publish(planReady).ConfigureAwait(false);
        }

        private static IReadOnlyList<PlanStepDefinition> BuildPlanSteps(PlanRequested message, string branchSlug)
        {
            var baseArgs = new
            {
                planId = message.FictionPlanId,
                agentId = message.AgentId,
                conversationId = message.ConversationId,
                providerId = message.ProviderId,
                modelId = message.ModelId,
                branchSlug
            };

            var steps = new List<PlanStepDefinition>
            {
                new(
                    "fiction.weaver.visionPlanner",
                    FictionBacklogTokens.VisionPlan,
                    new
                    {
                        baseArgs.planId,
                        baseArgs.agentId,
                        baseArgs.conversationId,
                        baseArgs.providerId,
                        baseArgs.modelId,
                        baseArgs.branchSlug,
                        backlogItemId = FictionBacklogTokens.VisionPlan
                    },
                    "Capture project vision summary"),
                new(
                    "fiction.weaver.worldBibleManager",
                    FictionBacklogTokens.WorldBible,
                    new
                    {
                        baseArgs.planId,
                        baseArgs.agentId,
                        baseArgs.conversationId,
                        baseArgs.providerId,
                        baseArgs.modelId,
                        baseArgs.branchSlug,
                        backlogItemId = FictionBacklogTokens.WorldBible
                    },
                    "Refresh world bible state"),
                new(
                    "fiction.weaver.iterativePlanner",
                    FictionBacklogTokens.IterationPlan,
                    new
                    {
                        baseArgs.planId,
                        baseArgs.agentId,
                        baseArgs.conversationId,
                        baseArgs.providerId,
                        baseArgs.modelId,
                        baseArgs.branchSlug,
                        iterationIndex = 1,
                        backlogItemId = FictionBacklogTokens.IterationPlan
                    },
                    "Generate iterative planning pass")
            };

            return steps;
        }

        private static Dictionary<string, object?> MergeMetadata(Dictionary<string, object?>? source, Guid conversationPlanId, string branchSlug, IEnumerable<object> steps)
        {
            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["conversationPlanId"] = conversationPlanId,
                ["branchSlug"] = branchSlug,
                ["steps"] = steps.ToList()
            };

            if (source is not null)
            {
                foreach (var kvp in source)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            return metadata;
        }

        private sealed record PlanStepDefinition(string ToolName, string BacklogItemId, object Args, string Goal);
    }
}
