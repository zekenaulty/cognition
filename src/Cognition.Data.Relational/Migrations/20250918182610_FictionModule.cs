using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class FictionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_profile_id",
                table: "tools",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "client_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "annotations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<string>(type: "text", nullable: false),
                    resolved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_annotations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "canon_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    evidence = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    plot_arc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_canon_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "draft_segment_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    body_markdown = table.Column<string>(type: "text", nullable: false),
                    metrics = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_draft_segment_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "draft_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outline_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    active_version_index = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_draft_segments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiction_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    logline = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    primary_style_guide_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "glossary_terms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term = table.Column<string>(type: "text", nullable: false),
                    definition = table.Column<string>(type: "text", nullable: false),
                    aliases = table.Column<string[]>(type: "text[]", nullable: true),
                    domain = table.Column<string>(type: "text", nullable: true),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_glossary_terms", x => x.id);
                    table.ForeignKey(
                        name: "fk_glossary_terms_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plot_arcs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    premise = table.Column<string>(type: "text", nullable: true),
                    goal = table.Column<string>(type: "text", nullable: true),
                    conflict = table.Column<string>(type: "text", nullable: true),
                    resolution = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plot_arcs", x => x.id);
                    table.ForeignKey(
                        name: "fk_plot_arcs_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    citation = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_sources_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "style_guides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    rules = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_style_guides", x => x.id);
                    table.ForeignKey(
                        name: "fk_style_guides_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: true),
                    active_version_index = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_world_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_world_assets_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outline_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plot_arc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    sequence_index = table.Column<int>(type: "integer", nullable: false),
                    active_version_index = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outline_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_outline_nodes_parent",
                        column: x => x.parent_id,
                        principalTable: "outline_nodes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_outline_nodes_plot_arc",
                        column: x => x.plot_arc_id,
                        principalTable: "plot_arcs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_outline_nodes_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "world_asset_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    world_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_world_asset_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_world_asset_versions_asset",
                        column: x => x.world_asset_id,
                        principalTable: "world_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outline_node_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outline_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    beats = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    pov = table.Column<string>(type: "text", nullable: true),
                    goals = table.Column<string>(type: "text", nullable: true),
                    tension = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outline_node_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_outline_node_versions_node",
                        column: x => x.outline_node_id,
                        principalTable: "outline_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timeline_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outline_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    in_world_date = table.Column<string>(type: "text", nullable: true),
                    index = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_timeline_events_outline_node",
                        column: x => x.outline_node_id,
                        principalTable: "outline_nodes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_timeline_events_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timeline_event_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timeline_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    world_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timeline_event_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_timeline_event_assets_asset",
                        column: x => x.world_asset_id,
                        principalTable: "world_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_timeline_event_assets_event",
                        column: x => x.timeline_event_id,
                        principalTable: "timeline_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tools_client_profile_id",
                table: "tools",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_annotations_target",
                table: "annotations",
                columns: new[] { "fiction_project_id", "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_canon_rules_plot_arc_id",
                table: "canon_rules",
                column: "plot_arc_id");

            migrationBuilder.CreateIndex(
                name: "ix_canon_rules_project_key",
                table: "canon_rules",
                columns: new[] { "fiction_project_id", "key" });

            migrationBuilder.CreateIndex(
                name: "ux_draft_segment_versions_segment_index",
                table: "draft_segment_versions",
                columns: new[] { "draft_segment_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_draft_segments_fiction_project_id",
                table: "draft_segments",
                column: "fiction_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_draft_segments_outline_node_id",
                table: "draft_segments",
                column: "outline_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_projects_primary_style_guide_id",
                table: "fiction_projects",
                column: "primary_style_guide_id");

            migrationBuilder.CreateIndex(
                name: "ux_glossary_terms_project_term",
                table: "glossary_terms",
                columns: new[] { "fiction_project_id", "term" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_outline_node_versions_node_index",
                table: "outline_node_versions",
                columns: new[] { "outline_node_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outline_nodes_parent_id",
                table: "outline_nodes",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_outline_nodes_plot_arc_id",
                table: "outline_nodes",
                column: "plot_arc_id");

            migrationBuilder.CreateIndex(
                name: "ix_outline_nodes_project_type_seq",
                table: "outline_nodes",
                columns: new[] { "fiction_project_id", "type", "sequence_index" });

            migrationBuilder.CreateIndex(
                name: "IX_plot_arcs_fiction_project_id",
                table: "plot_arcs",
                column: "fiction_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_sources_fiction_project_id",
                table: "sources",
                column: "fiction_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_style_guides_fiction_project_id",
                table: "style_guides",
                column: "fiction_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_timeline_event_assets_world_asset_id",
                table: "timeline_event_assets",
                column: "world_asset_id");

            migrationBuilder.CreateIndex(
                name: "ux_timeline_event_assets_event_asset",
                table: "timeline_event_assets",
                columns: new[] { "timeline_event_id", "world_asset_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_timeline_events_fiction_project_id",
                table: "timeline_events",
                column: "fiction_project_id");

            migrationBuilder.CreateIndex(
                name: "IX_timeline_events_outline_node_id",
                table: "timeline_events",
                column: "outline_node_id");

            migrationBuilder.CreateIndex(
                name: "ux_world_asset_versions_asset_index",
                table: "world_asset_versions",
                columns: new[] { "world_asset_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_world_assets_project_type_name",
                table: "world_assets",
                columns: new[] { "fiction_project_id", "type", "name" });

            migrationBuilder.AddForeignKey(
                name: "fk_tools_client_profiles",
                table: "tools",
                column: "client_profile_id",
                principalTable: "client_profiles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_annotations_project",
                table: "annotations",
                column: "fiction_project_id",
                principalTable: "fiction_projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_canon_rules_plot_arc",
                table: "canon_rules",
                column: "plot_arc_id",
                principalTable: "plot_arcs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_canon_rules_project",
                table: "canon_rules",
                column: "fiction_project_id",
                principalTable: "fiction_projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_draft_segment_versions_segment",
                table: "draft_segment_versions",
                column: "draft_segment_id",
                principalTable: "draft_segments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_draft_segments_outline_node",
                table: "draft_segments",
                column: "outline_node_id",
                principalTable: "outline_nodes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_draft_segments_project",
                table: "draft_segments",
                column: "fiction_project_id",
                principalTable: "fiction_projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fiction_projects_primary_style_guide",
                table: "fiction_projects",
                column: "primary_style_guide_id",
                principalTable: "style_guides",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tools_client_profiles",
                table: "tools");

            migrationBuilder.DropForeignKey(
                name: "fk_style_guides_project",
                table: "style_guides");

            migrationBuilder.DropTable(
                name: "annotations");

            migrationBuilder.DropTable(
                name: "canon_rules");

            migrationBuilder.DropTable(
                name: "draft_segment_versions");

            migrationBuilder.DropTable(
                name: "glossary_terms");

            migrationBuilder.DropTable(
                name: "outline_node_versions");

            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.DropTable(
                name: "timeline_event_assets");

            migrationBuilder.DropTable(
                name: "world_asset_versions");

            migrationBuilder.DropTable(
                name: "draft_segments");

            migrationBuilder.DropTable(
                name: "timeline_events");

            migrationBuilder.DropTable(
                name: "world_assets");

            migrationBuilder.DropTable(
                name: "outline_nodes");

            migrationBuilder.DropTable(
                name: "plot_arcs");

            migrationBuilder.DropTable(
                name: "fiction_projects");

            migrationBuilder.DropTable(
                name: "style_guides");

            migrationBuilder.DropIndex(
                name: "IX_tools_client_profile_id",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "client_profile_id",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "client_profiles");
        }
    }
}
