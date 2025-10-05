using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddFictionPlanGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiction_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    primary_branch_slug = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plans_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: true),
                    target_count = table.Column<int>(type: "integer", nullable: true),
                    progress = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    locked_by_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_by_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_checkpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_checkpoints_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_passes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pass_index = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_passes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_passes_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_world_bibles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    branch_slug = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_world_bibles", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_world_bibles_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_blueprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_index = table.Column<int>(type: "integer", nullable: false),
                    chapter_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: false),
                    structure = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_blueprints", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_blueprints_pass",
                        column: x => x.source_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_blueprints_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_scrolls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_blueprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    scroll_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    derived_from_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_scrolls", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scrolls_blueprint",
                        column: x => x.fiction_chapter_blueprint_id,
                        principalTable: "fiction_chapter_blueprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scrolls_parent",
                        column: x => x.derived_from_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_index = table.Column<int>(type: "integer", nullable: false),
                    section_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_sections_parent",
                        column: x => x.parent_section_id,
                        principalTable: "fiction_chapter_sections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_sections_scroll",
                        column: x => x.fiction_chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_scenes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_index = table.Column<int>(type: "integer", nullable: false),
                    scene_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    draft_segment_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    derived_from_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_scenes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scenes_draft_segment_version",
                        column: x => x.draft_segment_version_id,
                        principalTable: "draft_segment_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scenes_parent",
                        column: x => x.derived_from_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scenes_section",
                        column: x => x.fiction_chapter_section_id,
                        principalTable: "fiction_chapter_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_transcripts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "text", nullable: false),
                    fiction_chapter_blueprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    request_payload = table.Column<string>(type: "text", nullable: true),
                    response_payload = table.Column<string>(type: "text", nullable: true),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                    completion_tokens = table.Column<int>(type: "integer", nullable: true),
                    latency_ms = table.Column<double>(type: "double precision", nullable: true),
                    validation_status = table.Column<string>(type: "text", nullable: false),
                    validation_details = table.Column<string>(type: "text", nullable: true),
                    is_retry = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_transcripts", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_blueprint",
                        column: x => x.fiction_chapter_blueprint_id,
                        principalTable: "fiction_chapter_blueprints",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_story_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft_segment_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_key = table.Column<string>(type: "text", nullable: false),
                    numeric_value = table.Column<double>(type: "double precision", nullable: true),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_story_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_story_metrics_draft_segment_version",
                        column: x => x.draft_segment_version_id,
                        principalTable: "draft_segment_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_story_metrics_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_story_metrics_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_world_bible_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_world_bible_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_slug = table.Column<string>(type: "text", nullable: false),
                    entry_name = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    derived_from_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiction_chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_world_bible_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_bible",
                        column: x => x.fiction_world_bible_id,
                        principalTable: "fiction_world_bibles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_parent",
                        column: x => x.derived_from_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_scroll",
                        column: x => x.fiction_chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_blueprints_source_plan_pass_id",
                table: "fiction_chapter_blueprints",
                column: "source_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_blueprints_plan_index",
                table: "fiction_chapter_blueprints",
                columns: new[] { "fiction_plan_id", "chapter_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_blueprints_plan_slug",
                table: "fiction_chapter_blueprints",
                columns: new[] { "fiction_plan_id", "chapter_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_scenes_derived_from_scene_id",
                table: "fiction_chapter_scenes",
                column: "derived_from_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_scenes_draft_segment_version_id",
                table: "fiction_chapter_scenes",
                column: "draft_segment_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_chapter_scenes_section_index",
                table: "fiction_chapter_scenes",
                columns: new[] { "fiction_chapter_section_id", "scene_index" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_scrolls_derived_from_scroll_id",
                table: "fiction_chapter_scrolls",
                column: "derived_from_scroll_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_scrolls_blueprint_index",
                table: "fiction_chapter_scrolls",
                columns: new[] { "fiction_chapter_blueprint_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_sections_parent_section_id",
                table: "fiction_chapter_sections",
                column: "parent_section_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_chapter_sections_scroll_index",
                table: "fiction_chapter_sections",
                columns: new[] { "fiction_chapter_scroll_id", "section_index" });

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_checkpoints_plan_phase",
                table: "fiction_plan_checkpoints",
                columns: new[] { "fiction_plan_id", "phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_passes_plan_index",
                table: "fiction_plan_passes",
                columns: new[] { "fiction_plan_id", "pass_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plan_transcripts_fiction_chapter_blueprint_id",
                table: "fiction_plan_transcripts",
                column: "fiction_chapter_blueprint_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plan_transcripts_fiction_chapter_scene_id",
                table: "fiction_plan_transcripts",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_plan_transcripts_plan_created_at",
                table: "fiction_plan_transcripts",
                columns: new[] { "fiction_plan_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_fiction_plans_project_name",
                table: "fiction_plans",
                columns: new[] { "fiction_project_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_story_metrics_draft_segment_version_id",
                table: "fiction_story_metrics",
                column: "draft_segment_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_story_metrics_fiction_chapter_scene_id",
                table: "fiction_story_metrics",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_story_metrics_plan_key_created_at",
                table: "fiction_story_metrics",
                columns: new[] { "fiction_plan_id", "metric_key", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_derived_from_entry_id",
                table: "fiction_world_bible_entries",
                column: "derived_from_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_fiction_chapter_scene_id",
                table: "fiction_world_bible_entries",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_fiction_chapter_scroll_id",
                table: "fiction_world_bible_entries",
                column: "fiction_chapter_scroll_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_world_bible_entries_slug_version",
                table: "fiction_world_bible_entries",
                columns: new[] { "fiction_world_bible_id", "entry_slug", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_world_bibles_domain_branch",
                table: "fiction_world_bibles",
                columns: new[] { "fiction_plan_id", "domain", "branch_slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiction_plan_checkpoints");

            migrationBuilder.DropTable(
                name: "fiction_plan_transcripts");

            migrationBuilder.DropTable(
                name: "fiction_story_metrics");

            migrationBuilder.DropTable(
                name: "fiction_world_bible_entries");

            migrationBuilder.DropTable(
                name: "fiction_world_bibles");

            migrationBuilder.DropTable(
                name: "fiction_chapter_scenes");

            migrationBuilder.DropTable(
                name: "fiction_chapter_sections");

            migrationBuilder.DropTable(
                name: "fiction_chapter_scrolls");

            migrationBuilder.DropTable(
                name: "fiction_chapter_blueprints");

            migrationBuilder.DropTable(
                name: "fiction_plan_passes");

            migrationBuilder.DropTable(
                name: "fiction_plans");
        }
    }
}
