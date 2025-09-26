using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class WipeConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dangerous: wipe all conversation-related data
            migrationBuilder.Sql(@"
                TRUNCATE TABLE conversation_message_versions RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_messages RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_participants RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_plans RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_tasks RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_summaries RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_thoughts RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversation_workflow_states RESTART IDENTITY CASCADE;
                TRUNCATE TABLE workflow_events RESTART IDENTITY CASCADE;
                TRUNCATE TABLE conversations RESTART IDENTITY CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op rollback (data destructive)
        }
    }
}
