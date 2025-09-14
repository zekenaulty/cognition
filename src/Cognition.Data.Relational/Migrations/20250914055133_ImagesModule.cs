using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class ImagesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "image_styles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    prompt_prefix = table.Column<string>(type: "text", nullable: true),
                    negative_prompt = table.Column<string>(type: "text", nullable: true),
                    defaults = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_styles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "image_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: true),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    negative_prompt = table.Column<string>(type: "text", nullable: true),
                    style_id = table.Column<Guid>(type: "uuid", nullable: true),
                    steps = table.Column<int>(type: "integer", nullable: false),
                    guidance = table.Column<float>(type: "real", nullable: false),
                    seed = table.Column<int>(type: "integer", nullable: true),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_image_assets_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_image_assets_personas",
                        column: x => x.created_by_persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_image_assets_styles",
                        column: x => x.style_id,
                        principalTable: "image_styles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_image_assets_conversation_time",
                table: "image_assets",
                columns: new[] { "conversation_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_image_assets_created_by_persona_id",
                table: "image_assets",
                column: "created_by_persona_id");

            migrationBuilder.CreateIndex(
                name: "ix_image_assets_sha256",
                table: "image_assets",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "IX_image_assets_style_id",
                table: "image_assets",
                column: "style_id");

            migrationBuilder.CreateIndex(
                name: "ux_image_styles_name",
                table: "image_styles",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "image_assets");

            migrationBuilder.DropTable(
                name: "image_styles");
        }
    }
}
