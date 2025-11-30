using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AgentCutoverPersonaRelax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversation_messages_from_persona",
                table: "conversation_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_conversation_plans_persona",
                table: "conversation_plans");

            migrationBuilder.AlterColumn<Guid>(
                name: "persona_id",
                table: "conversation_plans",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "from_persona_id",
                table: "conversation_messages",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql("""
update conversation_messages m
set from_persona_id = a.persona_id
from agents a
where m.from_persona_id is null and m.from_agent_id = a.id;

update conversation_plans cp
set persona_id = a.persona_id
from conversations c
join agents a on c.agent_id = a.id
where cp.persona_id is null and cp.conversation_id = c.id;
""");

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_messages_from_persona",
                table: "conversation_messages",
                column: "from_persona_id",
                principalTable: "personas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_plans_persona",
                table: "conversation_plans",
                column: "persona_id",
                principalTable: "personas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversation_messages_from_persona",
                table: "conversation_messages");

            migrationBuilder.DropForeignKey(
                name: "fk_conversation_plans_persona",
                table: "conversation_plans");

            migrationBuilder.AlterColumn<Guid>(
                name: "persona_id",
                table: "conversation_plans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "from_persona_id",
                table: "conversation_messages",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_messages_from_persona",
                table: "conversation_messages",
                column: "from_persona_id",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_plans_persona",
                table: "conversation_plans",
                column: "persona_id",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
