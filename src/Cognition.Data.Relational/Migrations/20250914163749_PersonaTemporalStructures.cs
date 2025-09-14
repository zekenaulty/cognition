using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class PersonaTemporalStructures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "persona_dreams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    valence = table.Column<int>(type: "integer", nullable: false),
                    vividness = table.Column<int>(type: "integer", nullable: false),
                    lucid = table.Column<bool>(type: "boolean", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_dreams", x => x.id);
                    table.ForeignKey(
                        name: "fk_persona_dreams_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persona_event_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persona_memory_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_memory_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persona_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    categories = table.Column<string[]>(type: "text[]", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    importance = table.Column<double>(type: "double precision", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_persona_events_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_persona_events_type",
                        column: x => x.type_id,
                        principalTable: "persona_event_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "persona_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    importance = table.Column<double>(type: "double precision", nullable: true),
                    emotions = table.Column<string[]>(type: "text[]", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_memories", x => x.id);
                    table.ForeignKey(
                        name: "fk_persona_memories_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_persona_memories_type",
                        column: x => x.type_id,
                        principalTable: "persona_memory_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_persona_dreams_timeline",
                table: "persona_dreams",
                columns: new[] { "persona_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_persona_event_types_code",
                table: "persona_event_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_persona_events_timeline",
                table: "persona_events",
                columns: new[] { "persona_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_events_type_id",
                table: "persona_events",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_persona_memories_timeline",
                table: "persona_memories",
                columns: new[] { "persona_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_memories_type_id",
                table: "persona_memories",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ux_persona_memory_types_code",
                table: "persona_memory_types",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "persona_dreams");

            migrationBuilder.DropTable(
                name: "persona_events");

            migrationBuilder.DropTable(
                name: "persona_memories");

            migrationBuilder.DropTable(
                name: "persona_event_types");

            migrationBuilder.DropTable(
                name: "persona_memory_types");
        }
    }
}
