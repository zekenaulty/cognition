using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    public partial class RemoveLegacyFictionTools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fiction_projects_primary_style_guide",
                table: "fiction_projects");

            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_fiction_projects_PrimaryStyleGuideId\";");

            migrationBuilder.DropForeignKey(
                name: "fk_fiction_story_metrics_draft_segment_version",
                table: "fiction_story_metrics");

            migrationBuilder.DropForeignKey(
                name: "fk_fiction_chapter_scenes_draft_segment_version",
                table: "fiction_chapter_scenes");

            migrationBuilder.DropColumn(
                name: "primary_style_guide_id",
                table: "fiction_projects");

            migrationBuilder.DropColumn(
                name: "draft_segment_version_id",
                table: "fiction_story_metrics");

            migrationBuilder.DropColumn(
                name: "draft_segment_version_id",
                table: "fiction_chapter_scenes");

            migrationBuilder.DropTable(
                name: "annotations");

            migrationBuilder.DropTable(
                name: "draft_segment_versions");

            migrationBuilder.DropTable(
                name: "draft_segments");

            migrationBuilder.DropTable(
                name: "timeline_event_assets");

            migrationBuilder.DropTable(
                name: "timeline_events");

            migrationBuilder.DropTable(
                name: "outline_node_versions");

            migrationBuilder.DropTable(
                name: "outline_nodes");

            migrationBuilder.DropTable(
                name: "canon_rules");

            migrationBuilder.DropTable(
                name: "plot_arcs");

            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "world_asset_versions");

            migrationBuilder.DropTable(
                name: "world_assets");

            migrationBuilder.DropTable(
                name: "glossary_terms");

            migrationBuilder.DropTable(
                name: "style_guides");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Reverting removal of legacy fiction tools is not supported.");
        }
    }
}
