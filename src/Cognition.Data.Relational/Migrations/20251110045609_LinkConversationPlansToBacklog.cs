using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class LinkConversationPlansToBacklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_conversation_tasks_conversation_plan_id",
                table: "conversation_tasks");

            migrationBuilder.AddColumn<Guid>(
                name: "current_conversation_plan_id",
                table: "fiction_plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "backlog_item_id",
                table: "conversation_tasks",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plans_current_conversation_plan_id",
                table: "fiction_plans",
                column: "current_conversation_plan_id");

            migrationBuilder.CreateIndex(
                name: "ux_conversation_tasks_plan_backlog",
                table: "conversation_tasks",
                columns: new[] { "conversation_plan_id", "backlog_item_id" },
                unique: true,
                filter: "backlog_item_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_fiction_plans_conversation_plan",
                table: "fiction_plans",
                column: "current_conversation_plan_id",
                principalTable: "conversation_plans",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fiction_plans_conversation_plan",
                table: "fiction_plans");

            migrationBuilder.DropIndex(
                name: "IX_fiction_plans_current_conversation_plan_id",
                table: "fiction_plans");

            migrationBuilder.DropIndex(
                name: "ux_conversation_tasks_plan_backlog",
                table: "conversation_tasks");

            migrationBuilder.DropColumn(
                name: "current_conversation_plan_id",
                table: "fiction_plans");

            migrationBuilder.DropColumn(
                name: "backlog_item_id",
                table: "conversation_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_tasks_conversation_plan_id",
                table: "conversation_tasks",
                column: "conversation_plan_id");
        }
    }
}
