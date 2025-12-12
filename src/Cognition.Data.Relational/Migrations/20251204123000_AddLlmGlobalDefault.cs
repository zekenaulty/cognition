using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Cognition.Data.Relational;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CognitionDbContext))]
    [Migration("20251204123000_AddLlmGlobalDefault")]
    public partial class AddLlmGlobalDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean up any prior mis-cased table from earlier attempts
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"LlmGlobalDefaults\" CASCADE;");

            migrationBuilder.CreateTable(
                name: "llm_global_defaults",
                columns: table => new
                {
                    Id = table.Column<Guid>(name: "id", type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(name: "model_id", type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(name: "is_active", type: "boolean", nullable: false),
                    Priority = table.Column<int>(name: "priority", type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(name: "updated_by_user_id", type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(name: "created_at_utc", type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(name: "updated_at_utc", type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_global_defaults", x => x.Id);
                    table.ForeignKey(
                        name: "fk_llm_global_defaults_models",
                        column: x => x.ModelId,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_llm_global_defaults_model_id",
                table: "llm_global_defaults",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_global_defaults_is_active_priority",
                table: "llm_global_defaults",
                columns: new[] { "is_active", "priority" });

            // Seed an initial default pointing at Gemini Flash if present
            migrationBuilder.Sql(@"
                INSERT INTO llm_global_defaults (id, model_id, is_active, priority, updated_by_user_id, created_at_utc, updated_at_utc)
                SELECT COALESCE(gen_random_uuid(), uuid_generate_v4()), m.id, TRUE, 0, NULL, NOW(), NOW()
                FROM models m
                INNER JOIN providers p ON p.id = m.provider_id
                WHERE p.is_active = TRUE
                  AND (p.name ILIKE '%gemini%' OR p.name ILIKE '%google%')
                  AND (m.name ILIKE '%flash%' OR m.name ILIKE '%2.5%' OR m.name ILIKE '%2.0%')
                  AND NOT EXISTS (SELECT 1 FROM llm_global_defaults);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmGlobalDefaults");
        }
    }
}
