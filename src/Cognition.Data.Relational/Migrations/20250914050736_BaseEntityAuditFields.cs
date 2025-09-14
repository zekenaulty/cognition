using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class BaseEntityAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "tools",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tools",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "tool_provider_supports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tool_provider_supports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "tool_parameters",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tool_parameters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "tool_execution_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "tool_execution_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "system_variables",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "system_variables",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "question_categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "question_categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "prompt_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "persona_links",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "persona_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "models",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "models",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "knowledge_relations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "knowledge_relations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "knowledge_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "knowledge_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "instruction_set_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "instruction_set_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "data_sources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "data_sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "conversation_summaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "conversation_summaries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "conversation_participants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "conversation_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "conversation_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "conversation_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "api_credentials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "api_credentials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "agent_tool_bindings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at_utc",
                table: "agent_tool_bindings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "tool_provider_supports");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tool_provider_supports");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "tool_parameters");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tool_parameters");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "tool_execution_logs");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "tool_execution_logs");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "system_variables");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "system_variables");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "question_categories");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "question_categories");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "prompt_templates");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "persona_links");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "persona_links");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "models");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "models");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "knowledge_relations");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "knowledge_relations");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "knowledge_items");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "knowledge_items");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "instruction_set_items");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "instruction_set_items");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "data_sources");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "data_sources");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "conversation_summaries");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "conversation_summaries");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "conversation_participants");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "conversation_participants");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "conversation_messages");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "api_credentials");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "agent_tool_bindings");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "agent_tool_bindings");
        }
    }
}
