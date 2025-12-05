using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmGlobalDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmGlobalDefaults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmGlobalDefaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmGlobalDefaults_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmGlobalDefaults_ModelId",
                table: "LlmGlobalDefaults",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmGlobalDefaults_IsActive_Priority",
                table: "LlmGlobalDefaults",
                columns: new[] { "IsActive", "Priority" });

            // Seed an initial default pointing at Gemini Flash if present
            migrationBuilder.Sql(@"
                INSERT INTO LlmGlobalDefaults (Id, ModelId, IsActive, Priority, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
                SELECT NEWID(), m.Id, 1, 0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM Models m
                INNER JOIN Providers p ON p.Id = m.ProviderId
                WHERE p.IsActive = 1
                  AND (p.Name LIKE '%Gemini%' OR p.Name LIKE '%Google%')
                  AND (m.Name LIKE '%flash%' OR m.Name LIKE '%2.5%' OR m.Name LIKE '%2.0%')
                  AND NOT EXISTS (SELECT 1 FROM LlmGlobalDefaults);
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
