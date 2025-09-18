using Microsoft.EntityFrameworkCore.Migrations;

namespace Cognition.Data.Relational.Migrations
{
    public partial class AddDefaultAssistantPersona : Migration
    {
        private const string DefaultId = "11111111-1111-1111-1111-111111111111";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM personas WHERE owned_by = 'System' AND persona_type = 'Assistant'
    ) THEN
        INSERT INTO personas (id, name, nickname, role, is_public, persona_type, owned_by, created_at_utc)
        VALUES ('11111111-1111-1111-1111-111111111111', 'Cognition Assistant', 'Assistant', 'Default AI Assistant', true, 'Assistant', 'System', NOW());
    END IF;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM personas WHERE id = '{DefaultId}';");
        }
    }
}

