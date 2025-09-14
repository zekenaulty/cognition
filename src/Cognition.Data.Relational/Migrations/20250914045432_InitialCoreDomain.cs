using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    data_source_type = table.Column<string>(type: "text", nullable: false),
                    collection_name = table.Column<string>(type: "text", nullable: false),
                    config = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_sources", x => x.id);
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
                    embedding_vector = table.Column<float[]>(type: "real[]", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    known_personas = table.Column<Guid[]>(type: "uuid[]", nullable: true),
                    properties = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    prompt_type = table.Column<string>(type: "text", nullable: false),
                    template = table.Column<string>(type: "text", nullable: false),
                    tokens = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    example = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    description = table.Column<string>(type: "text", nullable: true)
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
                    value = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_variables", x => x.id);
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
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instruction_set_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instruction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
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
                name: "knowledge_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "text", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
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
                name: "conversation_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        name: "fk_conversation_messages_from_persona",
                        column: x => x.from_persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "conversation_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    by_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    references_persona_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "persona_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
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
                name: "api_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    last_used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true)
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
                    metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true)
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
                    difficulty = table.Column<int>(type: "integer", nullable: true)
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
                name: "tool_parameters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    options = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
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
                name: "client_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "text", nullable: true),
                    base_url_override = table.Column<string>(type: "text", nullable: true),
                    max_tokens = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: false),
                    top_p = table.Column<double>(type: "double precision", nullable: false),
                    presence_penalty = table.Column<double>(type: "double precision", nullable: false),
                    frequency_penalty = table.Column<double>(type: "double precision", nullable: false),
                    stream = table.Column<bool>(type: "boolean", nullable: false),
                    logging_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_profiles", x => x.id);
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
                name: "tool_provider_supports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    support_level = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
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
                    state = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
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
                name: "agent_tool_bindings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope_type = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tool_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    config = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    request = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    response = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
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
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ux_api_credentials_provider_keyref",
                table: "api_credentials",
                columns: new[] { "provider_id", "key_ref" },
                unique: true);

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
                name: "ix_conversation_messages_conversation_ts",
                table: "conversation_messages",
                columns: new[] { "conversation_id", "timestamp" });

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
                name: "ux_data_sources_name_type",
                table: "data_sources",
                columns: new[] { "name", "data_source_type" },
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
                name: "ux_models_provider_name",
                table: "models",
                columns: new[] { "provider_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_persona_links_pair",
                table: "persona_links",
                columns: new[] { "from_persona_id", "to_persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_persona_links_to_persona_id",
                table: "persona_links",
                column: "to_persona_id");

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
                name: "ux_tools_name",
                table: "tools",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_tool_bindings");

            migrationBuilder.DropTable(
                name: "api_credentials");

            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "conversation_participants");

            migrationBuilder.DropTable(
                name: "conversation_summaries");

            migrationBuilder.DropTable(
                name: "data_sources");

            migrationBuilder.DropTable(
                name: "instruction_set_items");

            migrationBuilder.DropTable(
                name: "knowledge_relations");

            migrationBuilder.DropTable(
                name: "persona_links");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "system_variables");

            migrationBuilder.DropTable(
                name: "tool_execution_logs");

            migrationBuilder.DropTable(
                name: "tool_parameters");

            migrationBuilder.DropTable(
                name: "tool_provider_supports");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "instructions");

            migrationBuilder.DropTable(
                name: "instruction_sets");

            migrationBuilder.DropTable(
                name: "knowledge_items");

            migrationBuilder.DropTable(
                name: "question_categories");

            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "tools");

            migrationBuilder.DropTable(
                name: "client_profiles");

            migrationBuilder.DropTable(
                name: "personas");

            migrationBuilder.DropTable(
                name: "models");

            migrationBuilder.DropTable(
                name: "providers");
        }
    }
}
