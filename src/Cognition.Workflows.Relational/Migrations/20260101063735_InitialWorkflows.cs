using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Workflows.Relational.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    node_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_nodes_definition",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_edges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_edges", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_edges_definition",
                        column: x => x.workflow_definition_id,
                        principalTable: "workflow_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workflow_edges_from_node",
                        column: x => x.from_node_id,
                        principalTable: "workflow_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workflow_edges_to_node",
                        column: x => x.to_node_id,
                        principalTable: "workflow_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_workflow_definitions_name_version",
                table: "workflow_definitions",
                columns: new[] { "name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_edges_definition_id",
                table: "workflow_edges",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_edges_from_node",
                table: "workflow_edges",
                columns: new[] { "workflow_definition_id", "from_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_edges_from_node_id",
                table: "workflow_edges",
                column: "from_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_edges_to_node",
                table: "workflow_edges",
                columns: new[] { "workflow_definition_id", "to_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_edges_to_node_id",
                table: "workflow_edges",
                column: "to_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_definition_id",
                table: "workflow_executions",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_executions_definition_status",
                table: "workflow_executions",
                columns: new[] { "workflow_definition_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_nodes_definition_id",
                table: "workflow_nodes",
                column: "workflow_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_workflow_nodes_definition_key",
                table: "workflow_nodes",
                columns: new[] { "workflow_definition_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_edges");

            migrationBuilder.DropTable(
                name: "workflow_executions");

            migrationBuilder.DropTable(
                name: "workflow_nodes");

            migrationBuilder.DropTable(
                name: "workflow_definitions");
        }
    }
}
