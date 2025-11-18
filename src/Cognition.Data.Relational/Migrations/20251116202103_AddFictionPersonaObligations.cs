using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddFictionPersonaObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiction_persona_obligations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_character_id = table.Column<Guid>(type: "uuid", nullable: true),
                    obligation_slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    source_phase = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_backlog_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_persona_obligations", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_character",
                        column: x => x.fiction_character_id,
                        principalTable: "fiction_characters",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_persona_obligations_fiction_character_id",
                table: "fiction_persona_obligations",
                column: "fiction_character_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_persona_obligations_persona_id",
                table: "fiction_persona_obligations",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_persona_obligations_plan_persona_slug",
                table: "fiction_persona_obligations",
                columns: new[] { "fiction_plan_id", "persona_id", "obligation_slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiction_persona_obligations");
        }
    }
}
