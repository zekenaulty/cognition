using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreThoughts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "result",
                table: "conversation_tasks",
                newName: "tool_name");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "conversation_tasks",
                newName: "status");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_thought_id",
                table: "conversation_thoughts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_snapshot_json",
                table: "conversation_thoughts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prompt",
                table: "conversation_thoughts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rationale",
                table: "conversation_thoughts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "step_number",
                table: "conversation_thoughts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "args_json",
                table: "conversation_tasks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error",
                table: "conversation_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_answer",
                table: "conversation_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "finish",
                table: "conversation_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "goal",
                table: "conversation_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observation",
                table: "conversation_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rationale",
                table: "conversation_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tool_id",
                table: "conversation_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "outline_json",
                table: "conversation_plans",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "parent_thought_id",
                table: "conversation_thoughts");

            migrationBuilder.DropColumn(
                name: "plan_snapshot_json",
                table: "conversation_thoughts");

            migrationBuilder.DropColumn(
                name: "prompt",
                table: "conversation_thoughts");

            migrationBuilder.DropColumn(
                name: "rationale",
                table: "conversation_thoughts");

            migrationBuilder.DropColumn(
                name: "step_number",
                table: "conversation_thoughts");

            migrationBuilder.DropColumn(
                name: "args_json",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "error",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "final_answer",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "finish",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "goal",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "observation",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "rationale",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "tool_id",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "outline_json",
                table: "conversation_plans");

            migrationBuilder.RenameColumn(
                name: "tool_name",
                table: "conversation_tasks",
                newName: "result");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "conversation_tasks",
                newName: "action");
        }
    }
}
