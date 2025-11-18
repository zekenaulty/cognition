using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Api.Infrastructure.Planning;

public interface IFictionPlanCreator
{
    Task<FictionPlan> CreatePlanAsync(FictionPlanCreationOptions options, CancellationToken cancellationToken = default);
}

public sealed record FictionPlanCreationOptions(
    Guid? ProjectId,
    string? ProjectTitle,
    string? ProjectLogline,
    string PlanName,
    string? PlanDescription,
    string? BranchSlug,
    Guid PersonaId,
    Guid? AgentId);

public sealed class FictionPlanCreator : IFictionPlanCreator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static readonly IReadOnlyList<PlanSeed> BacklogSeeds =
    [
        new PlanSeed(
            BacklogId: "vision-plan",
            Description: "Capture the overarching fiction plan vision.",
            ToolName: "fiction.weaver.visionPlanner",
            Inputs: Array.Empty<string>(),
            Outputs: new[] { "vision-plan" }),
        new PlanSeed(
            BacklogId: "world-bible",
            Description: "Refresh world bible entries and lore requirements.",
            ToolName: "fiction.weaver.worldBibleManager",
            Inputs: new[] { "vision-plan" },
            Outputs: new[] { "world-bible" }),
        new PlanSeed(
            BacklogId: "iteration-plan",
            Description: "Generate the first iteration plan for planners.",
            ToolName: "fiction.weaver.iterativePlanner",
            Inputs: new[] { "world-bible" },
            Outputs: new[] { "iteration-plan" },
            IterationIndex: 1)
    ];

    private readonly CognitionDbContext _db;
    private readonly ILogger<FictionPlanCreator> _logger;

    public FictionPlanCreator(CognitionDbContext db, ILogger<FictionPlanCreator> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FictionPlan> CreatePlanAsync(FictionPlanCreationOptions options, CancellationToken cancellationToken = default)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var planName = options.PlanName?.Trim();
        if (string.IsNullOrWhiteSpace(planName))
        {
            throw new ValidationException("Plan name is required.");
        }

        var branch = NormalizeBranch(options.BranchSlug);
        var now = DateTime.UtcNow;

        var project = await ResolveProjectAsync(options, cancellationToken).ConfigureAwait(false);
        var persona = await _db.Personas
            .FirstOrDefaultAsync(p => p.Id == options.PersonaId, cancellationToken)
            .ConfigureAwait(false);

        if (persona is null)
        {
            throw new ValidationException("Persona not found. Select an existing persona before creating a plan.");
        }

        var agentId = await ResolveAgentIdAsync(options.AgentId, persona.Id, cancellationToken).ConfigureAwait(false);
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = planName,
            Description = string.IsNullOrWhiteSpace(options.PlanDescription) ? null : options.PlanDescription.Trim(),
            PrimaryBranchSlug = branch,
            Status = FictionPlanStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            Title = $"{plan.Name} Planner",
            Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["planId"] = plan.Id,
                ["planName"] = plan.Name,
                ["projectId"] = project.Id,
                ["projectTitle"] = project.Title
            },
            Participants = new List<ConversationParticipant>()
        };
        conversation.Participants.Add(new ConversationParticipant
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = persona.Id,
            JoinedAtUtc = now
        });

        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = persona.Id,
            Title = $"{plan.Name} Backlog",
            Description = $"Automated plan orchestration for {plan.Name} ({branch}).",
            CreatedAt = now,
            Tasks = new List<ConversationTask>()
        };

        var backlogItems = BuildBacklog(plan.Id, now);
        var conversationTasks = BuildTasks(conversationPlan.Id, plan.Id, conversation.Id, agentId, branch);
        conversationPlan.Tasks.AddRange(conversationTasks);
        plan.CurrentConversationPlanId = conversationPlan.Id;
        plan.CurrentConversationPlan = conversationPlan;
        plan.Backlog.Clear();
        foreach (var item in backlogItems)
        {
            plan.Backlog.Add(item);
        }

        foreach (var backlog in backlogItems)
        {
            _db.FictionPlanBacklogItems.Add(backlog);
        }

        _db.FictionPlans.Add(plan);
        _db.Conversations.Add(conversation);
        _db.ConversationPlans.Add(conversationPlan);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created fiction plan {PlanName} ({PlanId}) on project {ProjectTitle}.", plan.Name, plan.Id, project.Title);

        return plan;
    }

    private async Task<FictionProject> ResolveProjectAsync(FictionPlanCreationOptions options, CancellationToken cancellationToken)
    {
        if (options.ProjectId.HasValue)
        {
            var project = await _db.FictionProjects
                .FirstOrDefaultAsync(p => p.Id == options.ProjectId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (project is null)
            {
                throw new ValidationException("Selected project was not found.");
            }

            if (project.Status == FictionProjectStatus.Archived)
            {
                throw new ValidationException("Cannot create a plan for an archived project.");
            }

            if (!string.IsNullOrWhiteSpace(options.ProjectLogline))
            {
                var trimmed = options.ProjectLogline.Trim();
                if (!string.Equals(project.Logline, trimmed, StringComparison.Ordinal))
                {
                    project.Logline = trimmed;
                    project.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            return project;
        }

        var title = options.ProjectTitle?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Project title is required when creating a new project.");
        }

        var projectLogline = string.IsNullOrWhiteSpace(options.ProjectLogline) ? null : options.ProjectLogline.Trim();
        var newProject = new FictionProject
        {
            Id = Guid.NewGuid(),
            Title = title,
            Logline = projectLogline,
            Status = FictionProjectStatus.Active
        };

        _db.FictionProjects.Add(newProject);
        return newProject;
    }

    private async Task<Guid> ResolveAgentIdAsync(Guid? requestedAgentId, Guid personaId, CancellationToken cancellationToken)
    {
        if (requestedAgentId.HasValue)
        {
            var exists = await _db.Agents.AnyAsync(a => a.Id == requestedAgentId.Value, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new ValidationException("Requested agent was not found.");
            }

            return requestedAgentId.Value;
        }

        var agentId = await _db.Agents
            .Where(a => a.PersonaId == personaId)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!agentId.HasValue)
        {
            throw new ValidationException("No agent is linked to the selected persona. Create an agent and try again.");
        }

        return agentId.Value;
    }

    private static IReadOnlyList<FictionPlanBacklogItem> BuildBacklog(Guid planId, DateTime nowUtc)
    {
        var items = new List<FictionPlanBacklogItem>(BacklogSeeds.Count);
        foreach (var seed in BacklogSeeds)
        {
            var backlog = new FictionPlanBacklogItem
            {
                Id = Guid.NewGuid(),
                FictionPlanId = planId,
                BacklogId = seed.BacklogId,
                Description = seed.Description,
                Status = FictionPlanBacklogStatus.Pending,
                Inputs = seed.Inputs.Count == 0 ? null : seed.Inputs.ToArray(),
                Outputs = seed.Outputs.Count == 0 ? null : seed.Outputs.ToArray(),
                CreatedAtUtc = nowUtc
            };

            items.Add(backlog);
        }

        return items;
    }

    private static IReadOnlyList<ConversationTask> BuildTasks(Guid conversationPlanId, Guid planId, Guid conversationId, Guid agentId, string branch)
    {
        var tasks = new List<ConversationTask>(BacklogSeeds.Count);
        var stepNumber = 1;
        foreach (var seed in BacklogSeeds)
        {
            var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["planId"] = planId,
                ["backlogItemId"] = seed.BacklogId,
                ["conversationId"] = conversationId,
                ["agentId"] = agentId,
                ["branchSlug"] = branch
            };

            if (seed.IterationIndex.HasValue)
            {
                args["iterationIndex"] = seed.IterationIndex.Value;
            }

            var task = new ConversationTask
            {
                Id = Guid.NewGuid(),
                ConversationPlanId = conversationPlanId,
                StepNumber = stepNumber++,
                Thought = seed.Description,
                Goal = seed.Description,
                ToolName = seed.ToolName,
                ArgsJson = JsonSerializer.Serialize(args, SerializerOptions),
                Status = "Pending",
                BacklogItemId = seed.BacklogId,
                CreatedAt = DateTime.UtcNow
            };

            tasks.Add(task);
        }

        return tasks;
    }

    private static string NormalizeBranch(string? branch)
        => string.IsNullOrWhiteSpace(branch) ? "main" : branch.Trim();

    private sealed record PlanSeed(
        string BacklogId,
        string Description,
        string ToolName,
        IReadOnlyList<string> Inputs,
        IReadOnlyList<string> Outputs,
        int? IterationIndex = null);
}
