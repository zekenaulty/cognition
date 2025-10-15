using System;
using System.Collections.Generic;
using Cognition.Data.Relational.Modules.Planning;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class PlannerExecutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planner_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planner_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    primary_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    scope_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    conversation_state = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    artifacts = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    metrics = table.Column<Dictionary<string, double>>(type: "jsonb", nullable: true),
                    diagnostics = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    transcript = table.Column<List<PlannerExecutionTranscriptEntry>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planner_executions", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planner_executions");
        }
    }
}
