using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class ProfileCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "api_credential_id",
                table: "client_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_api_credential_id",
                table: "client_profiles",
                column: "api_credential_id");

            migrationBuilder.AddForeignKey(
                name: "fk_client_profiles_api_credentials",
                table: "client_profiles",
                column: "api_credential_id",
                principalTable: "api_credentials",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_client_profiles_api_credentials",
                table: "client_profiles");

            migrationBuilder.DropIndex(
                name: "IX_client_profiles_api_credential_id",
                table: "client_profiles");

            migrationBuilder.DropColumn(
                name: "api_credential_id",
                table: "client_profiles");
        }
    }
}
