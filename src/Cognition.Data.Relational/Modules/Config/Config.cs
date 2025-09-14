using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Config;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> b)
    {
        b.ToTable("data_sources");
        b.HasKey(x => x.Id).HasName("pk_data_sources");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.DataSourceType).HasColumnName("data_source_type").HasConversion<string>();
        b.Property(x => x.CollectionName).HasColumnName("collection_name");
        b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
        b.HasIndex(x => new { x.Name, x.DataSourceType }).IsUnique().HasDatabaseName("ux_data_sources_name_type");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class SystemVariableConfiguration : IEntityTypeConfiguration<SystemVariable>
{
    public void Configure(EntityTypeBuilder<SystemVariable> b)
    {
        b.ToTable("system_variables");
        b.HasKey(x => x.Id).HasName("pk_system_variables");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.Property(x => x.Type).HasColumnName("type");
        b.Property(x => x.Value).HasColumnName("value").HasColumnType("jsonb");
        b.HasIndex(x => x.Key).IsUnique().HasDatabaseName("ux_system_variables_key");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
