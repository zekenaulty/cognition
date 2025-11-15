using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddFictionCharactersAndLoreRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiction_characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    world_bible_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    importance = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    provenance_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_characters", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_characters_agent",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_created_pass",
                        column: x => x.created_by_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_first_scene",
                        column: x => x.first_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_characters_world_bible_entry",
                        column: x => x.world_bible_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_lore_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    world_bible_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requirement_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_lore_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_created_pass",
                        column: x => x.created_by_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_scene",
                        column: x => x.chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_scroll",
                        column: x => x.chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_world_bible_entry",
                        column: x => x.world_bible_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_agent_id",
                table: "fiction_characters",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_created_by_plan_pass_id",
                table: "fiction_characters",
                column: "created_by_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_first_scene_id",
                table: "fiction_characters",
                column: "first_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_persona_id",
                table: "fiction_characters",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_world_bible_entry_id",
                table: "fiction_characters",
                column: "world_bible_entry_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_characters_plan_slug",
                table: "fiction_characters",
                columns: new[] { "fiction_plan_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_chapter_scene_id",
                table: "fiction_lore_requirements",
                column: "chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_chapter_scroll_id",
                table: "fiction_lore_requirements",
                column: "chapter_scroll_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_created_by_plan_pass_id",
                table: "fiction_lore_requirements",
                column: "created_by_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_world_bible_entry_id",
                table: "fiction_lore_requirements",
                column: "world_bible_entry_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_lore_requirements_plan_slug",
                table: "fiction_lore_requirements",
                columns: new[] { "fiction_plan_id", "requirement_slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiction_characters");

            migrationBuilder.DropTable(
                name: "fiction_lore_requirements");
        }
    }
}
