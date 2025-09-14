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
    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();

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
    public DbSet<UserPersona> UserPersonas => Set<UserPersona>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ImageAsset> ImageAssets => Set<ImageAsset>();
    public DbSet<ImageStyle> ImageStyles => Set<ImageStyle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CognitionDbContext).Assembly);
    }
}
