using Microsoft.EntityFrameworkCore.Migrations;

namespace Cognition.Data.Relational.Migrations
{
    public partial class ClientProfileIsActive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "client_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "client_profiles");
        }
    }
}

