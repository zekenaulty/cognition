using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class FictionWorldBibleProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "agent_id",
                table: "fiction_world_bible_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_slug",
                table: "fiction_world_bible_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "persona_id",
                table: "fiction_world_bible_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_backlog_id",
                table: "fiction_world_bible_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_conversation_id",
                table: "fiction_world_bible_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_plan_pass_id",
                table: "fiction_world_bible_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_agent_id",
                table: "fiction_world_bible_entries",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_persona_id",
                table: "fiction_world_bible_entries",
                column: "persona_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fiction_world_bible_entries_agent",
                table: "fiction_world_bible_entries",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fiction_world_bible_entries_persona",
                table: "fiction_world_bible_entries",
                column: "persona_id",
                principalTable: "personas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fiction_world_bible_entries_agent",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_fiction_world_bible_entries_persona",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropIndex(
                name: "IX_fiction_world_bible_entries_agent_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropIndex(
                name: "IX_fiction_world_bible_entries_persona_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "agent_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "branch_slug",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "persona_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "source_backlog_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "source_conversation_id",
                table: "fiction_world_bible_entries");

            migrationBuilder.DropColumn(
                name: "source_plan_pass_id",
                table: "fiction_world_bible_entries");
        }
    }
}
