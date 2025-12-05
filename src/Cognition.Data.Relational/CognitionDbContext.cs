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
using Cognition.Data.Relational.Modules.Planning;

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
    public DbSet<LlmGlobalDefault> LlmGlobalDefaults => Set<LlmGlobalDefault>();

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
    public DbSet<FictionPlan> FictionPlans => Set<FictionPlan>();
    public DbSet<FictionPlanPass> FictionPlanPasses => Set<FictionPlanPass>();
    public DbSet<FictionPlanCheckpoint> FictionPlanCheckpoints => Set<FictionPlanCheckpoint>();
    public DbSet<FictionChapterBlueprint> FictionChapterBlueprints => Set<FictionChapterBlueprint>();
    public DbSet<FictionChapterScroll> FictionChapterScrolls => Set<FictionChapterScroll>();
    public DbSet<FictionChapterSection> FictionChapterSections => Set<FictionChapterSection>();
    public DbSet<FictionChapterScene> FictionChapterScenes => Set<FictionChapterScene>();
    public DbSet<FictionPlanBacklogItem> FictionPlanBacklogItems => Set<FictionPlanBacklogItem>();
    public DbSet<FictionPlanTranscript> FictionPlanTranscripts => Set<FictionPlanTranscript>();
    public DbSet<FictionStoryMetric> FictionStoryMetrics => Set<FictionStoryMetric>();
    public DbSet<FictionWorldBible> FictionWorldBibles => Set<FictionWorldBible>();
    public DbSet<FictionWorldBibleEntry> FictionWorldBibleEntries => Set<FictionWorldBibleEntry>();
    public DbSet<FictionCharacter> FictionCharacters => Set<FictionCharacter>();
    public DbSet<FictionLoreRequirement> FictionLoreRequirements => Set<FictionLoreRequirement>();
    public DbSet<FictionPersonaObligation> FictionPersonaObligations => Set<FictionPersonaObligation>();
    public DbSet<PlannerExecution> PlannerExecutions => Set<PlannerExecution>();

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

    }

}
