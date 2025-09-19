using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational.Modules.FeatureFlags;
using Cognition.Data.Relational.Modules.LLM;
using Cognition.Data.Relational.Modules.Instructions;
using Cognition.Data.Relational.Modules.Prompts;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Tools;
using Cognition.Data.Relational.Modules.Knowledge;
using Cognition.Data.Relational.Modules.Questions;
using Cognition.Data.Relational.Modules.Config;
using Cognition.Data.Relational.Modules.Users;
using Cognition.Data.Relational.Modules.Images;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Data.Relational;

public class CognitionDbContext : DbContext
{
    public CognitionDbContext(DbContextOptions<CognitionDbContext> options)
        : base(options)
    {
    }

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Model> Models => Set<Model>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<ApiCredential> ApiCredentials => Set<ApiCredential>();

    public DbSet<Instruction> Instructions => Set<Instruction>();
    public DbSet<InstructionSet> InstructionSets => Set<InstructionSet>();
    public DbSet<InstructionSetItem> InstructionSetItems => Set<InstructionSetItem>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<PersonaLink> PersonaLinks => Set<PersonaLink>();
    public DbSet<PersonaPersonas> PersonaPersonas => Set<PersonaPersonas>();
    public DbSet<PersonaMemoryType> PersonaMemoryTypes => Set<PersonaMemoryType>();
    public DbSet<PersonaMemory> PersonaMemories => Set<PersonaMemory>();
    public DbSet<PersonaEventType> PersonaEventTypes => Set<PersonaEventType>();
    public DbSet<PersonaEvent> PersonaEvents => Set<PersonaEvent>();
    public DbSet<PersonaDream> PersonaDreams => Set<PersonaDream>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentToolBinding> AgentToolBindings => Set<AgentToolBinding>();

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<ConversationMessageVersion> ConversationMessageVersions => Set<ConversationMessageVersion>();
    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();

    public DbSet<ConversationPlan> ConversationPlans => Set<ConversationPlan>();
    public DbSet<ConversationTask> ConversationTasks => Set<ConversationTask>();
    public DbSet<ConversationThought> ConversationThoughts => Set<ConversationThought>();

    public DbSet<ConversationWorkflowState> ConversationWorkflowStates => Set<ConversationWorkflowState>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();

    public DbSet<Tool> Tools => Set<Tool>();
    public DbSet<ToolParameter> ToolParameters => Set<ToolParameter>();
    public DbSet<ToolProviderSupport> ToolProviderSupports => Set<ToolProviderSupport>();
    public DbSet<ToolExecutionLog> ToolExecutionLogs => Set<ToolExecutionLog>();

    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<KnowledgeRelation> KnowledgeRelations => Set<KnowledgeRelation>();
    public DbSet<KnowledgeEmbedding> KnowledgeEmbeddings => Set<KnowledgeEmbedding>();

    public DbSet<QuestionCategory> QuestionCategories => Set<QuestionCategory>();
    public DbSet<Question> Questions => Set<Question>();

    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<SystemVariable> SystemVariables => Set<SystemVariable>();

    public DbSet<User> Users => Set<User>();
    public DbSet<UserPersonas> UserPersonas => Set<UserPersonas>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<ImageStyle> ImageStyles => Set<ImageStyle>();

    // Fiction module
    public DbSet<FictionProject> FictionProjects => Set<FictionProject>();
    public DbSet<StyleGuide> StyleGuides => Set<StyleGuide>();
    public DbSet<GlossaryTerm> GlossaryTerms => Set<GlossaryTerm>();
    public DbSet<WorldAsset> WorldAssets => Set<WorldAsset>();
    public DbSet<WorldAssetVersion> WorldAssetVersions => Set<WorldAssetVersion>();
    public DbSet<CanonRule> CanonRules => Set<CanonRule>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<PlotArc> PlotArcs => Set<PlotArc>();
    public DbSet<OutlineNode> OutlineNodes => Set<OutlineNode>();
    public DbSet<OutlineNodeVersion> OutlineNodeVersions => Set<OutlineNodeVersion>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<TimelineEventAsset> TimelineEventAssets => Set<TimelineEventAsset>();
    public DbSet<DraftSegment> DraftSegments => Set<DraftSegment>();
    public DbSet<DraftSegmentVersion> DraftSegmentVersions => Set<DraftSegmentVersion>();
    public DbSet<Annotation> Annotations => Set<Annotation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure WorkflowEvent.Payload as JSONB
        modelBuilder.Entity<WorkflowEvent>()
            .Property(e => e.Payload)
            .HasConversion(
                v => v.ToString(Newtonsoft.Json.Formatting.None),
                v => Newtonsoft.Json.Linq.JObject.Parse(v))
            .HasColumnType("jsonb");

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CognitionDbContext).Assembly);

        // Configure ConversationWorkflowState.Blackboard as JSONB
        modelBuilder.Entity<ConversationWorkflowState>()
            .Property(e => e.Blackboard)
            .HasConversion(
                v => v.ToString(Newtonsoft.Json.Formatting.None),
                v => Newtonsoft.Json.Linq.JObject.Parse(v))
            .HasColumnType("jsonb");
    }

    public override int SaveChanges()
    {
        ValidateBeforeSaveAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesInternalAsync(cancellationToken);
    }

    private async Task<int> SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        await ValidateBeforeSaveAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateBeforeSaveAsync(CancellationToken cancellationToken)
    {
        // Enforce KnowledgeEmbedding invariants at application level
        var embeddingEntries = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Knowledge.KnowledgeEmbedding>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        foreach (var e in embeddingEntries)
        {
            var vector = e.Vector ?? Array.Empty<float>();
            if (e.Dimensions.HasValue)
            {
                if (vector.Length != e.Dimensions.Value)
                {
                    throw new InvalidOperationException($"KnowledgeEmbedding vector length ({vector.Length}) does not match Dimensions ({e.Dimensions}).");
                }
            }

            if (e.Normalized == true)
            {
                // If VectorL2Norm provided, validate ~1.0; otherwise compute for validation
                double l2 = e.VectorL2Norm ?? Math.Sqrt(vector.Sum(v => v * (double)v));
                if (Math.Abs(l2 - 1.0) > 1e-3)
                {
                    throw new InvalidOperationException($"KnowledgeEmbedding marked Normalized but L2 norm ≈ {l2:0.########} (expected ~1.0).");
                }
                if (e.VectorL2Norm == null)
                {
                    e.VectorL2Norm = l2;
                }
            }

            // Optional uniqueness check when identifiers are present
            if (e.Model != null && e.ModelVersion != null && e.ChunkIndex.HasValue)
            {
                var exists = await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeEmbedding>()
                    .AsNoTracking()
                    .AnyAsync(x => x.KnowledgeItemId == e.KnowledgeItemId
                                   && x.Model == e.Model
                                   && x.ModelVersion == e.ModelVersion
                                   && x.ChunkIndex == e.ChunkIndex
                                   && x.Id != e.Id, cancellationToken);
                if (exists)
                {
                    throw new InvalidOperationException("Duplicate KnowledgeEmbedding for (KnowledgeItemId, Model, ModelVersion, ChunkIndex).");
                }
            }
        }

        // Project Fiction entities to Knowledge on save (append/update KnowledgeItems)
        await ProjectFictionToKnowledgeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProjectFictionToKnowledgeAsync(CancellationToken cancellationToken)
    {
        // GlossaryTerm -> KnowledgeItem (KeywordDefinition)
        var newTerms = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Fiction.GlossaryTerm>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();
        foreach (var entry in newTerms)
        {
            var term = entry.Entity;
            var ki = term.KnowledgeItemId.HasValue
                ? await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().FirstOrDefaultAsync(x => x.Id == term.KnowledgeItemId.Value, cancellationToken)
                : null;
            if (ki is null)
            {
                ki = new Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem
                {
                    ContentType = Cognition.Data.Relational.Modules.Knowledge.KnowledgeContentType.KeywordDefinition,
                    CreatedAtUtc = DateTime.UtcNow
                };
                Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().Add(ki);
                term.KnowledgeItemId = ki.Id;
            }
            ki.Content = term.Definition;
            ki.Categories = new[] { "fiction", "glossary" };
            ki.Keywords = new[] { term.Term };
            ki.Source = $"project:{term.FictionProjectId}";
            ki.Timestamp = DateTime.UtcNow;
            ki.Properties = new Dictionary<string, object?>
            {
                ["term"] = term.Term,
                ["aliases"] = term.Aliases,
                ["domain"] = term.Domain,
                ["projectId"] = term.FictionProjectId
            };
        }

        // CanonRule -> KnowledgeItem (Fact)
        var canonEntries = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Fiction.CanonRule>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();
        foreach (var entry in canonEntries)
        {
            var rule = entry.Entity;
            var ki = rule.KnowledgeItemId.HasValue
                ? await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().FirstOrDefaultAsync(x => x.Id == rule.KnowledgeItemId.Value, cancellationToken)
                : null;
            if (ki is null)
            {
                ki = new Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem
                {
                    ContentType = Cognition.Data.Relational.Modules.Knowledge.KnowledgeContentType.Fact,
                    CreatedAtUtc = DateTime.UtcNow
                };
                Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().Add(ki);
                rule.KnowledgeItemId = ki.Id;
            }
            ki.Content = Newtonsoft.Json.JsonConvert.SerializeObject(rule.Value ?? new Dictionary<string, object?>());
            ki.Categories = new[] { "fiction", "canon", rule.Scope.ToString() };
            ki.Keywords = new[] { rule.Key };
            ki.Source = $"project:{rule.FictionProjectId}";
            ki.Timestamp = DateTime.UtcNow;
            ki.Properties = new Dictionary<string, object?>
            {
                ["scope"] = rule.Scope.ToString(),
                ["key"] = rule.Key,
                ["evidence"] = rule.Evidence,
                ["confidence"] = rule.Confidence,
                ["plotArcId"] = rule.PlotArcId,
                ["projectId"] = rule.FictionProjectId
            };
        }

        // WorldAssetVersion -> KnowledgeItem (Concept)
        var wavEntries = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Fiction.WorldAssetVersion>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();
        foreach (var entry in wavEntries)
        {
            var wav = entry.Entity;
            await Entry(wav).Reference(x => x.WorldAsset).LoadAsync(cancellationToken);
            var asset = wav.WorldAsset;
            var ki = wav.KnowledgeItemId.HasValue
                ? await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().FirstOrDefaultAsync(x => x.Id == wav.KnowledgeItemId.Value, cancellationToken)
                : null;
            if (ki is null)
            {
                ki = new Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem
                {
                    ContentType = Cognition.Data.Relational.Modules.Knowledge.KnowledgeContentType.Concept,
                    CreatedAtUtc = DateTime.UtcNow
                };
                Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().Add(ki);
                wav.KnowledgeItemId = ki.Id;
            }
            var content = wav.Content ?? new Dictionary<string, object?>();
            ki.Content = Newtonsoft.Json.JsonConvert.SerializeObject(content);
            ki.Categories = new[] { "fiction", "world", asset.Type.ToString() };
            ki.Keywords = new[] { asset.Name };
            ki.Source = $"project:{asset.FictionProjectId}";
            ki.Timestamp = DateTime.UtcNow;
            ki.Properties = new Dictionary<string, object?>
            {
                ["assetId"] = asset.Id,
                ["assetType"] = asset.Type.ToString(),
                ["assetName"] = asset.Name,
                ["versionIndex"] = wav.VersionIndex,
                ["projectId"] = asset.FictionProjectId
            };
        }

        // OutlineNodeVersion -> KnowledgeItem (Summary)
        var onvEntries = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Fiction.OutlineNodeVersion>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();
        foreach (var entry in onvEntries)
        {
            var onv = entry.Entity;
            await Entry(onv).Reference(x => x.OutlineNode).LoadAsync(cancellationToken);
            var node = onv.OutlineNode;
            var ki = onv.KnowledgeItemId.HasValue
                ? await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().FirstOrDefaultAsync(x => x.Id == onv.KnowledgeItemId.Value, cancellationToken)
                : null;
            if (ki is null)
            {
                ki = new Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem
                {
                    ContentType = Cognition.Data.Relational.Modules.Knowledge.KnowledgeContentType.Summary,
                    CreatedAtUtc = DateTime.UtcNow
                };
                Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().Add(ki);
                onv.KnowledgeItemId = ki.Id;
            }
            var beats = onv.Beats ?? new Dictionary<string, object?>();
            ki.Content = Newtonsoft.Json.JsonConvert.SerializeObject(beats);
            ki.Categories = new[] { "fiction", "outline", node.Type.ToString() };
            ki.Keywords = new[] { node.Title };
            ki.Source = $"project:{node.FictionProjectId}";
            ki.Timestamp = DateTime.UtcNow;
            ki.Properties = new Dictionary<string, object?>
            {
                ["outlineNodeId"] = node.Id,
                ["type"] = node.Type.ToString(),
                ["title"] = node.Title,
                ["sequenceIndex"] = node.SequenceIndex,
                ["versionIndex"] = onv.VersionIndex,
                ["projectId"] = node.FictionProjectId
            };
        }

        // DraftSegmentVersion -> KnowledgeItem (Other)
        var dsvEntries = ChangeTracker.Entries<Cognition.Data.Relational.Modules.Fiction.DraftSegmentVersion>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();
        foreach (var entry in dsvEntries)
        {
            var dsv = entry.Entity;
            await Entry(dsv).Reference(x => x.DraftSegment).LoadAsync(cancellationToken);
            var seg = dsv.DraftSegment;
            var ki = dsv.KnowledgeItemId.HasValue
                ? await Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().FirstOrDefaultAsync(x => x.Id == dsv.KnowledgeItemId.Value, cancellationToken)
                : null;
            if (ki is null)
            {
                ki = new Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem
                {
                    ContentType = Cognition.Data.Relational.Modules.Knowledge.KnowledgeContentType.Other,
                    CreatedAtUtc = DateTime.UtcNow
                };
                Set<Cognition.Data.Relational.Modules.Knowledge.KnowledgeItem>().Add(ki);
                dsv.KnowledgeItemId = ki.Id;
            }
            ki.Content = seg.Title + "\n\n" + (dsv.BodyMarkdown ?? string.Empty);
            ki.Categories = new[] { "fiction", "draft", "scene" };
            ki.Keywords = new[] { seg.Title };
            ki.Source = $"project:{seg.FictionProjectId}";
            ki.Timestamp = DateTime.UtcNow;
            ki.Properties = new Dictionary<string, object?>
            {
                ["draftSegmentId"] = seg.Id,
                ["outlineNodeId"] = seg.OutlineNodeId,
                ["versionIndex"] = dsv.VersionIndex,
                ["projectId"] = seg.FictionProjectId
            };
        }
    }
}
