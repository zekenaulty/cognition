using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class PersonaOwnershipRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_personas_owner",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "personas");

            migrationBuilder.AddColumn<bool>(
                name: "is_owner",
                table: "user_personas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "owned_by",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_owner",
                table: "user_personas");

            migrationBuilder.DropColumn(
                name: "owned_by",
                table: "personas");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "personas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_personas_owner",
                table: "personas",
                column: "owner_user_id");
        }
    }
}
