using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class PersonaTypeFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "persona_type",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "Assistant");

            // Backfill existing rows
            migrationBuilder.Sql("UPDATE personas SET persona_type = 'Assistant' WHERE persona_type IS NULL OR persona_type = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "persona_type",
                table: "personas");
        }
    }
}
