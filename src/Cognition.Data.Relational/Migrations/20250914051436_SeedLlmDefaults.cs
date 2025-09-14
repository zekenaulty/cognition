using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedLlmDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Providers
            var openAiId = new Guid("11111111-1111-1111-1111-111111111111");
            var ollamaId = new Guid("22222222-2222-2222-2222-222222222222");
            var geminiId = new Guid("33333333-3333-3333-3333-333333333333");

            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "providers",
                columns: new[] { "id", "name", "display_name", "base_url", "is_active", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { openAiId, "OpenAI", "OpenAI", null, true, now, null },
                    { ollamaId, "Ollama", "Ollama", "http://localhost:11434", true, now, null },
                    { geminiId, "Gemini", "Google Gemini", null, true, now, null }
                }
            );

            // Models - OpenAI
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), openAiId, "gpt-4.5-preview", "GPT-4.5 Preview", null, false, true, 75.00, 37.50, 150.00, false, null, now, null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), openAiId, "gpt-4o", "GPT-4o", null, true, true, 2.50, 1.25, 10.00, false, null, now, null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), openAiId, "o1-mini-2024-09-12", "o1-mini", null, false, true, 15.00, 7.50, 60.00, false, null, now, null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), openAiId, "o3-mini-2025-01-31", "o3-mini", null, false, true, 1.10, 0.55, 4.40, false, null, now, null }
                }
            );

            // Models - Gemini
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), geminiId, "gemini-1.5-flash", "Gemini 1.5 Flash", null, true, true, 0.25, 0.125, 0.75, false, null, now, null },
                    { new Guid("30000000-0000-0000-0000-000000000002"), geminiId, "gemini-2.0-flash", "Gemini 2.0 Flash", null, true, true, 2.50, 1.25, 0.75, false, null, now, null },
                    { new Guid("30000000-0000-0000-0000-000000000003"), geminiId, "gemini-2.0-flash-lite", "Gemini 2.0 Flash Lite", null, true, true, 0.75, 0.375, 2.25, false, null, now, null }
                }
            );

            // Models - Ollama (local)
            migrationBuilder.InsertData(
                table: "models",
                columns: new[] { "id", "provider_id", "name", "display_name", "context_window", "supports_vision", "supports_streaming", "input_cost_per_1m", "cached_input_cost_per_1m", "output_cost_per_1m", "is_deprecated", "metadata", "created_at_utc", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), ollamaId, "llama3.2:3b", "Llama 3.2 3B", null, false, true, 0.0, 0.0, 0.0, false, null, now, null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), ollamaId, "mistral", "Mistral", null, false, true, 0.0, 0.0, 0.0, false, null, now, null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), ollamaId, "llama2-uncensored", "Llama2 Uncensored", null, false, true, 0.0, 0.0, 0.0, false, null, now, null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), ollamaId, "deepseek-r1:7b", "DeepSeek R1 7B", null, false, true, 0.0, 0.0, 0.0, false, null, now, null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), ollamaId, "deepseek-r1:1.5b", "DeepSeek R1 1.5B", null, false, true, 0.0, 0.0, 0.0, false, null, now, null }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "models",
                keyColumn: "id",
                keyValues: new object[] {
                    new Guid("10000000-0000-0000-0000-000000000001"),
                    new Guid("10000000-0000-0000-0000-000000000002"),
                    new Guid("10000000-0000-0000-0000-000000000003"),
                    new Guid("10000000-0000-0000-0000-000000000004"),
                    new Guid("30000000-0000-0000-0000-000000000001"),
                    new Guid("30000000-0000-0000-0000-000000000002"),
                    new Guid("30000000-0000-0000-0000-000000000003"),
                    new Guid("20000000-0000-0000-0000-000000000001"),
                    new Guid("20000000-0000-0000-0000-000000000002"),
                    new Guid("20000000-0000-0000-0000-000000000003"),
                    new Guid("20000000-0000-0000-0000-000000000004"),
                    new Guid("20000000-0000-0000-0000-000000000005")
                }
            );

            migrationBuilder.DeleteData(
                table: "providers",
                keyColumn: "id",
                keyValues: new object[] {
                    new Guid("11111111-1111-1111-1111-111111111111"),
                    new Guid("22222222-2222-2222-2222-222222222222"),
                    new Guid("33333333-3333-3333-3333-333333333333")
                }
            );
        }
    }
}

