using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class EmbeddingTypedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "char_end",
                table: "knowledge_embeddings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "char_start",
                table: "knowledge_embeddings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "chunk_index",
                table: "knowledge_embeddings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dimensions",
                table: "knowledge_embeddings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_version",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "normalized",
                table: "knowledge_embeddings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "schema_version",
                table: "knowledge_embeddings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "space",
                table: "knowledge_embeddings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "vector_l2_norm",
                table: "knowledge_embeddings",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_dimensions",
                table: "knowledge_embeddings",
                column: "dimensions");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_model",
                table: "knowledge_embeddings",
                column: "model");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_provider",
                table: "knowledge_embeddings",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_embeddings_item_model_version_chunk",
                table: "knowledge_embeddings",
                columns: new[] { "knowledge_item_id", "model", "model_version", "chunk_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_knowledge_embeddings_dimensions",
                table: "knowledge_embeddings");

            migrationBuilder.DropIndex(
                name: "ix_knowledge_embeddings_model",
                table: "knowledge_embeddings");

            migrationBuilder.DropIndex(
                name: "ix_knowledge_embeddings_provider",
                table: "knowledge_embeddings");

            migrationBuilder.DropIndex(
                name: "ux_knowledge_embeddings_item_model_version_chunk",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "char_end",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "char_start",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "chunk_index",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "content_hash",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "dimensions",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "language",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "model",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "model_version",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "normalized",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "schema_version",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "space",
                table: "knowledge_embeddings");

            migrationBuilder.DropColumn(
                name: "vector_l2_norm",
                table: "knowledge_embeddings");
        }
    }
}
