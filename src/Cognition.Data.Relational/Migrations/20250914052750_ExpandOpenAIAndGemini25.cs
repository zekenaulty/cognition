using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOpenAIAndGemini25 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var openAiId = new Guid("11111111-1111-1111-1111-111111111111");
            var geminiId = new Guid("33333333-3333-3333-3333-333333333333");
            var now = DateTime.UtcNow;

            // Remove older Gemini 2.0 rows (refresh to 2.5)
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValues: new object[] {
                    new Guid("30000000-0000-0000-0000-000000000002"), // gemini-2.0-flash
                    new Guid("30000000-0000-0000-0000-000000000003")  // gemini-2.0-flash-lite
                }
            );

            // Insert Gemini 2.5 family (all multimodal)
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000101"), geminiId, "gemini-2.5-pro",        "Gemini 2.5 Pro",        null, true,  true, 1.25, null, 10.00, false, null, now, null },
                    { new Guid("30000000-0000-0000-0000-000000000102"), geminiId, "gemini-2.5-flash",      "Gemini 2.5 Flash",      null, true,  true, 0.30, null, 2.50,  false, null, now, null },
                    { new Guid("30000000-0000-0000-0000-000000000103"), geminiId, "gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite", null, true,  true, 0.15, null, 1.25,  false, null, now, null }
                }
            );

            // Insert OpenAI models (supports_vision=true for 4o/4o-mini/realtime; streaming=true for chat models)
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000006"), openAiId, "gpt-5",                 "GPT-5",                  null, false, true, null, null, null, false, null, now, null },
                    { new Guid("10000000-0000-0000-0000-000000000007"), openAiId, "gpt-5-mini",            "GPT-5 Mini",            null, false, true, null, null, null, false, null, now, null },
                    { new Guid("10000000-0000-0000-0000-000000000008"), openAiId, "gpt-5-nano",            "GPT-5 Nano",            null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000009"), openAiId, "gpt-4.1",               "GPT-4.1",               null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000010"), openAiId, "gpt-4.1-mini",          "GPT-4.1 Mini",          null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000011"), openAiId, "gpt-realtime",          "GPT Realtime",          null, true,  true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000012"), openAiId, "o4-mini",               "o4 Mini",               null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000013"), openAiId, "o4-mini-deep-research", "o4 Mini Deep Research", null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000014"), openAiId, "o3",                    "o3",                    null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000015"), openAiId, "o3-pro-2025-06-10",    "o3 Pro 2025-06-10",     null, false, true, null, null, null, false, null, now, null },
                    //{ new Guid("10000000-0000-0000-0000-000000000016"), openAiId, "o3-deep-research",      "o3 Deep Research",      null, false, true, null, null, null, false, null, now, null }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove inserted OpenAI models
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValues: new object[] {
                    new Guid("10000000-0000-0000-0000-000000000006"),
                    new Guid("10000000-0000-0000-0000-000000000007"),
                    new Guid("10000000-0000-0000-0000-000000000008"),
                    new Guid("10000000-0000-0000-0000-000000000009"),
                    new Guid("10000000-0000-0000-0000-000000000010"),
                    new Guid("10000000-0000-0000-0000-000000000011"),
                    new Guid("10000000-0000-0000-0000-000000000012"),
                    new Guid("10000000-0000-0000-0000-000000000013"),
                    new Guid("10000000-0000-0000-0000-000000000014"),
                    new Guid("10000000-0000-0000-0000-000000000015"),
                    new Guid("10000000-0000-0000-0000-000000000016")
                }
            );

            // Remove Gemini 2.5 rows
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValues: new object[] {
                    new Guid("30000000-0000-0000-0000-000000000101"),
                    new Guid("30000000-0000-0000-0000-000000000102"),
                    new Guid("30000000-0000-0000-0000-000000000103")
                }
            );

            // Re-insert Gemini 2.0 models that were removed
            var geminiId = new Guid("33333333-3333-3333-3333-333333333333");
            var now = DateTime.UtcNow;
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000002"), geminiId, "gemini-2.0-flash",      "Gemini 2.0 Flash",      null, true, true, 2.50, 1.25, 0.75,  false, null, now, null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), geminiId, "gemini-2.0-flash-lite", "Gemini 2.0 Flash Lite", null, true, true, 0.75, 0.375, 2.25, false, null, now, null }
                }
            );
        }
    }
}

