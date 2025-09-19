using Microsoft.EntityFrameworkCore.Migrations;

namespace Cognition.Data.Relational.Migrations
{
    public partial class ToolClientProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_profile_id",
                table: "tools",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tools_client_profile_id",
                table: "tools",
                column: "client_profile_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tools_client_profiles",
                table: "tools",
                column: "client_profile_id",
                principalTable: "client_profiles",
                principalColumn: "id");

            // Seed default OpenAI/gpt-4o provider/model and a default client profile; set as default for tools without profile
            migrationBuilder.Sql(@"DO $$
DECLARE openai_id uuid; gpt4o_id uuid; profile_id uuid;
BEGIN
    SELECT id INTO openai_id FROM providers WHERE lower(name) = 'openai' LIMIT 1;
    IF openai_id IS NULL THEN
        INSERT INTO providers (id, name, display_name, base_url, is_active, created_at_utc)
        VALUES (gen_random_uuid(), 'OpenAI', 'OpenAI', 'https://api.openai.com', true, NOW()) RETURNING id INTO openai_id;
    END IF;

    SELECT id INTO gpt4o_id FROM models WHERE lower(name) = 'gpt-4o' AND provider_id = openai_id LIMIT 1;
    IF gpt4o_id IS NULL THEN
        INSERT INTO models (id, provider_id, name, created_at_utc)
        VALUES (gen_random_uuid(), openai_id, 'gpt-4o', NOW()) RETURNING id INTO gpt4o_id;
    END IF;

    SELECT id INTO profile_id FROM client_profiles WHERE provider_id = openai_id AND model_id = gpt4o_id LIMIT 1;
    IF profile_id IS NULL THEN
        INSERT INTO client_profiles (id, name, provider_id, model_id, max_tokens, temperature, top_p, presence_penalty, frequency_penalty, stream, logging_enabled, created_at_utc)
        VALUES (gen_random_uuid(), 'Default (OpenAI gpt-4o)', openai_id, gpt4o_id, 8192, 0.8, 0.95, 0.5, 0.1, true, false, NOW())
        RETURNING id INTO profile_id;
    END IF;

    UPDATE tools SET client_profile_id = profile_id WHERE client_profile_id IS NULL;
END $$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tools_client_profiles",
                table: "tools");

            migrationBuilder.DropIndex(
                name: "ix_tools_client_profile_id",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "client_profile_id",
                table: "tools");
        }
    }
}

