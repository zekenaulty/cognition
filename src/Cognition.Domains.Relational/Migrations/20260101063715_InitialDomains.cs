using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Domains.Relational.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    schema_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bounded_context_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope_string = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    asset_type = table.Column<string>(type: "text", nullable: false),
                    content_ref = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    derived_from_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scope_instances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dimension_values = table.Column<string>(type: "jsonb", nullable: false),
                    compiled_scope_string = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bounded_context_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scope_instances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scope_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    ordered_dimensions = table.Column<string>(type: "jsonb", nullable: false),
                    format_pattern = table.Column<string>(type: "text", nullable: true),
                    canonicalization_rules = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scope_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tool_descriptors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    owning_domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_schema = table.Column<string>(type: "jsonb", nullable: true),
                    output_schema = table.Column<string>(type: "jsonb", nullable: true),
                    side_effect_profile = table.Column<string>(type: "text", nullable: false),
                    human_gate_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    required_approvals = table.Column<string>(type: "jsonb", nullable: false),
                    audit_tags = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_descriptors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bounded_contexts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    context_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bounded_contexts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_manifests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    allowed_embedding_flavors = table.Column<string>(type: "jsonb", nullable: false),
                    default_embedding_flavor = table.Column<string>(type: "text", nullable: true),
                    index_isolation_policy = table.Column<string>(type: "text", nullable: false),
                    allowed_tool_categories = table.Column<string>(type: "jsonb", nullable: false),
                    required_metadata_schema = table.Column<string>(type: "jsonb", nullable: true),
                    safety_profile = table.Column<string>(type: "text", nullable: true),
                    published_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_manifests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canonical_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    parent_domain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_manifest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domains", x => x.id);
                    table.ForeignKey(
                        name: "fk_domains_current_manifest",
                        column: x => x.current_manifest_id,
                        principalTable: "domain_manifests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    deny_by_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    rules_json = table.Column<string>(type: "jsonb", nullable: true),
                    applies_to_scope = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_policies_domain",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bounded_contexts_domain_id",
                table: "bounded_contexts",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_bounded_contexts_domain_context_key",
                table: "bounded_contexts",
                columns: new[] { "domain_id", "context_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_manifests_domain_id",
                table: "domain_manifests",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_domain_manifests_domain_version",
                table: "domain_manifests",
                columns: new[] { "domain_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domains_current_manifest_id",
                table: "domains",
                column: "current_manifest_id");

            migrationBuilder.CreateIndex(
                name: "ix_domains_parent_domain_id",
                table: "domains",
                column: "parent_domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_domains_canonical_key",
                table: "domains",
                column: "canonical_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_types_domain_id",
                table: "event_types",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_event_types_domain_name",
                table: "event_types",
                columns: new[] { "domain_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_assets_domain_hash",
                table: "knowledge_assets",
                columns: new[] { "domain_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_assets_scope_string",
                table: "knowledge_assets",
                column: "scope_string");

            migrationBuilder.CreateIndex(
                name: "ix_policies_domain_id",
                table: "policies",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_policies_domain_name",
                table: "policies",
                columns: new[] { "domain_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scope_instances_context_id",
                table: "scope_instances",
                column: "bounded_context_id");

            migrationBuilder.CreateIndex(
                name: "ix_scope_instances_domain_id",
                table: "scope_instances",
                column: "domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_scope_instances_type_compiled",
                table: "scope_instances",
                columns: new[] { "scope_type_id", "compiled_scope_string" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_scope_types_name",
                table: "scope_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tool_descriptors_domain_id",
                table: "tool_descriptors",
                column: "owning_domain_id");

            migrationBuilder.CreateIndex(
                name: "ux_tool_descriptors_domain_name",
                table: "tool_descriptors",
                columns: new[] { "owning_domain_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_bounded_contexts_domain",
                table: "bounded_contexts",
                column: "domain_id",
                principalTable: "domains",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_domain_manifests_domain",
                table: "domain_manifests",
                column: "domain_id",
                principalTable: "domains",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_domain_manifests_domain",
                table: "domain_manifests");

            migrationBuilder.DropTable(
                name: "bounded_contexts");

            migrationBuilder.DropTable(
                name: "event_types");

            migrationBuilder.DropTable(
                name: "knowledge_assets");

            migrationBuilder.DropTable(
                name: "policies");

            migrationBuilder.DropTable(
                name: "scope_instances");

            migrationBuilder.DropTable(
                name: "scope_types");

            migrationBuilder.DropTable(
                name: "tool_descriptors");

            migrationBuilder.DropTable(
                name: "domains");

            migrationBuilder.DropTable(
                name: "domain_manifests");
        }
    }
}
