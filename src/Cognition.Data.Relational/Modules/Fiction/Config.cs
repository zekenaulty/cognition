using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionProjectConfiguration : IEntityTypeConfiguration<FictionProject>
{
    public void Configure(EntityTypeBuilder<FictionProject> b)
    {
        b.ToTable("fiction_projects");
        b.HasKey(x => x.Id).HasName("pk_fiction_projects");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Logline).HasColumnName("logline");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class FictionPlanConfiguration : IEntityTypeConfiguration<FictionPlan>
{
    public void Configure(EntityTypeBuilder<FictionPlan> b)
    {
        b.ToTable("fiction_plans");
        b.HasKey(x => x.Id).HasName("pk_fiction_plans");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Name).HasColumnName("name");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.PrimaryBranchSlug).HasColumnName("primary_branch_slug");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject)
            .WithMany(p => p.FictionPlans)
            .HasForeignKey(x => x.FictionProjectId)
            .HasConstraintName("fk_fiction_plans_project");
        b.HasIndex(x => new { x.FictionProjectId, x.Name }).HasDatabaseName("ix_fiction_plans_project_name");
        b.HasMany(x => x.Backlog)
            .WithOne(x => x.FictionPlan)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_plan_backlog_plan");
    }
}

public class FictionPlanPassConfiguration : IEntityTypeConfiguration<FictionPlanPass>
{
    public void Configure(EntityTypeBuilder<FictionPlanPass> b)
    {
        b.ToTable("fiction_plan_passes");
        b.HasKey(x => x.Id).HasName("pk_fiction_plan_passes");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.PassIndex).HasColumnName("pass_index");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Summary).HasColumnName("summary");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.Passes)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_plan_passes_plan");
        b.HasIndex(x => new { x.FictionPlanId, x.PassIndex }).IsUnique().HasDatabaseName("ux_fiction_plan_passes_plan_index");
    }
}

public class FictionPlanBacklogItemConfiguration : IEntityTypeConfiguration<FictionPlanBacklogItem>
{
    public void Configure(EntityTypeBuilder<FictionPlanBacklogItem> b)
    {
        b.ToTable("fiction_plan_backlog");
        b.HasKey(x => x.Id).HasName("pk_fiction_plan_backlog");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.BacklogId).HasColumnName("backlog_id").HasMaxLength(128);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(512);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        b.Property(x => x.Inputs).HasColumnName("inputs").HasColumnType("jsonb");
        b.Property(x => x.Outputs).HasColumnName("outputs").HasColumnType("jsonb");
        b.Property(x => x.InProgressAtUtc).HasColumnName("in_progress_at_utc");
        b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => new { x.FictionPlanId, x.BacklogId }).IsUnique().HasDatabaseName("ux_fiction_plan_backlog_plan_backlog_id");
    }
}

public class FictionPlanCheckpointConfiguration : IEntityTypeConfiguration<FictionPlanCheckpoint>
{
    public void Configure(EntityTypeBuilder<FictionPlanCheckpoint> b)
    {
        b.ToTable("fiction_plan_checkpoints");
        b.HasKey(x => x.Id).HasName("pk_fiction_plan_checkpoints");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.Phase).HasColumnName("phase");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        b.Property(x => x.CompletedCount).HasColumnName("completed_count");
        b.Property(x => x.TargetCount).HasColumnName("target_count");
        b.Property(x => x.Progress).HasColumnName("progress").HasColumnType("jsonb");
        b.Property(x => x.LockedByAgentId).HasColumnName("locked_by_agent_id");
        b.Property(x => x.LockedByConversationId).HasColumnName("locked_by_conversation_id");
        b.Property(x => x.LockedAtUtc).HasColumnName("locked_at_utc");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.Checkpoints)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_plan_checkpoints_plan");
        b.HasIndex(x => new { x.FictionPlanId, x.Phase }).IsUnique().HasDatabaseName("ux_fiction_plan_checkpoints_plan_phase");
    }
}

public class FictionChapterBlueprintConfiguration : IEntityTypeConfiguration<FictionChapterBlueprint>
{
    public void Configure(EntityTypeBuilder<FictionChapterBlueprint> b)
    {
        b.ToTable("fiction_chapter_blueprints");
        b.HasKey(x => x.Id).HasName("pk_fiction_chapter_blueprints");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.SourcePlanPassId).HasColumnName("source_plan_pass_id");
        b.Property(x => x.ChapterIndex).HasColumnName("chapter_index");
        b.Property(x => x.ChapterSlug).HasColumnName("chapter_slug");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Synopsis).HasColumnName("synopsis");
        b.Property(x => x.Structure).HasColumnName("structure").HasColumnType("jsonb");
        b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.IsLocked).HasColumnName("is_locked");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.ChapterBlueprints)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_chapter_blueprints_plan");
        b.HasOne(x => x.SourcePlanPass)
            .WithMany()
            .HasForeignKey(x => x.SourcePlanPassId)
            .HasConstraintName("fk_fiction_chapter_blueprints_pass");
        b.HasIndex(x => new { x.FictionPlanId, x.ChapterIndex }).IsUnique().HasDatabaseName("ux_fiction_chapter_blueprints_plan_index");
        b.HasIndex(x => new { x.FictionPlanId, x.ChapterSlug }).IsUnique().HasDatabaseName("ux_fiction_chapter_blueprints_plan_slug");
    }
}

public class FictionChapterScrollConfiguration : IEntityTypeConfiguration<FictionChapterScroll>
{
    public void Configure(EntityTypeBuilder<FictionChapterScroll> b)
    {
        b.ToTable("fiction_chapter_scrolls");
        b.HasKey(x => x.Id).HasName("pk_fiction_chapter_scrolls");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionChapterBlueprintId).HasColumnName("fiction_chapter_blueprint_id");
        b.Property(x => x.VersionIndex).HasColumnName("version_index");
        b.Property(x => x.ScrollSlug).HasColumnName("scroll_slug");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Synopsis).HasColumnName("synopsis");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.DerivedFromScrollId).HasColumnName("derived_from_scroll_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionChapterBlueprint)
            .WithMany(c => c.Scrolls)
            .HasForeignKey(x => x.FictionChapterBlueprintId)
            .HasConstraintName("fk_fiction_chapter_scrolls_blueprint");
        b.HasOne(x => x.DerivedFromScroll)
            .WithMany()
            .HasForeignKey(x => x.DerivedFromScrollId)
            .HasConstraintName("fk_fiction_chapter_scrolls_parent");
        b.HasIndex(x => new { x.FictionChapterBlueprintId, x.VersionIndex }).IsUnique().HasDatabaseName("ux_fiction_chapter_scrolls_blueprint_index");
    }
}

public class FictionChapterSectionConfiguration : IEntityTypeConfiguration<FictionChapterSection>
{
    public void Configure(EntityTypeBuilder<FictionChapterSection> b)
    {
        b.ToTable("fiction_chapter_sections");
        b.HasKey(x => x.Id).HasName("pk_fiction_chapter_sections");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionChapterScrollId).HasColumnName("fiction_chapter_scroll_id");
        b.Property(x => x.ParentSectionId).HasColumnName("parent_section_id");
        b.Property(x => x.SectionIndex).HasColumnName("section_index");
        b.Property(x => x.SectionSlug).HasColumnName("section_slug");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionChapterScroll)
            .WithMany(s => s.Sections)
            .HasForeignKey(x => x.FictionChapterScrollId)
            .HasConstraintName("fk_fiction_chapter_sections_scroll");
        b.HasOne(x => x.ParentSection)
            .WithMany(x => x.ChildSections)
            .HasForeignKey(x => x.ParentSectionId)
            .HasConstraintName("fk_fiction_chapter_sections_parent");
        b.HasIndex(x => new { x.FictionChapterScrollId, x.SectionIndex }).IsUnique().HasDatabaseName("ux_fiction_chapter_sections_scroll_index");
    }
}

public class FictionChapterSceneConfiguration : IEntityTypeConfiguration<FictionChapterScene>
{
    public void Configure(EntityTypeBuilder<FictionChapterScene> b)
    {
        b.ToTable("fiction_chapter_scenes");
        b.HasKey(x => x.Id).HasName("pk_fiction_chapter_scenes");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionChapterSectionId).HasColumnName("fiction_chapter_section_id");
        b.Property(x => x.SceneIndex).HasColumnName("scene_index");
        b.Property(x => x.SceneSlug).HasColumnName("scene_slug");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        b.Property(x => x.DerivedFromSceneId).HasColumnName("derived_from_scene_id");
        b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionChapterSection)
            .WithMany(s => s.Scenes)
            .HasForeignKey(x => x.FictionChapterSectionId)
            .HasConstraintName("fk_fiction_chapter_scenes_section");
        b.HasOne(x => x.DerivedFromScene)
            .WithMany()
            .HasForeignKey(x => x.DerivedFromSceneId)
            .HasConstraintName("fk_fiction_chapter_scenes_parent");
        b.HasIndex(x => new { x.FictionChapterSectionId, x.SceneIndex }).IsUnique().HasDatabaseName("ux_fiction_chapter_scenes_section_index");
    }
}

public class FictionPlanTranscriptConfiguration : IEntityTypeConfiguration<FictionPlanTranscript>
{
    public void Configure(EntityTypeBuilder<FictionPlanTranscript> b)
    {
        b.ToTable("fiction_plan_transcripts");
        b.HasKey(x => x.Id).HasName("pk_fiction_plan_transcripts");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.Phase).HasColumnName("phase");
        b.Property(x => x.FictionChapterBlueprintId).HasColumnName("fiction_chapter_blueprint_id");
        b.Property(x => x.FictionChapterSceneId).HasColumnName("fiction_chapter_scene_id");
        b.Property(x => x.AgentId).HasColumnName("agent_id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.ConversationMessageId).HasColumnName("conversation_message_id");
        b.Property(x => x.Attempt).HasColumnName("attempt");
        b.Property(x => x.RequestPayload).HasColumnName("request_payload");
        b.Property(x => x.ResponsePayload).HasColumnName("response_payload");
        b.Property(x => x.PromptTokens).HasColumnName("prompt_tokens");
        b.Property(x => x.CompletionTokens).HasColumnName("completion_tokens");
        b.Property(x => x.LatencyMs).HasColumnName("latency_ms");
        b.Property(x => x.ValidationStatus).HasColumnName("validation_status").HasConversion<string>();
        b.Property(x => x.ValidationDetails).HasColumnName("validation_details");
        b.Property(x => x.IsRetry).HasColumnName("is_retry");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.Transcripts)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_plan_transcripts_plan");
        b.HasOne(x => x.FictionChapterBlueprint)
            .WithMany()
            .HasForeignKey(x => x.FictionChapterBlueprintId)
            .HasConstraintName("fk_fiction_plan_transcripts_blueprint");
        b.HasOne(x => x.FictionChapterScene)
            .WithMany(s => s.Transcripts)
            .HasForeignKey(x => x.FictionChapterSceneId)
            .HasConstraintName("fk_fiction_plan_transcripts_scene");
        b.HasIndex(x => new { x.FictionPlanId, x.CreatedAtUtc }).HasDatabaseName("ix_fiction_plan_transcripts_plan_created_at");
    }
}

public class FictionStoryMetricConfiguration : IEntityTypeConfiguration<FictionStoryMetric>
{
    public void Configure(EntityTypeBuilder<FictionStoryMetric> b)
    {
        b.ToTable("fiction_story_metrics");
        b.HasKey(x => x.Id).HasName("pk_fiction_story_metrics");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.FictionChapterSceneId).HasColumnName("fiction_chapter_scene_id");
        b.Property(x => x.MetricKey).HasColumnName("metric_key");
        b.Property(x => x.NumericValue).HasColumnName("numeric_value");
        b.Property(x => x.TextValue).HasColumnName("text_value");
        b.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.StoryMetrics)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_story_metrics_plan");
        b.HasOne(x => x.FictionChapterScene)
            .WithMany(s => s.StoryMetrics)
            .HasForeignKey(x => x.FictionChapterSceneId)
            .HasConstraintName("fk_fiction_story_metrics_scene");
        b.HasIndex(x => new { x.FictionPlanId, x.MetricKey, x.CreatedAtUtc }).HasDatabaseName("ix_fiction_story_metrics_plan_key_created_at");
    }
}

public class FictionWorldBibleConfiguration : IEntityTypeConfiguration<FictionWorldBible>
{
    public void Configure(EntityTypeBuilder<FictionWorldBible> b)
    {
        b.ToTable("fiction_world_bibles");
        b.HasKey(x => x.Id).HasName("pk_fiction_world_bibles");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionPlanId).HasColumnName("fiction_plan_id");
        b.Property(x => x.Domain).HasColumnName("domain");
        b.Property(x => x.BranchSlug).HasColumnName("branch_slug");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionPlan)
            .WithMany(p => p.WorldBibles)
            .HasForeignKey(x => x.FictionPlanId)
            .HasConstraintName("fk_fiction_world_bibles_plan");
        b.HasIndex(x => new { x.FictionPlanId, x.Domain, x.BranchSlug }).IsUnique().HasDatabaseName("ux_fiction_world_bibles_domain_branch");
    }
}

public class FictionWorldBibleEntryConfiguration : IEntityTypeConfiguration<FictionWorldBibleEntry>
{
    public void Configure(EntityTypeBuilder<FictionWorldBibleEntry> b)
    {
        b.ToTable("fiction_world_bible_entries");
        b.HasKey(x => x.Id).HasName("pk_fiction_world_bible_entries");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionWorldBibleId).HasColumnName("fiction_world_bible_id");
        b.Property(x => x.EntrySlug).HasColumnName("entry_slug");
        b.Property(x => x.EntryName).HasColumnName("entry_name");
        b.OwnsOne(x => x.Content, owned =>
        {
            owned.ToJson("content");
        });
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.ChangeType).HasColumnName("change_type").HasConversion<string>();
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.DerivedFromEntryId).HasColumnName("derived_from_entry_id");
        b.Property(x => x.FictionChapterScrollId).HasColumnName("fiction_chapter_scroll_id");
        b.Property(x => x.FictionChapterSceneId).HasColumnName("fiction_chapter_scene_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionWorldBible)
            .WithMany(bible => bible.Entries)
            .HasForeignKey(x => x.FictionWorldBibleId)
            .HasConstraintName("fk_fiction_world_bible_entries_bible");
        b.HasOne(x => x.DerivedFromEntry)
            .WithMany()
            .HasForeignKey(x => x.DerivedFromEntryId)
            .HasConstraintName("fk_fiction_world_bible_entries_parent");
        b.HasOne(x => x.FictionChapterScroll)
            .WithMany(s => s.WorldBibleEntries)
            .HasForeignKey(x => x.FictionChapterScrollId)
            .HasConstraintName("fk_fiction_world_bible_entries_scroll");
        b.HasOne(x => x.FictionChapterScene)
            .WithMany(s => s.WorldBibleEntries)
            .HasForeignKey(x => x.FictionChapterSceneId)
            .HasConstraintName("fk_fiction_world_bible_entries_scene");
        b.HasIndex(x => new { x.FictionWorldBibleId, x.EntrySlug, x.Version }).IsUnique().HasDatabaseName("ux_fiction_world_bible_entries_slug_version");
    }
}
