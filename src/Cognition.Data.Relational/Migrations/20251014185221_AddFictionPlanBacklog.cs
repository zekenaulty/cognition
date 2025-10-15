using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddFictionPlanBacklog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fiction_plan_backlog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    backlog_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    inputs = table.Column<string[]>(type: "jsonb", nullable: true),
                    outputs = table.Column<string[]>(type: "jsonb", nullable: true),
                    in_progress_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_backlog", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_backlog_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_backlog_plan_backlog_id",
                table: "fiction_plan_backlog",
                columns: new[] { "fiction_plan_id", "backlog_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fiction_plan_backlog");
        }
    }
}
