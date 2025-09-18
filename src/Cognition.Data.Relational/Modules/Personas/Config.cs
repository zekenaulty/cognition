using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> b)
    {
        b.ToTable("personas");
        b.HasKey(x => x.Id).HasName("pk_personas");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Nickname).HasColumnName("nickname").HasMaxLength(128);
        b.Property(x => x.Role).HasColumnName("role").HasMaxLength(256);
        b.Property(x => x.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
        b.Property(x => x.Type).HasColumnName("persona_type").HasConversion<string>();
        b.Property(x => x.OwnedBy).HasColumnName("owned_by").HasConversion<string>();
        b.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(64);
        b.Property(x => x.Essence).HasColumnName("essence");
        b.Property(x => x.Beliefs).HasColumnName("beliefs");
        b.Property(x => x.Background).HasColumnName("background");
        b.Property(x => x.CommunicationStyle).HasColumnName("communication_style");
        b.Property(x => x.EmotionalDrivers).HasColumnName("emotional_drivers");
        b.Property(x => x.Voice).HasColumnName("voice").HasMaxLength(256);

        b.Property(x => x.SignatureTraits).HasColumnName("signature_traits");
        b.Property(x => x.NarrativeThemes).HasColumnName("narrative_themes");
        b.Property(x => x.DomainExpertise).HasColumnName("domain_expertise");
        b.Property(x => x.KnownPersonas).HasColumnName("known_personas");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        // Removed owner_user_id index; ownership is tracked via UserPersona links
    }
}

public class PersonaLinkConfiguration : IEntityTypeConfiguration<PersonaLink>
{
    public void Configure(EntityTypeBuilder<PersonaLink> b)
    {
        b.ToTable("persona_links");
        b.HasKey(x => x.Id).HasName("pk_persona_links");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FromPersonaId).HasColumnName("from_persona_id");
        b.Property(x => x.ToPersonaId).HasColumnName("to_persona_id");
        b.HasOne(x => x.FromPersona).WithMany(p => p.OutboundLinks).HasForeignKey(x => x.FromPersonaId).HasConstraintName("fk_persona_links_from");
        b.HasOne(x => x.ToPersona).WithMany(p => p.InboundLinks).HasForeignKey(x => x.ToPersonaId).HasConstraintName("fk_persona_links_to");
        b.Property(x => x.RelationshipType).HasColumnName("relationship_type");
        b.Property(x => x.Weight).HasColumnName("weight");
        b.Property(x => x.Description).HasColumnName("description");
        b.HasIndex(x => new { x.FromPersonaId, x.ToPersonaId }).HasDatabaseName("ix_persona_links_pair");
    }
}

public class PersonaPersonasConfiguration : IEntityTypeConfiguration<PersonaPersonas>
{
    public void Configure(EntityTypeBuilder<PersonaPersonas> b)
    {
        b.ToTable("persona_personas");
        b.HasKey(x => x.Id).HasName("pk_persona_personas");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FromPersonaId).HasColumnName("from_persona_id");
        b.Property(x => x.ToPersonaId).HasColumnName("to_persona_id");
        b.Property(x => x.IsOwner).HasColumnName("is_owner").HasDefaultValue(false);
        b.Property(x => x.Label).HasColumnName("label");
        b.HasOne(x => x.FromPersona).WithMany().HasForeignKey(x => x.FromPersonaId).HasConstraintName("fk_persona_personas_from");
        b.HasOne(x => x.ToPersona).WithMany().HasForeignKey(x => x.ToPersonaId).HasConstraintName("fk_persona_personas_to");
        b.HasIndex(x => new { x.FromPersonaId, x.ToPersonaId }).IsUnique().HasDatabaseName("ux_persona_personas_pair");
    }
}

public class PersonaMemoryTypeConfiguration : IEntityTypeConfiguration<PersonaMemoryType>
{
    public void Configure(EntityTypeBuilder<PersonaMemoryType> b)
    {
        b.ToTable("persona_memory_types");
        b.HasKey(x => x.Id).HasName("pk_persona_memory_types");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(128);
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_persona_memory_types_code");
    }
}

public class PersonaMemoryConfiguration : IEntityTypeConfiguration<PersonaMemory>
{
    public void Configure(EntityTypeBuilder<PersonaMemory> b)
    {
        b.ToTable("persona_memories");
        b.HasKey(x => x.Id).HasName("pk_persona_memories");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.Property(x => x.TypeId).HasColumnName("type_id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.Importance).HasColumnName("importance");
        b.Property(x => x.Emotions).HasColumnName("emotions");
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Source).HasColumnName("source");
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.Property(x => x.RecordedAtUtc).HasColumnName("recorded_at_utc");
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_persona_memories_persona");
        b.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId).HasConstraintName("fk_persona_memories_type");
        b.HasIndex(x => new { x.PersonaId, x.OccurredAtUtc }).HasDatabaseName("ix_persona_memories_timeline");
    }
}

public class PersonaEventTypeConfiguration : IEntityTypeConfiguration<PersonaEventType>
{
    public void Configure(EntityTypeBuilder<PersonaEventType> b)
    {
        b.ToTable("persona_event_types");
        b.HasKey(x => x.Id).HasName("pk_persona_event_types");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(128);
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_persona_event_types_code");
    }
}

public class PersonaEventConfiguration : IEntityTypeConfiguration<PersonaEvent>
{
    public void Configure(EntityTypeBuilder<PersonaEvent> b)
    {
        b.ToTable("persona_events");
        b.HasKey(x => x.Id).HasName("pk_persona_events");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.Property(x => x.TypeId).HasColumnName("type_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(256);
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Categories).HasColumnName("categories");
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Location).HasColumnName("location");
        b.Property(x => x.Importance).HasColumnName("importance");
        b.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        b.Property(x => x.EndedAtUtc).HasColumnName("ended_at_utc");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_persona_events_persona");
        b.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId).HasConstraintName("fk_persona_events_type");
        b.HasIndex(x => new { x.PersonaId, x.StartedAtUtc }).HasDatabaseName("ix_persona_events_timeline");
    }
}

public class PersonaDreamConfiguration : IEntityTypeConfiguration<PersonaDream>
{
    public void Configure(EntityTypeBuilder<PersonaDream> b)
    {
        b.ToTable("persona_dreams");
        b.HasKey(x => x.Id).HasName("pk_persona_dreams");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Valence).HasColumnName("valence");
        b.Property(x => x.Vividness).HasColumnName("vividness");
        b.Property(x => x.Lucid).HasColumnName("lucid");
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_persona_dreams_persona");
        b.HasIndex(x => new { x.PersonaId, x.OccurredAtUtc }).HasDatabaseName("ix_persona_dreams_timeline");
    }
}
