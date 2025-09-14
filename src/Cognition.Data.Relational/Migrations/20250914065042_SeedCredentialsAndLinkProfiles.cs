using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedCredentialsAndLinkProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
DECLARE openai_id uuid; gemini_id uuid; cp_id uuid; m_id uuid; cred_id uuid;
BEGIN
    SELECT id INTO openai_id FROM providers WHERE name = 'OpenAI';
    IF openai_id IS NOT NULL THEN
        INSERT INTO api_credentials (id, provider_id, key_ref, last_used_at_utc, is_valid, notes)
        VALUES (gen_random_uuid(), openai_id, 'OPENAI_KEY', NULL, TRUE, 'Seeded from reference .env')
        ON CONFLICT DO NOTHING;
        SELECT id INTO cp_id FROM client_profiles WHERE name = 'Default OpenAI';
        IF cp_id IS NULL THEN
            SELECT id INTO m_id FROM models WHERE provider_id = openai_id AND name = 'gpt-4o';
            INSERT INTO client_profiles (id, name, provider_id, model_id, user_name, base_url_override, max_tokens, temperature, top_p, presence_penalty, frequency_penalty, stream, logging_enabled, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), 'Default OpenAI', openai_id, m_id, NULL, NULL, 8192, 0.7, 0.95, 0.2, 0.1, TRUE, FALSE, NOW(), NULL);
            SELECT id INTO cp_id FROM client_profiles WHERE name = 'Default OpenAI';
        END IF;
        SELECT id INTO cred_id FROM api_credentials WHERE provider_id = openai_id AND key_ref = 'OPENAI_KEY' LIMIT 1;
        UPDATE client_profiles SET api_credential_id = cred_id WHERE id = cp_id AND api_credential_id IS DISTINCT FROM cred_id;
    END IF;

    SELECT id INTO gemini_id FROM providers WHERE name = 'Gemini';
    IF gemini_id IS NOT NULL THEN
        INSERT INTO api_credentials (id, provider_id, key_ref, last_used_at_utc, is_valid, notes)
        VALUES (gen_random_uuid(), gemini_id, 'GOOGLE_API_KEY', NULL, TRUE, 'Seeded from reference .env')
        ON CONFLICT DO NOTHING;
        SELECT id INTO cp_id FROM client_profiles WHERE name = 'Default Gemini';
        IF cp_id IS NULL THEN
            SELECT id INTO m_id FROM models WHERE provider_id = gemini_id AND (name = 'gemini-2.5-flash' OR name = 'gemini-2.0-flash') LIMIT 1;
            INSERT INTO client_profiles (id, name, provider_id, model_id, user_name, base_url_override, max_tokens, temperature, top_p, presence_penalty, frequency_penalty, stream, logging_enabled, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), 'Default Gemini', gemini_id, m_id, NULL, NULL, 8192, 0.7, 0.95, 0.0, 0.0, TRUE, FALSE, NOW(), NULL);
            SELECT id INTO cp_id FROM client_profiles WHERE name = 'Default Gemini';
        END IF;
        SELECT id INTO cred_id FROM api_credentials WHERE provider_id = gemini_id AND key_ref = 'GOOGLE_API_KEY' LIMIT 1;
        UPDATE client_profiles SET api_credential_id = cred_id WHERE id = cp_id AND api_credential_id IS DISTINCT FROM cred_id;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE client_profiles SET api_credential_id = NULL WHERE name IN ('Default OpenAI','Default Gemini');");
        }
    }
}

