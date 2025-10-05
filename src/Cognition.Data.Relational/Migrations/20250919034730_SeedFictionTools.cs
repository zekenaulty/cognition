using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedFictionTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM tool_parameters WHERE tool_id IN (SELECT id FROM tools WHERE name IN ('WorldbuilderTool','LoreKeeperTool','OutlinerTool','SceneDraftTool','FactCheckerTool','RewriterTool','NPCDesignerTool','NPCSimulatorTool'));
DELETE FROM tools WHERE name IN ('WorldbuilderTool','LoreKeeperTool','OutlinerTool','SceneDraftTool','FactCheckerTool','RewriterTool','NPCDesignerTool','NPCSimulatorTool');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Re-introducing legacy fiction tools is not supported.");
        }
    }
}
