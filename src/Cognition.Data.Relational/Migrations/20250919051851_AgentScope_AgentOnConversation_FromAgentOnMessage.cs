using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AgentScope_AgentOnConversation_FromAgentOnMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Ensure unique index on agents.persona_id
            migrationBuilder.DropIndex(
                name: "IX_agents_persona_id",
                table: "agents");

            migrationBuilder.CreateIndex(
                name: "IX_agents_persona_id",
                table: "agents",
                column: "persona_id",
                unique: true);

            // 2) Add columns as NULLABLE initially for backfill
            migrationBuilder.AddColumn<Guid>(
                name: "agent_id",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "from_agent_id",
                table: "conversation_messages",
                type: "uuid",
                nullable: true);

            // 3) Create indexes (before FKs)
            migrationBuilder.CreateIndex(
                name: "IX_conversations_agent_id",
                table: "conversations",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_from_agent_id",
                table: "conversation_messages",
                column: "from_agent_id");

            // 4) Create missing Agents for Personas (id generation via pgcrypto/uuid-ossp)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
            migrationBuilder.Sql(@"
                INSERT INTO agents (id, persona_id, version, role_play, prefix, suffix, client_profile_id, state, created_at_utc, updated_at_utc)
                SELECT COALESCE(gen_random_uuid(), uuid_generate_v4()), p.id, COALESCE(gen_random_uuid(), uuid_generate_v4()), FALSE, NULL, NULL, NULL, '{}'::jsonb, NOW(), NULL
                FROM personas p
                LEFT JOIN agents a ON a.persona_id = p.id
                WHERE a.id IS NULL;
            ");

            // 5) Backfill message from_agent_id from from_persona_id
            migrationBuilder.Sql(@"
                UPDATE conversation_messages m
                SET from_agent_id = a.id
                FROM agents a
                WHERE a.persona_id = m.from_persona_id;
            ");

            // 6) Backfill conversation agent_id by earliest message persona, then participant fallback
            migrationBuilder.Sql(@"
                -- Earliest message persona per conversation
                WITH first_msg AS (
                    SELECT DISTINCT ON (m.conversation_id) m.conversation_id, m.from_persona_id
                    FROM conversation_messages m
                    ORDER BY m.conversation_id, m.timestamp ASC
                )
                UPDATE conversations c
                SET agent_id = a.id
                FROM first_msg fm
                JOIN agents a ON a.persona_id = fm.from_persona_id
                WHERE c.id = fm.conversation_id AND c.agent_id IS NULL;
            ");

            migrationBuilder.Sql(@"
                -- Participant fallback by earliest join
                WITH first_participant AS (
                    SELECT DISTINCT ON (cp.conversation_id) cp.conversation_id, cp.persona_id
                    FROM conversation_participants cp
                    ORDER BY cp.conversation_id, cp.joined_at_utc ASC
                )
                UPDATE conversations c
                SET agent_id = a.id
                FROM first_participant fp
                JOIN agents a ON a.persona_id = fp.persona_id
                WHERE c.id = fp.conversation_id AND c.agent_id IS NULL;
            ");

            // 7) Enforce NOT NULL and add FKs now that data is populated
            migrationBuilder.Sql(@"ALTER TABLE conversation_messages ALTER COLUMN from_agent_id SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE conversations ALTER COLUMN agent_id SET NOT NULL;");

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_messages_from_agent",
                table: "conversation_messages",
                column: "from_agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_agents",
                table: "conversations",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversation_messages_from_agent",
                table: "conversation_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_conversations_agents",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_agent_id",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversation_messages_from_agent_id",
                table: "conversation_messages");

            migrationBuilder.DropIndex(
                name: "IX_agents_persona_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "agent_id",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "from_agent_id",
                table: "conversation_messages");

            migrationBuilder.CreateIndex(
                name: "IX_agents_persona_id",
                table: "agents",
                column: "persona_id");
        }
    }
}
