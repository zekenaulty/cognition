using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class ScopePathDualWrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "scope_path",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scope_principal_id",
                table: "knowledge_embeddings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope_principal_type",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "scope_segments",
                table: "knowledge_embeddings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_scope_principal",
                table: "knowledge_embeddings",
                columns: new[] { "scope_principal_type", "scope_principal_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_knowledge_embeddings_scope_principal",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "scope_path",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "scope_principal_id",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "scope_principal_type",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "scope_segments",
                table: "knowledge_embeddings");
        }
    }
}