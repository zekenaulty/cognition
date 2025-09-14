using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.LLM;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> b)
    {
        b.ToTable("providers");
        b.HasKey(x => x.Id).HasName("pk_providers");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_providers_name");
        b.Property(x => x.DisplayName).HasColumnName("display_name");
        b.Property(x => x.BaseUrl).HasColumnName("base_url");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> b)
    {
        b.ToTable("models");
        b.HasKey(x => x.Id).HasName("pk_models");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProviderId).HasColumnName("provider_id");
        b.HasOne(x => x.Provider).WithMany(p => p.Models).HasForeignKey(x => x.ProviderId).HasConstraintName("fk_models_providers");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.HasIndex(x => new { x.ProviderId, x.Name }).IsUnique().HasDatabaseName("ux_models_provider_name");
        b.Property(x => x.DisplayName).HasColumnName("display_name");
        b.Property(x => x.ContextWindow).HasColumnName("context_window");
        b.Property(x => x.SupportsVision).HasColumnName("supports_vision");
        b.Property(x => x.SupportsStreaming).HasColumnName("supports_streaming");
        b.Property(x => x.InputCostPer1M).HasColumnName("input_cost_per_1m");
        b.Property(x => x.CachedInputCostPer1M).HasColumnName("cached_input_cost_per_1m");
        b.Property(x => x.OutputCostPer1M).HasColumnName("output_cost_per_1m");
        b.Property(x => x.IsDeprecated).HasColumnName("is_deprecated");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ClientProfileConfiguration : IEntityTypeConfiguration<ClientProfile>
{
    public void Configure(EntityTypeBuilder<ClientProfile> b)
    {
        b.ToTable("client_profiles");
        b.HasKey(x => x.Id).HasName("pk_client_profiles");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Name).HasDatabaseName("ix_client_profiles_name");
        b.Property(x => x.ProviderId).HasColumnName("provider_id");
        b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).HasConstraintName("fk_client_profiles_providers");
        b.Property(x => x.ModelId).HasColumnName("model_id");
        b.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).HasConstraintName("fk_client_profiles_models");
        b.Property(x => x.ApiCredentialId).HasColumnName("api_credential_id");
        b.HasOne(x => x.ApiCredential).WithMany().HasForeignKey(x => x.ApiCredentialId).HasConstraintName("fk_client_profiles_api_credentials");
        b.Property(x => x.UserName).HasColumnName("user_name");
        b.Property(x => x.BaseUrlOverride).HasColumnName("base_url_override");
        b.Property(x => x.MaxTokens).HasColumnName("max_tokens");
        b.Property(x => x.Temperature).HasColumnName("temperature");
        b.Property(x => x.TopP).HasColumnName("top_p");
        b.Property(x => x.PresencePenalty).HasColumnName("presence_penalty");
        b.Property(x => x.FrequencyPenalty).HasColumnName("frequency_penalty");
        b.Property(x => x.Stream).HasColumnName("stream");
        b.Property(x => x.LoggingEnabled).HasColumnName("logging_enabled");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ApiCredentialConfiguration : IEntityTypeConfiguration<ApiCredential>
{
    public void Configure(EntityTypeBuilder<ApiCredential> b)
    {
        b.ToTable("api_credentials");
        b.HasKey(x => x.Id).HasName("pk_api_credentials");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProviderId).HasColumnName("provider_id");
        b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).HasConstraintName("fk_api_credentials_providers");
        b.Property(x => x.KeyRef).HasColumnName("key_ref").HasMaxLength(256).IsRequired();
        b.HasIndex(x => new { x.ProviderId, x.KeyRef }).IsUnique().HasDatabaseName("ux_api_credentials_provider_keyref");
        b.Property(x => x.LastUsedAtUtc).HasColumnName("last_used_at_utc");
        b.Property(x => x.IsValid).HasColumnName("is_valid").HasDefaultValue(true);
        b.Property(x => x.Notes).HasColumnName("notes");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
