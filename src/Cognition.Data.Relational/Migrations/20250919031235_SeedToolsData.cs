using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedToolsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO providers (id, name, display_name, base_url, is_active, created_at_utc)
SELECT gen_random_uuid(), 'OpenAI', 'OpenAI', 'https://api.openai.com', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM providers WHERE lower(name)='openai');

INSERT INTO models (id, provider_id, name, created_at_utc)
SELECT gen_random_uuid(), p.id, 'gpt-4o', NOW()
FROM providers p
WHERE lower(p.name)='openai'
  AND NOT EXISTS (SELECT 1 FROM models m WHERE m.provider_id=p.id AND lower(m.name)='gpt-4o');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'Knowledge Query', 'Cognition.Clients.Tools.KnowledgeQueryTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='Knowledge Query');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'Memory Write', 'Cognition.Clients.Tools.MemoryWriteTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='Memory Write');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'Text Transform', 'Cognition.Clients.tools.TextTransformTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='Text Transform');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM tools WHERE name IN ('Knowledge Query','Memory Write','Text Transform');");
        }
    }
}
