using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_workflow_states");
        }
    }
}
