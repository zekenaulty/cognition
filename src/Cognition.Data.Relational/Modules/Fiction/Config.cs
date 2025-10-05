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
        b.Property(x => x.PrimaryStyleGuideId).HasColumnName("primary_style_guide_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.PrimaryStyleGuide)
            .WithMany()
            .HasForeignKey(x => x.PrimaryStyleGuideId)
            .HasConstraintName("fk_fiction_projects_primary_style_guide");
    }
}

public class StyleGuideConfiguration : IEntityTypeConfiguration<StyleGuide>
{
    public void Configure(EntityTypeBuilder<StyleGuide> b)
    {
        b.ToTable("style_guides");
        b.HasKey(x => x.Id).HasName("pk_style_guides");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Name).HasColumnName("name");
        b.Property(x => x.Rules).HasColumnName("rules").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany(p => p.StyleGuides).HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_style_guides_project");
    }
}

public class GlossaryTermConfiguration : IEntityTypeConfiguration<GlossaryTerm>
{
    public void Configure(EntityTypeBuilder<GlossaryTerm> b)
    {
        b.ToTable("glossary_terms");
        b.HasKey(x => x.Id).HasName("pk_glossary_terms");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Term).HasColumnName("term");
        b.Property(x => x.Definition).HasColumnName("definition");
        b.Property(x => x.Aliases).HasColumnName("aliases");
        b.Property(x => x.Domain).HasColumnName("domain");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany(p => p.GlossaryTerms).HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_glossary_terms_project");
        b.HasIndex(x => new { x.FictionProjectId, x.Term }).IsUnique().HasDatabaseName("ux_glossary_terms_project_term");
    }
}

public class WorldAssetConfiguration : IEntityTypeConfiguration<WorldAsset>
{
    public void Configure(EntityTypeBuilder<WorldAsset> b)
    {
        b.ToTable("world_assets");
        b.HasKey(x => x.Id).HasName("pk_world_assets");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>();
        b.Property(x => x.Name).HasColumnName("name");
        b.Property(x => x.Slug).HasColumnName("slug");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.Property(x => x.ActiveVersionIndex).HasColumnName("active_version_index");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany(p => p.WorldAssets).HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_world_assets_project");
        b.HasIndex(x => new { x.FictionProjectId, x.Type, x.Name }).HasDatabaseName("ix_world_assets_project_type_name");
    }
}

public class WorldAssetVersionConfiguration : IEntityTypeConfiguration<WorldAssetVersion>
{
    public void Configure(EntityTypeBuilder<WorldAssetVersion> b)
    {
        b.ToTable("world_asset_versions");
        b.HasKey(x => x.Id).HasName("pk_world_asset_versions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.WorldAssetId).HasColumnName("world_asset_id");
        b.Property(x => x.VersionIndex).HasColumnName("version_index");
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.WorldAsset).WithMany(a => a.Versions).HasForeignKey(x => x.WorldAssetId).HasConstraintName("fk_world_asset_versions_asset");
        b.HasIndex(x => new { x.WorldAssetId, x.VersionIndex }).IsUnique().HasDatabaseName("ux_world_asset_versions_asset_index");
    }
}

public class CanonRuleConfiguration : IEntityTypeConfiguration<CanonRule>
{
    public void Configure(EntityTypeBuilder<CanonRule> b)
    {
        b.ToTable("canon_rules");
        b.HasKey(x => x.Id).HasName("pk_canon_rules");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Scope).HasColumnName("scope").HasConversion<string>();
        b.Property(x => x.Key).HasColumnName("key");
        b.Property(x => x.Value).HasColumnName("value").HasColumnType("jsonb");
        b.Property(x => x.Evidence).HasColumnName("evidence");
        b.Property(x => x.Confidence).HasColumnName("confidence");
        b.Property(x => x.PlotArcId).HasColumnName("plot_arc_id");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_canon_rules_project");
        b.HasOne(x => x.PlotArc).WithMany().HasForeignKey(x => x.PlotArcId).HasConstraintName("fk_canon_rules_plot_arc");
        b.HasIndex(x => new { x.FictionProjectId, x.Key }).HasDatabaseName("ix_canon_rules_project_key");
    }
}

public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> b)
    {
        b.ToTable("sources");
        b.HasKey(x => x.Id).HasName("pk_sources");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Url).HasColumnName("url");
        b.Property(x => x.Citation).HasColumnName("citation");
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_sources_project");
    }
}

public class PlotArcConfiguration : IEntityTypeConfiguration<PlotArc>
{
    public void Configure(EntityTypeBuilder<PlotArc> b)
    {
        b.ToTable("plot_arcs");
        b.HasKey(x => x.Id).HasName("pk_plot_arcs");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.Name).HasColumnName("name");
        b.Property(x => x.Premise).HasColumnName("premise");
        b.Property(x => x.Goal).HasColumnName("goal");
        b.Property(x => x.Conflict).HasColumnName("conflict");
        b.Property(x => x.Resolution).HasColumnName("resolution");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany(p => p.PlotArcs).HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_plot_arcs_project");
    }
}

public class OutlineNodeConfiguration : IEntityTypeConfiguration<OutlineNode>
{
    public void Configure(EntityTypeBuilder<OutlineNode> b)
    {
        b.ToTable("outline_nodes");
        b.HasKey(x => x.Id).HasName("pk_outline_nodes");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.PlotArcId).HasColumnName("plot_arc_id");
        b.Property(x => x.ParentId).HasColumnName("parent_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>();
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.SequenceIndex).HasColumnName("sequence_index");
        b.Property(x => x.ActiveVersionIndex).HasColumnName("active_version_index");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_outline_nodes_project");
        b.HasOne(x => x.PlotArc).WithMany(a => a.OutlineNodes).HasForeignKey(x => x.PlotArcId).HasConstraintName("fk_outline_nodes_plot_arc");
        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).HasConstraintName("fk_outline_nodes_parent");
        b.HasIndex(x => new { x.FictionProjectId, x.Type, x.SequenceIndex }).HasDatabaseName("ix_outline_nodes_project_type_seq");
    }
}

public class OutlineNodeVersionConfiguration : IEntityTypeConfiguration<OutlineNodeVersion>
{
    public void Configure(EntityTypeBuilder<OutlineNodeVersion> b)
    {
        b.ToTable("outline_node_versions");
        b.HasKey(x => x.Id).HasName("pk_outline_node_versions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.OutlineNodeId).HasColumnName("outline_node_id");
        b.Property(x => x.VersionIndex).HasColumnName("version_index");
        b.Property(x => x.Beats).HasColumnName("beats").HasColumnType("jsonb");
        b.Property(x => x.Pov).HasColumnName("pov");
        b.Property(x => x.Goals).HasColumnName("goals");
        b.Property(x => x.Tension).HasColumnName("tension");
        b.Property(x => x.Status).HasColumnName("status");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.OutlineNode).WithMany(n => n.Versions).HasForeignKey(x => x.OutlineNodeId).HasConstraintName("fk_outline_node_versions_node");
        b.HasIndex(x => new { x.OutlineNodeId, x.VersionIndex }).IsUnique().HasDatabaseName("ux_outline_node_versions_node_index");
    }
}

public class TimelineEventConfiguration : IEntityTypeConfiguration<TimelineEvent>
{
    public void Configure(EntityTypeBuilder<TimelineEvent> b)
    {
        b.ToTable("timeline_events");
        b.HasKey(x => x.Id).HasName("pk_timeline_events");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.OutlineNodeId).HasColumnName("outline_node_id");
        b.Property(x => x.InWorldDate).HasColumnName("in_world_date");
        b.Property(x => x.Index).HasColumnName("index");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_timeline_events_project");
        b.HasOne(x => x.OutlineNode).WithMany().HasForeignKey(x => x.OutlineNodeId).HasConstraintName("fk_timeline_events_outline_node");
    }
}

public class TimelineEventAssetConfiguration : IEntityTypeConfiguration<TimelineEventAsset>
{
    public void Configure(EntityTypeBuilder<TimelineEventAsset> b)
    {
        b.ToTable("timeline_event_assets");
        b.HasKey(x => x.Id).HasName("pk_timeline_event_assets");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TimelineEventId).HasColumnName("timeline_event_id");
        b.Property(x => x.WorldAssetId).HasColumnName("world_asset_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.TimelineEvent).WithMany(e => e.Assets).HasForeignKey(x => x.TimelineEventId).HasConstraintName("fk_timeline_event_assets_event");
        b.HasOne(x => x.WorldAsset).WithMany().HasForeignKey(x => x.WorldAssetId).HasConstraintName("fk_timeline_event_assets_asset");
        b.HasIndex(x => new { x.TimelineEventId, x.WorldAssetId }).IsUnique().HasDatabaseName("ux_timeline_event_assets_event_asset");
    }
}

public class DraftSegmentConfiguration : IEntityTypeConfiguration<DraftSegment>
{
    public void Configure(EntityTypeBuilder<DraftSegment> b)
    {
        b.ToTable("draft_segments");
        b.HasKey(x => x.Id).HasName("pk_draft_segments");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.OutlineNodeId).HasColumnName("outline_node_id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.ActiveVersionIndex).HasColumnName("active_version_index");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_draft_segments_project");
        b.HasOne(x => x.OutlineNode).WithMany().HasForeignKey(x => x.OutlineNodeId).HasConstraintName("fk_draft_segments_outline_node");
    }
}

public class DraftSegmentVersionConfiguration : IEntityTypeConfiguration<DraftSegmentVersion>
{
    public void Configure(EntityTypeBuilder<DraftSegmentVersion> b)
    {
        b.ToTable("draft_segment_versions");
        b.HasKey(x => x.Id).HasName("pk_draft_segment_versions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.DraftSegmentId).HasColumnName("draft_segment_id");
        b.Property(x => x.VersionIndex).HasColumnName("version_index");
        b.Property(x => x.BodyMarkdown).HasColumnName("body_markdown");
        b.Property(x => x.Metrics).HasColumnName("metrics").HasColumnType("jsonb");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.DraftSegment).WithMany(d => d.Versions).HasForeignKey(x => x.DraftSegmentId).HasConstraintName("fk_draft_segment_versions_segment");
        b.HasIndex(x => new { x.DraftSegmentId, x.VersionIndex }).IsUnique().HasDatabaseName("ux_draft_segment_versions_segment_index");
    }
}

public class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> b)
    {
        b.ToTable("annotations");
        b.HasKey(x => x.Id).HasName("pk_annotations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FictionProjectId).HasColumnName("fiction_project_id");
        b.Property(x => x.TargetType).HasColumnName("target_type");
        b.Property(x => x.TargetId).HasColumnName("target_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>();
        b.Property(x => x.Message).HasColumnName("message");
        b.Property(x => x.Details).HasColumnName("details");
        b.Property(x => x.Severity).HasColumnName("severity").HasConversion<string>();
        b.Property(x => x.Resolved).HasColumnName("resolved");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionProject).WithMany().HasForeignKey(x => x.FictionProjectId).HasConstraintName("fk_annotations_project");
        b.HasIndex(x => new { x.FictionProjectId, x.TargetType, x.TargetId }).HasDatabaseName("ix_annotations_target");
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
            .WithMany(p => p.ChildSections)
            .HasForeignKey(x => x.ParentSectionId)
            .HasConstraintName("fk_fiction_chapter_sections_parent");
        b.HasIndex(x => new { x.FictionChapterScrollId, x.SectionIndex }).HasDatabaseName("ix_fiction_chapter_sections_scroll_index");
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
        b.Property(x => x.DraftSegmentVersionId).HasColumnName("draft_segment_version_id");
        b.Property(x => x.DerivedFromSceneId).HasColumnName("derived_from_scene_id");
        b.Property(x => x.BranchId).HasColumnName("branch_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasOne(x => x.FictionChapterSection)
            .WithMany(s => s.Scenes)
            .HasForeignKey(x => x.FictionChapterSectionId)
            .HasConstraintName("fk_fiction_chapter_scenes_section");
        b.HasOne(x => x.DraftSegmentVersion)
            .WithMany()
            .HasForeignKey(x => x.DraftSegmentVersionId)
            .HasConstraintName("fk_fiction_chapter_scenes_draft_segment_version");
        b.HasOne(x => x.DerivedFromScene)
            .WithMany()
            .HasForeignKey(x => x.DerivedFromSceneId)
            .HasConstraintName("fk_fiction_chapter_scenes_parent");
        b.HasIndex(x => new { x.FictionChapterSectionId, x.SceneIndex }).HasDatabaseName("ix_fiction_chapter_scenes_section_index");
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
        b.Property(x => x.DraftSegmentVersionId).HasColumnName("draft_segment_version_id");
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
        b.HasOne(x => x.DraftSegmentVersion)
            .WithMany()
            .HasForeignKey(x => x.DraftSegmentVersionId)
            .HasConstraintName("fk_fiction_story_metrics_draft_segment_version");
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
        b.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb");
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
