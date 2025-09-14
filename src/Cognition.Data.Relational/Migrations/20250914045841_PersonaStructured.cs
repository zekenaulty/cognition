using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class PersonaStructured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "properties",
                table: "personas");

            migrationBuilder.AddColumn<string>(
                name: "background",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "beliefs",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "communication_style",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "domain_expertise",
                table: "personas",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emotional_drivers",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "essence",
                table: "personas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "personas",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "narrative_themes",
                table: "personas",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "personas",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "signature_traits",
                table: "personas",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "background",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "beliefs",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "communication_style",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "domain_expertise",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "emotional_drivers",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "essence",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "narrative_themes",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "nickname",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "signature_traits",
                table: "personas");

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "properties",
                table: "personas",
                type: "jsonb",
                nullable: false);
        }
    }
}
