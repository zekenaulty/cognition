using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs;

public sealed class FictionBacklogScheduler : IFictionBacklogScheduler
{
    private const string DefaultBranch = "main";
    private const string DefaultWorldBibleDomain = "core";
    private static readonly TimeSpan BacklogResumeThreshold = TimeSpan.FromMinutes(60);
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly CognitionDbContext _db;
    private readonly IFictionWeaverJobClient _jobs;
    private readonly ILogger<FictionBacklogScheduler> _logger;
    private readonly FictionAutomationOptions _automationOptions;

    public FictionBacklogScheduler(
        CognitionDbContext db,
        IFictionWeaverJobClient jobs,
        ILogger<FictionBacklogScheduler> logger,
        Microsoft.Extensions.Options.IOptions<FictionAutomationOptions>? automationOptions = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _automationOptions = automationOptions?.Value ?? new FictionAutomationOptions();
    }

    public async Task ScheduleAsync(
        FictionPlan plan,
        FictionPhase completedPhase,
        FictionPhaseResult result,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var backlogItems = await _db.FictionPlanBacklogItems
            .Where(x => x.FictionPlanId == plan.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!TryResolveProvider(context.Metadata, out var providerId))
        {
            _logger.LogWarning("Unable to resolve providerId for plan {PlanId}, skipping scheduling.", plan.Id);
            return;
        }

        var modelId = TryResolveModel(context.Metadata);
        var branch = ResolveBranchSlug(plan, context);

        await AutoResumeStaleBacklogItemsAsync(plan, branch, backlogItems, context, providerId, modelId, cancellationToken).ConfigureAwait(false);
        await AutoQueueLoreFulfillmentAsync(plan, branch, context, providerId, modelId, cancellationToken).ConfigureAwait(false);

        if (backlogItems.Count == 0)
        {
            return;
        }

        var readyItem = FindNextReadyItem(backlogItems);
        if (readyItem is null)
        {
            return;
        }

        // Atomic claim: only proceed if we can move Pending -> InProgress for this item
        int claimed;
        var providerName = _db.Database.ProviderName ?? string.Empty;
        var isInMemory = providerName.IndexOf("InMemory", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isInMemory)
        {
            var pending = backlogItems.FirstOrDefault(x => x.Id == readyItem.Id);
            if (pending != null && pending.Status == FictionPlanBacklogStatus.Pending)
            {
                pending.Status = FictionPlanBacklogStatus.InProgress;
                pending.InProgressAtUtc = DateTime.UtcNow;
                pending.UpdatedAtUtc = DateTime.UtcNow;
                claimed = 1;
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                claimed = 0;
            }
        }
        else
        {
            claimed = await _db.FictionPlanBacklogItems
                .Where(x => x.Id == readyItem.Id && x.Status == FictionPlanBacklogStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.Status, FictionPlanBacklogStatus.InProgress)
                    .SetProperty(i => i.InProgressAtUtc, DateTime.UtcNow)
                    .SetProperty(i => i.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (claimed == 0)
        {
            _logger.LogWarning("Backlog item {BacklogId} could not be claimed (already in progress or complete).", readyItem.BacklogId);
            return;
        }

        // Reload the claimed item to ensure tracking reflects current state
        readyItem = await _db.FictionPlanBacklogItems
            .FirstAsync(x => x.Id == readyItem.Id, cancellationToken)
            .ConfigureAwait(false);

        var phase = ResolvePhase(readyItem);
        if (phase is null)
        {
            _logger.LogWarning("Backlog item {BacklogId} for plan {PlanId} does not map to a runnable phase.", readyItem.BacklogId, plan.Id);
            return;
        }

        await EnsureTargetsAsync(plan.Id, readyItem, backlogItems, branch, cancellationToken).ConfigureAwait(false);

        Guid? conversationPlanId = plan.CurrentConversationPlanId;
        if (!conversationPlanId.HasValue && context.Metadata is not null &&
            context.Metadata.TryGetValue("conversationPlanId", out var conversationPlanRaw) &&
            Guid.TryParse(conversationPlanRaw, out var parsedConversationPlanId))
        {
            conversationPlanId = parsedConversationPlanId;
        }

        var conversationTask = await FindConversationTaskAsync(conversationPlanId, readyItem.BacklogId, cancellationToken).ConfigureAwait(false);
        UpdateConversationTaskMetadata(conversationTask, plan, readyItem, branch, providerId, modelId, context, conversationPlanId);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var metadata = BuildMetadata(context.Metadata, readyItem);
        metadata["providerId"] = providerId.ToString();
        if (modelId.HasValue)
        {
            metadata["modelId"] = modelId.Value.ToString();
        }
        if (context.AgentId != Guid.Empty)
        {
            metadata["agentId"] = context.AgentId.ToString();
        }
        if (context.ConversationId != Guid.Empty)
        {
            metadata["conversationId"] = context.ConversationId.ToString();
        }
        metadata["branchSlug"] = branch;

        if (conversationTask is not null)
        {
            metadata["taskId"] = conversationTask.Id.ToString();
        }

        if (conversationPlanId.HasValue)
        {
            metadata["conversationPlanId"] = conversationPlanId.Value.ToString();
        }
        else if (plan.CurrentConversationPlanId.HasValue)
        {
            metadata["conversationPlanId"] = plan.CurrentConversationPlanId.Value.ToString();
        }
        EnqueuePhase(phase.Value, plan.Id, context, providerId, modelId, branch, readyItem, metadata);
    }

    private static FictionPlanBacklogItem? FindNextReadyItem(IReadOnlyList<FictionPlanBacklogItem> backlog)
    {
        foreach (var item in backlog)
        {
            if (item.Status != FictionPlanBacklogStatus.Pending)
            {
                continue;
            }

            if (ResolvePhase(item) is null)
            {
                continue;
            }

            if (!DependenciesSatisfied(item, backlog))
            {
                continue;
            }

            return item;
        }

        return null;
    }

    private static bool DependenciesSatisfied(FictionPlanBacklogItem target, IReadOnlyList<FictionPlanBacklogItem> backlog)
    {
        if (target.Inputs is null || target.Inputs.Length == 0)
        {
            return true;
        }

        foreach (var input in target.Inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var dependency = backlog.FirstOrDefault(item =>
                item.Outputs is not null &&
                item.Outputs.Any(output => string.Equals(output, input, StringComparison.OrdinalIgnoreCase)));

            if (dependency is not null && dependency.Status != FictionPlanBacklogStatus.Complete)
            {
                return false;
            }
        }

        return true;
    }

    private static FictionPhase? ResolvePhase(FictionPlanBacklogItem item)
        => FictionBacklogPhaseResolver.ResolvePhase(item);

    private async Task EnsureTargetsAsync(
        Guid planId,
        FictionPlanBacklogItem item,
        IReadOnlyList<FictionPlanBacklogItem> backlog,
        string branch,
        CancellationToken cancellationToken)
    {
        var phase = ResolvePhase(item);
        if (phase is null)
        {
            return;
        }

        switch (phase.Value)
        {
            case FictionPhase.ChapterArchitect:
                await EnsureBlueprintAsync(planId, item, cancellationToken).ConfigureAwait(false);
                break;
            case FictionPhase.ScrollRefiner:
                await EnsureScrollAsync(planId, item, backlog, cancellationToken).ConfigureAwait(false);
                break;
            case FictionPhase.SceneWeaver:
                await EnsureSceneAsync(planId, item, backlog, cancellationToken).ConfigureAwait(false);
                break;
            case FictionPhase.WorldBibleManager:
                await EnsureWorldBibleAsync(planId, item, branch, cancellationToken).ConfigureAwait(false);
                break;
            case FictionPhase.IterativePlanner:
                await EnsureIterationMetadataAsync(planId, item, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task AutoResumeStaleBacklogItemsAsync(
        FictionPlan plan,
        string branch,
        IReadOnlyList<FictionPlanBacklogItem> backlogItems,
        FictionPhaseExecutionContext context,
        Guid providerId,
        Guid? modelId,
        CancellationToken cancellationToken)
    {
        if (backlogItems.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var cutoff = now - BacklogResumeThreshold;
        var providerName = _db.Database.ProviderName ?? string.Empty;
        var isInMemory = providerName.IndexOf("InMemory", StringComparison.OrdinalIgnoreCase) >= 0;
        var staleItems = backlogItems
            .Where(item => item.Status == FictionPlanBacklogStatus.InProgress)
            .Where(item =>
            {
                var stamp = item.UpdatedAtUtc ?? item.InProgressAtUtc;
                if (!stamp.HasValue)
                {
                    stamp = item.CreatedAtUtc;
                }

                return stamp.Value <= cutoff;
            })
            .ToList();

        if (staleItems.Count == 0)
        {
            return;
        }

        var backlogIdSet = new HashSet<string>(
            staleItems
                .Select(item => item.BacklogId)
                .Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);

        Dictionary<string, List<ConversationTask>>? taskLookup = null;
        if (plan.CurrentConversationPlanId.HasValue && backlogIdSet.Count > 0)
        {
            var tasks = await _db.ConversationTasks
                .Where(t => t.ConversationPlanId == plan.CurrentConversationPlanId.Value && t.BacklogItemId != null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            taskLookup = tasks
                .Where(t => t.BacklogItemId is not null && backlogIdSet.Contains(t.BacklogItemId))
                .GroupBy(t => t.BacklogItemId!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        foreach (var item in staleItems)
        {
            // Guard: only reset items still marked InProgress
            int resetCount;
            if (isInMemory)
            {
                if (item.Status == FictionPlanBacklogStatus.InProgress)
                {
                    item.Status = FictionPlanBacklogStatus.Pending;
                    item.InProgressAtUtc = null;
                    item.CompletedAtUtc = null;
                    item.UpdatedAtUtc = now;
                    resetCount = 1;
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    resetCount = 0;
                }
            }
            else
            {
                resetCount = await _db.FictionPlanBacklogItems
                    .Where(x => x.Id == item.Id && x.Status == FictionPlanBacklogStatus.InProgress)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.Status, FictionPlanBacklogStatus.Pending)
                        .SetProperty(i => i.InProgressAtUtc, (DateTime?)null)
                        .SetProperty(i => i.CompletedAtUtc, (DateTime?)null)
                        .SetProperty(i => i.UpdatedAtUtc, now),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (resetCount == 0)
            {
                continue;
            }

            var previousTimestamp = item.UpdatedAtUtc ?? item.InProgressAtUtc ?? item.CreatedAtUtc;
            var age = now - previousTimestamp;

            var resolvedProviderId = providerId;
            var resolvedModelId = modelId;
            var resolvedAgentId = context.AgentId == Guid.Empty ? (Guid?)null : context.AgentId;
            Guid? resolvedConversationPlanId = plan.CurrentConversationPlanId;
            Guid? resolvedTaskId = null;

            if (taskLookup is not null &&
                !string.IsNullOrWhiteSpace(item.BacklogId) &&
                taskLookup.TryGetValue(item.BacklogId, out var tasks))
            {
                foreach (var task in tasks)
                {
                    resolvedConversationPlanId ??= task.ConversationPlanId;
                    resolvedTaskId ??= task.Id;
                    if (task.ProviderId.HasValue)
                    {
                        resolvedProviderId = task.ProviderId.Value;
                    }
                    if (task.ModelId.HasValue)
                    {
                        resolvedModelId ??= task.ModelId;
                    }
                    if (task.AgentId.HasValue)
                    {
                        resolvedAgentId ??= task.AgentId;
                    }

                    task.Status = "Pending";
                    task.Error = null;
                    task.Observation = null;
                    task.UpdatedAtUtc = now;
                    task.ProviderId ??= resolvedProviderId;
                    task.ModelId ??= resolvedModelId;
                    task.AgentId ??= context.AgentId == Guid.Empty ? task.AgentId : context.AgentId;
                }
            }

            plan.CurrentConversationPlanId ??= resolvedConversationPlanId;

            LogAutoResumeBacklogAction(
                plan,
                item,
                branch,
                context,
                age,
                resolvedConversationPlanId,
                resolvedProviderId,
                resolvedModelId,
                resolvedAgentId,
                resolvedTaskId);
            _logger.LogWarning(
                "Backlog item {BacklogId} for plan {PlanId} was in progress for {Age:g}. Automatically reset to pending.",
                item.BacklogId,
                plan.Id,
                age);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task AutoQueueLoreFulfillmentAsync(
        FictionPlan plan,
        string branch,
        FictionPhaseExecutionContext context,
        Guid providerId,
        Guid? modelId,
        CancellationToken cancellationToken)
    {
        if (context.ConversationId == Guid.Empty)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var cutoff = now - _automationOptions.LoreAutoFulfillmentSla;
        var requirements = await _db.FictionLoreRequirements
            .Where(r => r.FictionPlanId == plan.Id && r.Status == FictionLoreRequirementStatus.Blocked && r.WorldBibleEntryId == null)
            .Where(r => (r.UpdatedAtUtc ?? r.CreatedAtUtc) <= cutoff)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requirements.Count == 0)
        {
            return;
        }

        var metadataUpdated = false;
        foreach (var requirement in requirements)
        {
            var metadata = LoreRequirementMetadata.FromJson(requirement.MetadataJson);
            var branchContext = metadata.ResolveBranchContext(plan.PrimaryBranchSlug, branch);
            var lastUpdatedAt = requirement.UpdatedAtUtc ?? requirement.CreatedAtUtc;
            var age = now - lastUpdatedAt;
            if (metadata.AutoFulfillmentRequestedUtc.HasValue)
            {
                continue;
            }

            metadata.AutoFulfillmentRequestedUtc = now;
            metadata.BranchSlug ??= branchContext.Slug;
            metadata.BranchLineage ??= branchContext.Lineage;
            if (context.ConversationId != Guid.Empty)
            {
                metadata.AutoFulfillmentConversationId ??= context.ConversationId;
            }
            if (context.AgentId != Guid.Empty)
            {
                metadata.AutoFulfillmentAgentId ??= context.AgentId;
            }
            requirement.MetadataJson = metadata.Serialize();
            metadataUpdated = true;

            var jobMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["autoFulfillment"] = "true",
                ["requirementId"] = requirement.Id.ToString(),
                ["branchSlug"] = branchContext.Slug,
                ["slaMinutes"] = ((int)Math.Round(_automationOptions.LoreAutoFulfillmentSla.TotalMinutes)).ToString(CultureInfo.InvariantCulture)
            };

            if (metadata.BranchLineage is { Count: > 0 })
            {
                jobMetadata["branchLineage"] = string.Join(",", metadata.BranchLineage);
            }

            LogLoreAutomationRequest(plan, requirement, branchContext, context, age);

            _jobs.EnqueueLoreFulfillment(
                plan.Id,
                requirement.Id,
                context.AgentId,
                context.ConversationId,
                providerId,
                modelId,
                branchContext.Slug,
                jobMetadata);

            _logger.LogInformation(
                "Queued lore fulfillment automation for requirement {RequirementId} on plan {PlanId}.",
                requirement.Id,
                plan.Id);
        }

        if (metadataUpdated)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureBlueprintAsync(Guid planId, FictionPlanBacklogItem item, CancellationToken cancellationToken)
    {
        if (TryGetOutputGuid(item, MetadataKeys.ChapterBlueprintId, out _))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var blueprint = new FictionChapterBlueprint
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            ChapterIndex = await _db.FictionChapterBlueprints
                .Where(x => x.FictionPlanId == planId)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false) + 1,
            ChapterSlug = NormalizeSlug(item.BacklogId),
            Title = ResolveBacklogTitle(item),
            Synopsis = string.Empty,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.FictionChapterBlueprints.Add(blueprint);
        SetOutputMetadata(item, MetadataKeys.ChapterBlueprintId, blueprint.Id.ToString("D"));
    }

    private async Task EnsureScrollAsync(
        Guid planId,
        FictionPlanBacklogItem item,
        IReadOnlyList<FictionPlanBacklogItem> backlog,
        CancellationToken cancellationToken)
    {
        if (TryGetOutputGuid(item, MetadataKeys.ChapterScrollId, out _))
        {
            return;
        }

        var blueprintId = ResolveDependencyGuid(item, backlog, MetadataKeys.ChapterBlueprintId);
        if (!blueprintId.HasValue)
        {
            await EnsureBlueprintAsync(planId, item, cancellationToken).ConfigureAwait(false);
            blueprintId = ResolveDependencyGuid(item, backlog, MetadataKeys.ChapterBlueprintId);
        }

        blueprintId ??= Guid.NewGuid();
        var now = DateTime.UtcNow;
        var scroll = new FictionChapterScroll
        {
            Id = Guid.NewGuid(),
            FictionChapterBlueprintId = blueprintId.Value,
            VersionIndex = 1,
            ScrollSlug = $"{NormalizeSlug(item.BacklogId)}-{Guid.NewGuid():N}",
            Title = ResolveBacklogTitle(item),
            Synopsis = string.Empty,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var section = new FictionChapterSection
        {
            Id = Guid.NewGuid(),
            FictionChapterScrollId = scroll.Id,
            SectionIndex = 1,
            SectionSlug = $"{scroll.ScrollSlug}-section",
            Title = scroll.Title,
            Description = scroll.Synopsis,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.FictionChapterScrolls.Add(scroll);
        _db.FictionChapterSections.Add(section);
        SetOutputMetadata(item, MetadataKeys.ChapterScrollId, scroll.Id.ToString("D"));
        SetOutputMetadata(item, MetadataKeys.ChapterSectionId, section.Id.ToString("D"));
        SetOutputMetadata(item, MetadataKeys.ChapterBlueprintId, blueprintId.Value.ToString("D"));
    }

    private async Task EnsureSceneAsync(
        Guid planId,
        FictionPlanBacklogItem item,
        IReadOnlyList<FictionPlanBacklogItem> backlog,
        CancellationToken cancellationToken)
    {
        if (TryGetOutputGuid(item, MetadataKeys.ChapterSceneId, out _))
        {
            return;
        }

        var scrollId = ResolveDependencyGuid(item, backlog, MetadataKeys.ChapterScrollId);
        if (!scrollId.HasValue)
        {
            await EnsureScrollAsync(planId, item, backlog, cancellationToken).ConfigureAwait(false);
            scrollId = ResolveDependencyGuid(item, backlog, MetadataKeys.ChapterScrollId);
        }

        if (!scrollId.HasValue)
        {
            _logger.LogWarning("Unable to resolve scroll for backlog item {BacklogId} on plan {PlanId}.", item.BacklogId, planId);
            return;
        }

        var sectionId = ResolveDependencyGuid(item, backlog, MetadataKeys.ChapterSectionId);
        if (!sectionId.HasValue)
        {
            sectionId = await CreateSectionAsync(scrollId.Value, item, cancellationToken).ConfigureAwait(false);
        }

        var now = DateTime.UtcNow;
        var scene = new FictionChapterScene
        {
            Id = Guid.NewGuid(),
            FictionChapterSectionId = sectionId.Value,
            SceneIndex = await _db.FictionChapterScenes
                .Where(x => x.FictionChapterSectionId == sectionId.Value)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false) + 1,
            SceneSlug = $"{NormalizeSlug(item.BacklogId)}-{Guid.NewGuid():N}",
            Title = string.IsNullOrWhiteSpace(item.Description) ? item.BacklogId : item.Description,
            Description = item.Description,
            Status = FictionSceneStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.FictionChapterScenes.Add(scene);
        SetOutputMetadata(item, MetadataKeys.ChapterSceneId, scene.Id.ToString("D"));
        SetOutputMetadata(item, MetadataKeys.ChapterScrollId, scrollId.Value.ToString("D"));
        SetOutputMetadata(item, MetadataKeys.ChapterSectionId, sectionId.Value.ToString("D"));
    }

    private async Task EnsureWorldBibleAsync(
        Guid planId,
        FictionPlanBacklogItem item,
        string branch,
        CancellationToken cancellationToken)
    {
        if (TryGetOutputGuid(item, MetadataKeys.WorldBibleId, out _))
        {
            return;
        }

        var normalizedBranch = NormalizeBranch(branch);
        var query = _db.FictionWorldBibles
            .Where(x => x.FictionPlanId == planId && x.Domain == DefaultWorldBibleDomain);

        if (string.Equals(normalizedBranch, DefaultBranch, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.BranchSlug == null || x.BranchSlug == normalizedBranch);
        }
        else
        {
            query = query.Where(x => x.BranchSlug == normalizedBranch);
        }

        var worldBible = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (worldBible is null)
        {
            worldBible = new FictionWorldBible
            {
                Id = Guid.NewGuid(),
                FictionPlanId = planId,
                Domain = DefaultWorldBibleDomain,
                BranchSlug = string.Equals(normalizedBranch, DefaultBranch, StringComparison.OrdinalIgnoreCase) ? null : normalizedBranch,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.FictionWorldBibles.Add(worldBible);
        }
        else
        {
            worldBible.UpdatedAtUtc = DateTime.UtcNow;
        }

        SetOutputMetadata(item, MetadataKeys.WorldBibleId, worldBible.Id.ToString("D"));
    }

    private async Task EnsureIterationMetadataAsync(
        Guid planId,
        FictionPlanBacklogItem item,
        CancellationToken cancellationToken)
    {
        if (TryGetOutputMetadata(item, MetadataKeys.IterationIndex, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return;
        }

        var maxIndex = await _db.FictionPlanPasses
            .Where(x => x.FictionPlanId == planId)
            .Select(x => (int?)x.PassIndex)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        var nextIndex = (maxIndex ?? 0) + 1;
        SetOutputMetadata(item, MetadataKeys.IterationIndex, nextIndex.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<Guid> CreateSectionAsync(Guid scrollId, FictionPlanBacklogItem item, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var section = new FictionChapterSection
        {
            Id = Guid.NewGuid(),
            FictionChapterScrollId = scrollId,
            SectionIndex = await _db.FictionChapterSections
                .Where(x => x.FictionChapterScrollId == scrollId)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false) + 1,
            SectionSlug = $"{NormalizeSlug(item.BacklogId)}-section-{Guid.NewGuid():N}",
            Title = ResolveBacklogTitle(item),
            Description = item.Description,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.FictionChapterSections.Add(section);
        return section.Id;
    }

    private static bool TryResolveProvider(IReadOnlyDictionary<string, string>? metadata, out Guid providerId)
    {
        providerId = Guid.Empty;
        if (metadata is null)
        {
            return false;
        }

        if (!metadata.TryGetValue("providerId", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return Guid.TryParse(raw, out providerId);
    }

    private static Guid? TryResolveModel(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        if (!metadata.TryGetValue("modelId", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var modelId) ? modelId : null;
    }

    private static void MarkBacklogInProgress(FictionPlanBacklogItem item)
    {
        item.Status = FictionPlanBacklogStatus.InProgress;
        item.InProgressAtUtc ??= DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Dictionary<string, string> BuildMetadata(IReadOnlyDictionary<string, string>? contextMetadata, FictionPlanBacklogItem item)
    {
        var metadata = contextMetadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(contextMetadata, StringComparer.OrdinalIgnoreCase);

        metadata["backlogItemId"] = item.BacklogId;
        TryAddOutputMetadata(item, metadata, MetadataKeys.ChapterBlueprintId);
        TryAddOutputMetadata(item, metadata, MetadataKeys.ChapterScrollId);
        TryAddOutputMetadata(item, metadata, MetadataKeys.ChapterSectionId);
        TryAddOutputMetadata(item, metadata, MetadataKeys.ChapterSceneId);
        TryAddOutputMetadata(item, metadata, MetadataKeys.WorldBibleId);
        TryAddOutputMetadata(item, metadata, MetadataKeys.IterationIndex);
        return metadata;
    }

    private async Task<ConversationTask?> FindConversationTaskAsync(Guid? conversationPlanId, string? backlogItemId, CancellationToken cancellationToken)
    {
        if (!conversationPlanId.HasValue || string.IsNullOrWhiteSpace(backlogItemId))
        {
            return null;
        }

        return await _db.ConversationTasks
            .FirstOrDefaultAsync(t => t.ConversationPlanId == conversationPlanId.Value && t.BacklogItemId == backlogItemId, cancellationToken)
            .ConfigureAwait(false);
    }

    private void UpdateConversationTaskMetadata(
        ConversationTask? task,
        FictionPlan plan,
        FictionPlanBacklogItem backlog,
        string branch,
        Guid providerId,
        Guid? modelId,
        FictionPhaseExecutionContext context,
        Guid? conversationPlanId)
    {
        if (task is null)
        {
            return;
        }

        var args = DeserializeArgs(task.ArgsJson);
        args["planId"] = plan.Id;
        args["backlogItemId"] = backlog.BacklogId;
        args["conversationId"] = context.ConversationId;
        args["providerId"] = providerId;
        if (context.AgentId != Guid.Empty)
        {
            args["agentId"] = context.AgentId;
        }
        else
        {
            args.Remove("agentId");
        }
        args["branchSlug"] = branch;

        conversationPlanId ??= plan.CurrentConversationPlanId;
        if (!conversationPlanId.HasValue && context.Metadata is not null &&
            context.Metadata.TryGetValue("conversationPlanId", out var conversationPlanRaw) &&
            Guid.TryParse(conversationPlanRaw, out var parsedConversationPlanId))
        {
            conversationPlanId = parsedConversationPlanId;
        }

        if (conversationPlanId.HasValue)
        {
            args["conversationPlanId"] = conversationPlanId.Value;
            plan.CurrentConversationPlanId ??= conversationPlanId.Value;
            task.ConversationPlanId = conversationPlanId.Value;
        }
        else
        {
            args.Remove("conversationPlanId");
        }

        if (modelId.HasValue)
        {
            args["modelId"] = modelId.Value;
        }
        else
        {
            args.Remove("modelId");
        }

        var serialized = JsonSerializer.Serialize(args, MetadataSerializerOptions);
        if (!string.Equals(task.ArgsJson, serialized, StringComparison.Ordinal))
        {
            task.ArgsJson = serialized;
        }

        task.ProviderId ??= providerId;
        task.ModelId ??= modelId;
        if (context.AgentId != Guid.Empty)
        {
            task.AgentId ??= context.AgentId;
        }
        if (!string.IsNullOrWhiteSpace(backlog.BacklogId))
        {
            task.BacklogItemId ??= backlog.BacklogId;
        }
        task.Status = string.IsNullOrWhiteSpace(task.Status) ? "Pending" : task.Status;
        task.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Dictionary<string, object?> DeserializeArgs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, MetadataSerializerOptions);
            return parsed is null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void EnqueuePhase(
        FictionPhase phase,
        Guid planId,
        FictionPhaseExecutionContext context,
        Guid providerId,
        Guid? modelId,
        string branch,
        FictionPlanBacklogItem item,
        IReadOnlyDictionary<string, string> metadata)
    {
        switch (phase)
        {
            case FictionPhase.ChapterArchitect:
                if (!TryGetOutputGuid(item, MetadataKeys.ChapterBlueprintId, out var blueprintId))
                {
                    _logger.LogWarning("Backlog item {BacklogId} missing chapterBlueprintId metadata; skipping queue.", item.BacklogId);
                    return;
                }
                _jobs.EnqueueChapterArchitect(planId, context.AgentId, context.ConversationId, blueprintId, providerId, modelId, branch, metadata);
                _logger.LogInformation("Queued ChapterArchitect for backlog item {BacklogId} on plan {PlanId}.", item.BacklogId, planId);
                break;
            case FictionPhase.ScrollRefiner:
                if (!TryGetOutputGuid(item, MetadataKeys.ChapterScrollId, out var scrollId))
                {
                    _logger.LogWarning("Backlog item {BacklogId} missing chapterScrollId metadata; skipping queue.", item.BacklogId);
                    return;
                }
                _jobs.EnqueueScrollRefiner(planId, context.AgentId, context.ConversationId, scrollId, providerId, modelId, branch, metadata);
                _logger.LogInformation("Queued ScrollRefiner for backlog item {BacklogId} on plan {PlanId}.", item.BacklogId, planId);
                break;
            case FictionPhase.SceneWeaver:
                if (!TryGetOutputGuid(item, MetadataKeys.ChapterSceneId, out var sceneId))
                {
                    _logger.LogWarning("Backlog item {BacklogId} missing chapterSceneId metadata; skipping queue.", item.BacklogId);
                    return;
                }
                _jobs.EnqueueSceneWeaver(planId, context.AgentId, context.ConversationId, sceneId, providerId, modelId, branch, metadata);
                _logger.LogInformation("Queued SceneWeaver for backlog item {BacklogId} on plan {PlanId}.", item.BacklogId, planId);
                break;
            case FictionPhase.WorldBibleManager:
                _jobs.EnqueueWorldBibleManager(planId, context.AgentId, context.ConversationId, providerId, modelId, branch, metadata);
                _logger.LogInformation("Queued WorldBibleManager for backlog item {BacklogId} on plan {PlanId}.", item.BacklogId, planId);
                break;
            case FictionPhase.IterativePlanner:
                var iterationIndex = ResolveIterationIndex(item);
                _jobs.EnqueueIterativePlanner(planId, context.AgentId, context.ConversationId, iterationIndex, providerId, modelId, branch, metadata);
                _logger.LogInformation("Queued IterativePlanner for backlog item {BacklogId} on plan {PlanId} at iteration {Iteration}.", item.BacklogId, planId, iterationIndex);
                break;
            default:
                _logger.LogDebug("Backlog phase {Phase} for item {BacklogId} does not require automatic queueing.", phase, item.BacklogId);
                break;
        }
    }

    private static Guid? ResolveDependencyGuid(FictionPlanBacklogItem item, IReadOnlyList<FictionPlanBacklogItem> backlog, string key)
    {
        if (TryGetOutputGuid(item, key, out var value))
        {
            return value;
        }

        if (item.Inputs is null)
        {
            return null;
        }

        foreach (var input in item.Inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var dependency = backlog.FirstOrDefault(candidate =>
                candidate.Outputs is not null &&
                candidate.Outputs.Any(output => string.Equals(output, input, StringComparison.OrdinalIgnoreCase)));

            if (dependency is not null && TryGetOutputGuid(dependency, key, out var dependencyValue))
            {
                return dependencyValue;
            }
        }

        return null;
    }

    private static bool TryGetOutputGuid(FictionPlanBacklogItem item, string key, out Guid value)
    {
        value = Guid.Empty;
        if (!TryGetOutputMetadata(item, key, out var raw))
        {
            return false;
        }

        return Guid.TryParse(raw, out value);
    }

    private static bool TryGetOutputMetadata(FictionPlanBacklogItem item, string key, out string value)
    {
        value = string.Empty;
        if (item.Outputs is null)
        {
            return false;
        }

        var prefix = $"{key}=";
        foreach (var output in item.Outputs)
        {
            if (output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = output.Substring(prefix.Length);
                return true;
            }
        }

        return false;
    }

    private static void SetOutputMetadata(FictionPlanBacklogItem item, string key, string value)
    {
        var list = item.Outputs is null
            ? new List<string>()
            : new List<string>(item.Outputs);

        var prefix = $"{key}=";
        var index = list.FindIndex(output => output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            list[index] = prefix + value;
        }
        else
        {
            list.Add(prefix + value);
        }

        item.Outputs = list.ToArray();
        item.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void TryAddOutputMetadata(FictionPlanBacklogItem item, IDictionary<string, string> metadata, string key)
    {
        if (metadata.ContainsKey(key))
        {
            return;
        }

        if (TryGetOutputMetadata(item, key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }

    private void LogAutoResumeBacklogAction(
        FictionPlan plan,
        FictionPlanBacklogItem backlog,
        string branch,
        FictionPhaseExecutionContext context,
        TimeSpan age,
        Guid? conversationPlanId,
        Guid? providerId,
        Guid? modelId,
        Guid? agentId,
        Guid? taskId)
    {
        if (context.ConversationId == Guid.Empty)
        {
            return;
        }

        var planName = string.IsNullOrWhiteSpace(plan.Name) ? $"Plan {plan.Id:N}" : plan.Name;
        var payload = new JObject
        {
            ["planId"] = plan.Id,
            ["planName"] = planName,
            ["backlogId"] = backlog.BacklogId,
            ["description"] = backlog.Description,
            ["action"] = "auto-resume",
            ["branch"] = branch,
            ["status"] = backlog.Status.ToString(),
            ["source"] = "automation",
            ["conversationId"] = context.ConversationId,
            ["agentId"] = agentId ?? context.AgentId,
            ["age"] = age.TotalSeconds
        };

        if (conversationPlanId.HasValue)
        {
            payload["conversationPlanId"] = conversationPlanId.Value;
        }

        if (providerId.HasValue)
        {
            payload["providerId"] = providerId.Value;
        }

        if (modelId.HasValue)
        {
            payload["modelId"] = modelId.Value;
        }

        if (taskId.HasValue)
        {
            payload["taskId"] = taskId.Value;
        }

        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = context.ConversationId,
            Kind = "fiction.backlog.action",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        });
    }

    private void LogLoreAutomationRequest(
        FictionPlan plan,
        FictionLoreRequirement requirement,
        LoreBranchContext branchContext,
        FictionPhaseExecutionContext context,
        TimeSpan age)
    {
        var payload = new JObject
        {
            ["planId"] = plan.Id,
            ["planName"] = plan.Name,
            ["requirementId"] = requirement.Id,
            ["requirementSlug"] = requirement.RequirementSlug,
            ["title"] = requirement.Title,
            ["action"] = "auto-requested",
            ["branch"] = branchContext.Slug,
            ["status"] = requirement.Status.ToString(),
            ["source"] = "automation",
            ["age"] = age.TotalSeconds,
            ["requestedAtUtc"] = DateTime.UtcNow,
            ["slaMinutes"] = (int)Math.Round(_automationOptions.LoreAutoFulfillmentSla.TotalMinutes)
        };

        if (branchContext.Lineage is { Count: > 0 })
        {
            payload["branchLineage"] = new JArray(branchContext.Lineage);
        }

        if (context.ConversationId != Guid.Empty)
        {
            payload["conversationId"] = context.ConversationId;
        }

        if (context.AgentId != Guid.Empty)
        {
            payload["agentId"] = context.AgentId;
        }

        if (TryResolveProvider(context.Metadata, out var providerId))
        {
            payload["providerId"] = providerId;
        }

        var modelId = TryResolveModel(context.Metadata);
        if (modelId.HasValue)
        {
            payload["modelId"] = modelId.Value;
        }

        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = context.ConversationId,
            Kind = "fiction.lore.fulfillment",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        });
    }

    private static string ResolveBranchSlug(FictionPlan plan, FictionPhaseExecutionContext context)
    {
        string resolved;
        if (!string.IsNullOrWhiteSpace(context.BranchSlug))
        {
            resolved = context.BranchSlug;
        }
        else if (!string.IsNullOrWhiteSpace(plan.PrimaryBranchSlug))
        {
            resolved = plan.PrimaryBranchSlug;
        }
        else
        {
            resolved = DefaultBranch;
        }

        return NormalizeBranch(resolved);
    }

    private static string NormalizeBranch(string? branch)
        => string.IsNullOrWhiteSpace(branch)
            ? DefaultBranch
            : branch.Trim();

    private static string ResolveBacklogTitle(FictionPlanBacklogItem item)
        => string.IsNullOrWhiteSpace(item.Description) ? item.BacklogId : item.Description;

    private static int ResolveIterationIndex(FictionPlanBacklogItem item)
    {
        if (TryGetOutputMetadata(item, MetadataKeys.IterationIndex, out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return 1;
    }

    private static class MetadataKeys
    {
        public const string ChapterBlueprintId = "chapterBlueprintId";
        public const string ChapterScrollId = "chapterScrollId";
        public const string ChapterSectionId = "chapterSectionId";
        public const string ChapterSceneId = "chapterSceneId";
        public const string WorldBibleId = "worldBibleId";
        public const string IterationIndex = "iterationIndex";
    }

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $"backlog-{Guid.NewGuid():N}";
        }

        var normalized = new string(value
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        return normalized.Trim('-');
    }
}
