using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLlmModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var openAiId = new Guid("11111111-1111-1111-1111-111111111111");
            var now = DateTime.UtcNow;

            // Add missing OpenAI model: gpt-4o-mini
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000005"), openAiId, "gpt-4o-mini", "GPT-4o Mini", null, false, true, 0.15, 0.075, 0.60, false, null, now, null }
            );

            // Remove Gemini model not present in current supported list
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001")
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var geminiId = new Guid("33333333-3333-3333-3333-333333333333");
            var now = DateTime.UtcNow;

            // Re-insert deleted Gemini model
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[] { new Guid("30000000-0000-0000-0000-000000000001"), geminiId, "gemini-1.5-flash", "Gemini 1.5 Flash", null, true, true, 0.25, 0.125, 0.75, false, null, now, null }
            );

            // Remove added OpenAI model
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005")
            );
        }
    }
}

