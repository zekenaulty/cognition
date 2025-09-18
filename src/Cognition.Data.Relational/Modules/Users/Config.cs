using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Users;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id).HasName("pk_users");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(128);
        b.Property(x => x.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(128);
        b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
        b.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
        b.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
        b.Property(x => x.PasswordHash).HasColumnName("password_hash");
        b.Property(x => x.PasswordSalt).HasColumnName("password_salt");
        b.Property(x => x.PasswordAlgo).HasColumnName("password_algo").HasMaxLength(64);
        b.Property(x => x.PasswordHashVersion).HasColumnName("password_hash_version");
        b.Property(x => x.PasswordUpdatedAtUtc).HasColumnName("password_updated_at_utc");
        b.Property(x => x.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(64);
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.PrimaryPersonaId).HasColumnName("primary_persona_id");
        b.HasOne(x => x.PrimaryPersona).WithMany().HasForeignKey(x => x.PrimaryPersonaId).HasConstraintName("fk_users_primary_persona");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.NormalizedUsername).IsUnique().HasDatabaseName("ux_users_normalized_username");
        b.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_users_normalized_email");
    }
}

public class UserPersonaConfiguration : IEntityTypeConfiguration<UserPersonas>
{
    public void Configure(EntityTypeBuilder<UserPersonas> b)
    {
        b.ToTable("user_personas");
        b.HasKey(x => x.Id).HasName("pk_user_personas");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.Property(x => x.IsDefault).HasColumnName("is_default");
        b.Property(x => x.Label).HasColumnName("label");
        b.Property(x => x.IsOwner).HasColumnName("is_owner").HasDefaultValue(false);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.User).WithMany(u => u.UserPersonas).HasForeignKey(x => x.UserId).HasConstraintName("fk_user_personas_users");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_user_personas_personas");
        b.HasIndex(x => new { x.UserId, x.PersonaId }).IsUnique().HasDatabaseName("ux_user_personas_user_persona");
        b.HasIndex(x => x.PersonaId).HasDatabaseName("IX_user_personas_persona_id");
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id).HasName("pk_refresh_tokens");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_refresh_tokens_users");
        b.Property(x => x.Token).HasColumnName("token").HasMaxLength(512).IsRequired();
        b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        b.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.UserId, x.Token }).IsUnique().HasDatabaseName("ux_refresh_tokens_user_token");
    }
}
