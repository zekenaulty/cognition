using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotRebuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    data_source_type = table.Column<string>(type: "text", nullable: false),
                    collection_name = table.Column<string>(type: "text", nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fiction_projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    logline = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "image_styles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    prompt_prefix = table.Column<string>(type: "text", nullable: true),
                    negative_prompt = table.Column<string>(type: "text", nullable: true),
                    defaults = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_image_styles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instruction_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scope = table.Column<string>(type: "text", nullable: true),
                    scope_ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruction_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instructions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    role_play = table.Column<bool>(type: "boolean", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    version = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    categories = table.Column<string[]>(type: "text[]", nullable: true),
                    keywords = table.Column<string[]>(type: "text[]", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_items", x => x.id);
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
                    properties = table.Column<string>(type: "jsonb", nullable: true),
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
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_memory_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    nickname = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    persona_type = table.Column<string>(type: "text", nullable: false),
                    owned_by = table.Column<string>(type: "text", nullable: false),
                    gender = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    essence = table.Column<string>(type: "text", nullable: false),
                    beliefs = table.Column<string>(type: "text", nullable: false),
                    background = table.Column<string>(type: "text", nullable: false),
                    communication_style = table.Column<string>(type: "text", nullable: false),
                    emotional_drivers = table.Column<string>(type: "text", nullable: false),
                    voice = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    signature_traits = table.Column<string[]>(type: "text[]", nullable: true),
                    narrative_themes = table.Column<string[]>(type: "text[]", nullable: true),
                    domain_expertise = table.Column<string[]>(type: "text[]", nullable: true),
                    known_personas = table.Column<Guid[]>(type: "uuid[]", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planner_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planner_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    primary_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    scope_path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    conversation_state = table.Column<string>(type: "jsonb", nullable: true),
                    artifacts = table.Column<string>(type: "jsonb", nullable: true),
                    metrics = table.Column<string>(type: "jsonb", nullable: true),
                    diagnostics = table.Column<string>(type: "jsonb", nullable: true),
                    transcript = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planner_executions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    prompt_type = table.Column<string>(type: "text", nullable: false),
                    template = table.Column<string>(type: "text", nullable: false),
                    tokens = table.Column<string>(type: "jsonb", nullable: true),
                    example = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prompt_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    base_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_variables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_variables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "instruction_set_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruction_set_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_instruction_set_items_instructions",
                        column: x => x.instruction_id,
                        principalTable: "instructions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_instruction_set_items_sets",
                        column: x => x.instruction_set_id,
                        principalTable: "instruction_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_embeddings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    knowledge_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    vector = table.Column<float[]>(type: "real[]", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    model_version = table.Column<string>(type: "text", nullable: true),
                    dimensions = table.Column<int>(type: "integer", nullable: true),
                    space = table.Column<string>(type: "text", nullable: true),
                    normalized = table.Column<bool>(type: "boolean", nullable: true),
                    vector_l2_norm = table.Column<double>(type: "double precision", nullable: true),
                    content_hash = table.Column<string>(type: "text", nullable: true),
                    chunk_index = table.Column<int>(type: "integer", nullable: true),
                    char_start = table.Column<int>(type: "integer", nullable: true),
                    char_end = table.Column<int>(type: "integer", nullable: true),
                    language = table.Column<string>(type: "text", nullable: true),
                    schema_version = table.Column<int>(type: "integer", nullable: true),
                    scope_principal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scope_principal_type = table.Column<string>(type: "text", nullable: true),
                    scope_path = table.Column<string>(type: "text", nullable: true),
                    scope_segments = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_embeddings", x => x.id);
                    table.ForeignKey(
                        name: "fk_knowledge_embeddings_item",
                        column: x => x.knowledge_item_id,
                        principalTable: "knowledge_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "text", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_knowledge_relations_from",
                        column: x => x.from_item_id,
                        principalTable: "knowledge_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_knowledge_relations_to",
                        column: x => x.to_item_id,
                        principalTable: "knowledge_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    properties = table.Column<string>(type: "jsonb", nullable: true),
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
                    properties = table.Column<string>(type: "jsonb", nullable: true),
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
                name: "persona_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_persona_links_from",
                        column: x => x.from_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_persona_links_to",
                        column: x => x.to_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    properties = table.Column<string>(type: "jsonb", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "persona_personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persona_personas", x => x.id);
                    table.ForeignKey(
                        name: "fk_persona_personas_from",
                        column: x => x.from_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_persona_personas_to",
                        column: x => x.to_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    normalized_username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    password_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    password_algo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash_version = table.Column<int>(type: "integer", nullable: false),
                    password_updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    primary_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_primary_persona",
                        column: x => x.primary_persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "api_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    last_used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_credentials_providers",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    context_window = table.Column<int>(type: "integer", nullable: true),
                    supports_vision = table.Column<bool>(type: "boolean", nullable: false),
                    supports_streaming = table.Column<bool>(type: "boolean", nullable: false),
                    input_cost_per_1m = table.Column<double>(type: "double precision", nullable: true),
                    cached_input_cost_per_1m = table.Column<double>(type: "double precision", nullable: true),
                    output_cost_per_1m = table.Column<double>(type: "double precision", nullable: true),
                    is_deprecated = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_models_providers",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    difficulty = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_questions_categories",
                        column: x => x.category_id,
                        principalTable: "question_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_personas", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_personas_personas",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_personas_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    api_credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "text", nullable: true),
                    base_url_override = table.Column<string>(type: "text", nullable: true),
                    max_tokens = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: false),
                    top_p = table.Column<double>(type: "double precision", nullable: false),
                    presence_penalty = table.Column<double>(type: "double precision", nullable: false),
                    frequency_penalty = table.Column<double>(type: "double precision", nullable: false),
                    stream = table.Column<bool>(type: "boolean", nullable: false),
                    logging_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_profiles_api_credentials",
                        column: x => x.api_credential_id,
                        principalTable: "api_credentials",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_client_profiles_models",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_client_profiles_providers",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "llm_global_defaults",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_global_defaults", x => x.id);
                    table.ForeignKey(
                        name: "fk_llm_global_defaults_models",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    role_play = table.Column<bool>(type: "boolean", nullable: false),
                    prefix = table.Column<string>(type: "text", nullable: true),
                    suffix = table.Column<string>(type: "text", nullable: true),
                    client_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agents", x => x.id);
                    table.ForeignKey(
                        name: "fk_agents_client_profiles",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_agents_personas",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    class_path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    example = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    client_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tools", x => x.id);
                    table.ForeignKey(
                        name: "fk_tools_client_profiles",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversations_agents",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_tool_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_tool_bindings", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_tool_bindings_agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_agent_tool_bindings_tools",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tool_execution_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    request = table.Column<string>(type: "jsonb", nullable: true),
                    response = table.Column<string>(type: "jsonb", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_execution_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_tool_execution_logs_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tool_execution_logs_tools",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tool_parameters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "jsonb", nullable: true),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_parameters", x => x.id);
                    table.ForeignKey(
                        name: "fk_tool_parameters_tools",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tool_provider_supports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    support_level = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tool_provider_supports", x => x.id);
                    table.ForeignKey(
                        name: "fk_tool_provider_supports_models",
                        column: x => x.model_id,
                        principalTable: "models",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tool_provider_supports_providers",
                        column: x => x.provider_id,
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tool_provider_supports_tools",
                        column: x => x.tool_id,
                        principalTable: "tools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    metatype = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    active_version_index = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_messages_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_messages_from_agent",
                        column: x => x.from_agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_messages_from_persona",
                        column: x => x.from_persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_conversation_messages_to_persona",
                        column: x => x.to_persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conversation_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_participants_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_participants_personas",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outline_json = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_plans_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_plans_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conversation_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    by_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    references_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_summaries_by_persona",
                        column: x => x.by_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_summaries_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_summaries_ref_persona",
                        column: x => x.references_persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conversation_thoughts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thought = table.Column<string>(type: "text", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    rationale = table.Column<string>(type: "text", nullable: true),
                    plan_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    prompt = table.Column<string>(type: "text", nullable: true),
                    parent_thought_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_thoughts", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_thoughts_conversations",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversation_thoughts_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_workflow_states",
                columns: table => new
                {
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Pointer = table.Column<int>(type: "integer", nullable: false),
                    Blackboard = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_workflow_states", x => x.ConversationId);
                    table.ForeignKey(
                        name: "FK_conversation_workflow_states_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "conversation_message_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_message_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_message_versions_message",
                        column: x => x.conversation_message_id,
                        principalTable: "conversation_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_number = table.Column<int>(type: "integer", nullable: false),
                    thought = table.Column<string>(type: "text", nullable: false),
                    goal = table.Column<string>(type: "text", nullable: true),
                    rationale = table.Column<string>(type: "text", nullable: true),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tool_name = table.Column<string>(type: "text", nullable: true),
                    args_json = table.Column<string>(type: "jsonb", nullable: true),
                    observation = table.Column<string>(type: "text", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    finish = table.Column<bool>(type: "boolean", nullable: false),
                    final_answer = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    backlog_item_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_tasks_plan",
                        column: x => x.conversation_plan_id,
                        principalTable: "conversation_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    primary_branch_slug = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    current_conversation_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plans_conversation_plan",
                        column: x => x.current_conversation_plan_id,
                        principalTable: "conversation_plans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_plans_project",
                        column: x => x.fiction_project_id,
                        principalTable: "fiction_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_backlog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    backlog_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    inputs = table.Column<string[]>(type: "jsonb", nullable: true),
                    outputs = table.Column<string[]>(type: "jsonb", nullable: true),
                    in_progress_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_backlog", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_backlog_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_checkpoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: true),
                    target_count = table.Column<int>(type: "integer", nullable: true),
                    progress = table.Column<string>(type: "jsonb", nullable: true),
                    locked_by_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_by_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    locked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_checkpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_checkpoints_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_passes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pass_index = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_passes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_passes_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_world_bibles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    branch_slug = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_world_bibles", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_world_bibles_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_blueprints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_index = table.Column<int>(type: "integer", nullable: false),
                    chapter_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: false),
                    structure = table.Column<string>(type: "jsonb", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_blueprints", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_blueprints_pass",
                        column: x => x.source_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_blueprints_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_scrolls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_blueprint_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_index = table.Column<int>(type: "integer", nullable: false),
                    scroll_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    synopsis = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    derived_from_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_scrolls", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scrolls_blueprint",
                        column: x => x.fiction_chapter_blueprint_id,
                        principalTable: "fiction_chapter_blueprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scrolls_parent",
                        column: x => x.derived_from_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_index = table.Column<int>(type: "integer", nullable: false),
                    section_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_sections_parent",
                        column: x => x.parent_section_id,
                        principalTable: "fiction_chapter_sections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_sections_scroll",
                        column: x => x.fiction_chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_chapter_scenes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_index = table.Column<int>(type: "integer", nullable: false),
                    scene_slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    derived_from_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_chapter_scenes", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scenes_parent",
                        column: x => x.derived_from_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_chapter_scenes_section",
                        column: x => x.fiction_chapter_section_id,
                        principalTable: "fiction_chapter_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fiction_plan_transcripts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phase = table.Column<string>(type: "text", nullable: false),
                    fiction_chapter_blueprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conversation_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    request_payload = table.Column<string>(type: "text", nullable: true),
                    response_payload = table.Column<string>(type: "text", nullable: true),
                    prompt_tokens = table.Column<int>(type: "integer", nullable: true),
                    completion_tokens = table.Column<int>(type: "integer", nullable: true),
                    latency_ms = table.Column<double>(type: "double precision", nullable: true),
                    validation_status = table.Column<string>(type: "text", nullable: false),
                    validation_details = table.Column<string>(type: "text", nullable: true),
                    is_retry = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_plan_transcripts", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_blueprint",
                        column: x => x.fiction_chapter_blueprint_id,
                        principalTable: "fiction_chapter_blueprints",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_plan_transcripts_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_story_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric_key = table.Column<string>(type: "text", nullable: false),
                    numeric_value = table.Column<double>(type: "double precision", nullable: true),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_story_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_story_metrics_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_story_metrics_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_world_bible_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_world_bible_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_slug = table.Column<string>(type: "text", nullable: false),
                    entry_name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    derived_from_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_backlog_id = table.Column<string>(type: "text", nullable: true),
                    branch_slug = table.Column<string>(type: "text", nullable: true),
                    fiction_chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fiction_chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    content = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_world_bible_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_agent",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_bible",
                        column: x => x.fiction_world_bible_id,
                        principalTable: "fiction_world_bibles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_parent",
                        column: x => x.derived_from_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_scene",
                        column: x => x.fiction_chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_world_bible_entries_scroll",
                        column: x => x.fiction_chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    world_bible_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    importance = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    summary = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    provenance_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_characters", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_characters_agent",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_created_pass",
                        column: x => x.created_by_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_first_scene",
                        column: x => x.first_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_characters_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_characters_world_bible_entry",
                        column: x => x.world_bible_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_lore_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_scroll_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chapter_scene_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    world_bible_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requirement_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    notes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_lore_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_created_pass",
                        column: x => x.created_by_plan_pass_id,
                        principalTable: "fiction_plan_passes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_scene",
                        column: x => x.chapter_scene_id,
                        principalTable: "fiction_chapter_scenes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_scroll",
                        column: x => x.chapter_scroll_id,
                        principalTable: "fiction_chapter_scrolls",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_lore_requirements_world_bible_entry",
                        column: x => x.world_bible_entry_id,
                        principalTable: "fiction_world_bible_entries",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fiction_persona_obligations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiction_character_id = table.Column<Guid>(type: "uuid", nullable: true),
                    obligation_slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    description = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    source_phase = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_backlog_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source_plan_pass_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiction_persona_obligations", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_character",
                        column: x => x.fiction_character_id,
                        principalTable: "fiction_characters",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_persona",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiction_persona_obligations_plan",
                        column: x => x.fiction_plan_id,
                        principalTable: "fiction_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_tool_bindings_AgentId",
                table: "agent_tool_bindings",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_tool_bindings_tool_id",
                table: "agent_tool_bindings",
                column: "tool_id");

            migrationBuilder.CreateIndex(
                name: "ux_agent_tool_bindings_scope_tool",
                table: "agent_tool_bindings",
                columns: new[] { "scope_type", "scope_id", "tool_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agents_client_profile_id",
                table: "agents",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_agents_persona_id",
                table: "agents",
                column: "persona_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_api_credentials_provider_keyref",
                table: "api_credentials",
                columns: new[] { "provider_id", "key_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_api_credential_id",
                table: "client_profiles",
                column: "api_credential_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_model_id",
                table: "client_profiles",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_profiles_name",
                table: "client_profiles",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_provider_id",
                table: "client_profiles",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ux_conversation_message_versions_message_index",
                table: "conversation_message_versions",
                columns: new[] { "conversation_message_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversation_messages_conversation_ts",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_from_agent_id",
                table: "conversation_messages",
                column: "from_agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_from_persona_id",
                table: "conversation_messages",
                column: "from_persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_to_persona_id",
                table: "conversation_messages",
                column: "to_persona_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_participants_conversation_persona",
                table: "conversation_participants",
                columns: new[] { "conversation_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_participants_persona_id",
                table: "conversation_participants",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_plans_conversation_id",
                table: "conversation_plans",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_plans_persona_id",
                table: "conversation_plans",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_summaries_by_persona_id",
                table: "conversation_summaries",
                column: "by_persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_summaries_conversation_id",
                table: "conversation_summaries",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_summaries_references_persona_id",
                table: "conversation_summaries",
                column: "references_persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_conversation_tasks_plan_backlog",
                table: "conversation_tasks",
                columns: new[] { "conversation_plan_id", "backlog_item_id" },
                unique: true,
                filter: "backlog_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_thoughts_conversation_id",
                table: "conversation_thoughts",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_thoughts_persona_id",
                table: "conversation_thoughts",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_agent_id",
                table: "conversations",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ux_data_sources_name_type",
                table: "data_sources",
                columns: new[] { "name", "data_source_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_feature_flags_key",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_blueprints_source_plan_pass_id",
                table: "fiction_chapter_blueprints",
                column: "source_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_blueprints_plan_index",
                table: "fiction_chapter_blueprints",
                columns: new[] { "fiction_plan_id", "chapter_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_blueprints_plan_slug",
                table: "fiction_chapter_blueprints",
                columns: new[] { "fiction_plan_id", "chapter_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_scenes_derived_from_scene_id",
                table: "fiction_chapter_scenes",
                column: "derived_from_scene_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_scenes_section_index",
                table: "fiction_chapter_scenes",
                columns: new[] { "fiction_chapter_section_id", "scene_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_scrolls_derived_from_scroll_id",
                table: "fiction_chapter_scrolls",
                column: "derived_from_scroll_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_scrolls_blueprint_index",
                table: "fiction_chapter_scrolls",
                columns: new[] { "fiction_chapter_blueprint_id", "version_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_chapter_sections_parent_section_id",
                table: "fiction_chapter_sections",
                column: "parent_section_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_chapter_sections_scroll_index",
                table: "fiction_chapter_sections",
                columns: new[] { "fiction_chapter_scroll_id", "section_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_agent_id",
                table: "fiction_characters",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_created_by_plan_pass_id",
                table: "fiction_characters",
                column: "created_by_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_first_scene_id",
                table: "fiction_characters",
                column: "first_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_persona_id",
                table: "fiction_characters",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_characters_world_bible_entry_id",
                table: "fiction_characters",
                column: "world_bible_entry_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_characters_plan_slug",
                table: "fiction_characters",
                columns: new[] { "fiction_plan_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_chapter_scene_id",
                table: "fiction_lore_requirements",
                column: "chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_chapter_scroll_id",
                table: "fiction_lore_requirements",
                column: "chapter_scroll_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_created_by_plan_pass_id",
                table: "fiction_lore_requirements",
                column: "created_by_plan_pass_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_lore_requirements_world_bible_entry_id",
                table: "fiction_lore_requirements",
                column: "world_bible_entry_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_lore_requirements_plan_slug",
                table: "fiction_lore_requirements",
                columns: new[] { "fiction_plan_id", "requirement_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_persona_obligations_fiction_character_id",
                table: "fiction_persona_obligations",
                column: "fiction_character_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_persona_obligations_persona_id",
                table: "fiction_persona_obligations",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_persona_obligations_plan_persona_slug",
                table: "fiction_persona_obligations",
                columns: new[] { "fiction_plan_id", "persona_id", "obligation_slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_backlog_plan_backlog_id",
                table: "fiction_plan_backlog",
                columns: new[] { "fiction_plan_id", "backlog_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_checkpoints_plan_phase",
                table: "fiction_plan_checkpoints",
                columns: new[] { "fiction_plan_id", "phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_plan_passes_plan_index",
                table: "fiction_plan_passes",
                columns: new[] { "fiction_plan_id", "pass_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plan_transcripts_fiction_chapter_blueprint_id",
                table: "fiction_plan_transcripts",
                column: "fiction_chapter_blueprint_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plan_transcripts_fiction_chapter_scene_id",
                table: "fiction_plan_transcripts",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_plan_transcripts_plan_created_at",
                table: "fiction_plan_transcripts",
                columns: new[] { "fiction_plan_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_plans_current_conversation_plan_id",
                table: "fiction_plans",
                column: "current_conversation_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_plans_project_name",
                table: "fiction_plans",
                columns: new[] { "fiction_project_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_story_metrics_fiction_chapter_scene_id",
                table: "fiction_story_metrics",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiction_story_metrics_plan_key_created_at",
                table: "fiction_story_metrics",
                columns: new[] { "fiction_plan_id", "metric_key", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_agent_id",
                table: "fiction_world_bible_entries",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_derived_from_entry_id",
                table: "fiction_world_bible_entries",
                column: "derived_from_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_fiction_chapter_scene_id",
                table: "fiction_world_bible_entries",
                column: "fiction_chapter_scene_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_fiction_chapter_scroll_id",
                table: "fiction_world_bible_entries",
                column: "fiction_chapter_scroll_id");

            migrationBuilder.CreateIndex(
                name: "IX_fiction_world_bible_entries_persona_id",
                table: "fiction_world_bible_entries",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_fiction_world_bible_entries_slug_version",
                table: "fiction_world_bible_entries",
                columns: new[] { "fiction_world_bible_id", "entry_slug", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_fiction_world_bibles_domain_branch",
                table: "fiction_world_bibles",
                columns: new[] { "fiction_plan_id", "domain", "branch_slug" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_instruction_set_items_instruction_id",
                table: "instruction_set_items",
                column: "instruction_id");

            migrationBuilder.CreateIndex(
                name: "ix_instruction_set_items_order",
                table: "instruction_set_items",
                columns: new[] { "instruction_set_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_instruction_sets_name",
                table: "instruction_sets",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_dimensions",
                table: "knowledge_embeddings",
                column: "dimensions");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_item_id",
                table: "knowledge_embeddings",
                column: "knowledge_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_model",
                table: "knowledge_embeddings",
                column: "model");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_provider",
                table: "knowledge_embeddings",
                column: "provider");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_embeddings_scope_principal",
                table: "knowledge_embeddings",
                columns: new[] { "scope_principal_type", "scope_principal_id" });

            migrationBuilder.CreateIndex(
                name: "ux_knowledge_embeddings_item_model_version_chunk",
                table: "knowledge_embeddings",
                columns: new[] { "knowledge_item_id", "model", "model_version", "chunk_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_items_timestamp",
                table: "knowledge_items",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_relations_pair",
                table: "knowledge_relations",
                columns: new[] { "from_item_id", "to_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relations_to_item_id",
                table: "knowledge_relations",
                column: "to_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_llm_global_defaults_is_active_priority",
                table: "llm_global_defaults",
                columns: new[] { "is_active", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_llm_global_defaults_model_id",
                table: "llm_global_defaults",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ux_models_provider_name",
                table: "models",
                columns: new[] { "provider_id", "name" },
                unique: true);

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
                name: "ix_persona_links_pair",
                table: "persona_links",
                columns: new[] { "from_persona_id", "to_persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_links_to_persona_id",
                table: "persona_links",
                column: "to_persona_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_persona_personas_to_persona_id",
                table: "persona_personas",
                column: "to_persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_persona_personas_pair",
                table: "persona_personas",
                columns: new[] { "from_persona_id", "to_persona_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_providers_name",
                table: "providers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_question_categories_key",
                table: "question_categories",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_questions_category",
                table: "questions",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_user_token",
                table: "refresh_tokens",
                columns: new[] { "user_id", "token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_system_variables_key",
                table: "system_variables",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tool_execution_logs_agent_id",
                table: "tool_execution_logs",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_tool_execution_logs_tool_time",
                table: "tool_execution_logs",
                columns: new[] { "tool_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_tool_parameters_tool_name_dir",
                table: "tool_parameters",
                columns: new[] { "tool_id", "name", "direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tool_provider_supports_model_id",
                table: "tool_provider_supports",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "IX_tool_provider_supports_provider_id",
                table: "tool_provider_supports",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ux_tool_provider_support_key",
                table: "tool_provider_supports",
                columns: new[] { "tool_id", "provider_id", "model_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tools_client_profile_id",
                table: "tools",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "ux_tools_name",
                table: "tools",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_personas_persona_id",
                table: "user_personas",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_personas_user_persona",
                table: "user_personas",
                columns: new[] { "user_id", "persona_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_primary_persona_id",
                table: "users",
                column: "primary_persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_username",
                table: "users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_tool_bindings");

            migrationBuilder.DropTable(
                name: "conversation_message_versions");

            migrationBuilder.DropTable(
                name: "conversation_participants");

            migrationBuilder.DropTable(
                name: "conversation_summaries");

            migrationBuilder.DropTable(
                name: "conversation_tasks");

            migrationBuilder.DropTable(
                name: "conversation_thoughts");

            migrationBuilder.DropTable(
                name: "conversation_workflow_states");

            migrationBuilder.DropTable(
                name: "data_sources");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "fiction_lore_requirements");

            migrationBuilder.DropTable(
                name: "fiction_persona_obligations");

            migrationBuilder.DropTable(
                name: "fiction_plan_backlog");

            migrationBuilder.DropTable(
                name: "fiction_plan_checkpoints");

            migrationBuilder.DropTable(
                name: "fiction_plan_transcripts");

            migrationBuilder.DropTable(
                name: "fiction_story_metrics");

            migrationBuilder.DropTable(
                name: "image_assets");

            migrationBuilder.DropTable(
                name: "instruction_set_items");

            migrationBuilder.DropTable(
                name: "knowledge_embeddings");

            migrationBuilder.DropTable(
                name: "knowledge_relations");

            migrationBuilder.DropTable(
                name: "llm_global_defaults");

            migrationBuilder.DropTable(
                name: "persona_dreams");

            migrationBuilder.DropTable(
                name: "persona_events");

            migrationBuilder.DropTable(
                name: "persona_links");

            migrationBuilder.DropTable(
                name: "persona_memories");

            migrationBuilder.DropTable(
                name: "persona_personas");

            migrationBuilder.DropTable(
                name: "planner_executions");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "system_variables");

            migrationBuilder.DropTable(
                name: "tool_execution_logs");

            migrationBuilder.DropTable(
                name: "tool_parameters");

            migrationBuilder.DropTable(
                name: "tool_provider_supports");

            migrationBuilder.DropTable(
                name: "user_personas");

            migrationBuilder.DropTable(
                name: "workflow_events");

            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "fiction_characters");

            migrationBuilder.DropTable(
                name: "image_styles");

            migrationBuilder.DropTable(
                name: "instructions");

            migrationBuilder.DropTable(
                name: "instruction_sets");

            migrationBuilder.DropTable(
                name: "knowledge_items");

            migrationBuilder.DropTable(
                name: "persona_event_types");

            migrationBuilder.DropTable(
                name: "persona_memory_types");

            migrationBuilder.DropTable(
                name: "question_categories");

            migrationBuilder.DropTable(
                name: "tools");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "fiction_world_bible_entries");

            migrationBuilder.DropTable(
                name: "fiction_world_bibles");

            migrationBuilder.DropTable(
                name: "fiction_chapter_scenes");

            migrationBuilder.DropTable(
                name: "fiction_chapter_sections");

            migrationBuilder.DropTable(
                name: "fiction_chapter_scrolls");

            migrationBuilder.DropTable(
                name: "fiction_chapter_blueprints");

            migrationBuilder.DropTable(
                name: "fiction_plan_passes");

            migrationBuilder.DropTable(
                name: "fiction_plans");

            migrationBuilder.DropTable(
                name: "conversation_plans");

            migrationBuilder.DropTable(
                name: "fiction_projects");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "client_profiles");

            migrationBuilder.DropTable(
                name: "personas");

            migrationBuilder.DropTable(
                name: "api_credentials");

            migrationBuilder.DropTable(
                name: "models");

            migrationBuilder.DropTable(
                name: "providers");
        }
    }
}
