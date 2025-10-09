using Microsoft.EntityFrameworkCore.Migrations;

namespace Cognition.Data.Relational.Migrations
{
    [Migration("20250918093500_SeedInitialTools")]
    public partial class SeedInitialTools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
DECLARE openai_id uuid; gpt4o_id uuid; default_profile_id uuid;
BEGIN
  -- Ensure provider/model/profile exist
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

  SELECT id INTO default_profile_id FROM client_profiles WHERE provider_id = openai_id AND model_id = gpt4o_id LIMIT 1;
  IF default_profile_id IS NULL THEN
    INSERT INTO client_profiles (id, name, provider_id, model_id, max_tokens, temperature, top_p, presence_penalty, frequency_penalty, stream, logging_enabled, is_active, created_at_utc)
    VALUES (gen_random_uuid(), 'Default (OpenAI gpt-4o)', openai_id, gpt4o_id, 8192, 0.8, 0.95, 0.5, 0.1, true, false, true, NOW())
    RETURNING id INTO default_profile_id;
  END IF;

  -- Helper to insert a tool if missing
  PERFORM 1;
END $$;");

            // Seed each tool with parameters and provider support
            migrationBuilder.Sql(SeedToolSql(
                name: "Knowledge Query",
                classPath: "Cognition.Clients.Tools.KnowledgeQueryTool, Cognition.Clients",
                parameters: new[] {
                    Param("query","string", true),
                    Param("category","string", false)
                }));

            migrationBuilder.Sql(SeedToolSql(
                name: "Memory Write",
                classPath: "Cognition.Clients.Tools.MemoryWriteTool, Cognition.Clients",
                parameters: new[] {
                    Param("text","string", true)
                }));

            migrationBuilder.Sql(SeedToolSql(
                name: "Text Transform",
                classPath: "Cognition.Clients.Tools.TextTransformTool, Cognition.Clients",
                parameters: new[] {
                    Param("text","string", true),
                    Param("mode","string", false)
                }));








        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded tools and related parameters/provider supports
            var toolNames = new[] {
                "Knowledge Query","Memory Write","Text Transform"
            };
            foreach (var name in toolNames)
            {
                migrationBuilder.Sql($@"DO $$
DECLARE tid uuid;
BEGIN
  SELECT id INTO tid FROM tools WHERE name = '{name.Replace("'","''")}' LIMIT 1;
  IF tid IS NOT NULL THEN
    DELETE FROM tool_provider_supports WHERE tool_id = tid;
    DELETE FROM tool_parameters WHERE tool_id = tid;
    DELETE FROM tool_execution_logs WHERE tool_id = tid;
    DELETE FROM tools WHERE id = tid;
  END IF;
END $$;");
            }
        }

        private static string Param(string name, string type, bool required)
            => $"SELECT '{{\"name\":\"{name}\",\"type\":\"{type}\",\"required\":{(required ? "true" : "false")}}}'";

        private static string SeedToolSql(string name, string classPath, string[] parameters)
        {
            // parameters is an array of SELECT 'json' rows; we will iterate in plpgsql
            var paramsTable = string.Join(" UNION ALL ", parameters);
            return $@"DO $$
DECLARE t_id uuid; openai_id uuid; gpt4o_id uuid; default_profile_id uuid;
BEGIN
  SELECT id INTO openai_id FROM providers WHERE lower(name) = 'openai' LIMIT 1;
  SELECT id INTO gpt4o_id FROM models WHERE lower(name) = 'gpt-4o' AND provider_id = openai_id LIMIT 1;
  SELECT id INTO default_profile_id FROM client_profiles WHERE provider_id = openai_id AND model_id = gpt4o_id LIMIT 1;

  IF NOT EXISTS (SELECT 1 FROM tools WHERE name = '{name.Replace("'","''")}') THEN
    INSERT INTO tools (id, name, class_path, description, is_active, client_profile_id, created_at_utc)
    VALUES (gen_random_uuid(), '{name.Replace("'","''")}', '{classPath.Replace("'","''")}', NULL, true, default_profile_id, NOW());
  END IF;

  SELECT id INTO t_id FROM tools WHERE name = '{name.Replace("'","''")}' LIMIT 1;

  -- Parameters
  FOR r IN ({paramsTable}) LOOP
    IF NOT EXISTS (SELECT 1 FROM tool_parameters WHERE tool_id = t_id AND name = (r::json->>'name')) THEN
      INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
      VALUES (gen_random_uuid(), t_id, (r::json->>'name'), (r::json->>'type'), 'Input', ((r::json->>'required')::boolean), NOW());
    END IF;
  END LOOP;

  -- Provider support (OpenAI gpt-4o full)
  IF NOT EXISTS (SELECT 1 FROM tool_provider_supports WHERE tool_id = t_id AND provider_id = openai_id AND (model_id IS NULL OR model_id = gpt4o_id)) THEN
    INSERT INTO tool_provider_supports (id, tool_id, provider_id, model_id, support_level, created_at_utc)
    VALUES (gen_random_uuid(), t_id, openai_id, gpt4o_id, 'Full', NOW());
  END IF;
END $$;";
        }
    }
}

