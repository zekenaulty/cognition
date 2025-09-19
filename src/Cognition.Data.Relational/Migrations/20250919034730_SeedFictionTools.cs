using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cognition.Data.Relational.Migrations
{
    /// <inheritdoc />
    public partial class SeedFictionTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Ensure OpenAI + gpt-4o exist
INSERT INTO providers (id, name, display_name, base_url, is_active, created_at_utc)
SELECT gen_random_uuid(), 'OpenAI', 'OpenAI', 'https://api.openai.com', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM providers WHERE lower(name)='openai');

INSERT INTO models (id, provider_id, name, created_at_utc)
SELECT gen_random_uuid(), p.id, 'gpt-4o', NOW()
FROM providers p
WHERE lower(p.name)='openai'
  AND NOT EXISTS (SELECT 1 FROM models m WHERE m.provider_id=p.id AND lower(m.name)='gpt-4o');

-- Seed fiction tools (idempotent)
INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'WorldbuilderTool', 'Cognition.Clients.Tools.Fiction.WorldbuilderTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='WorldbuilderTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'LoreKeeperTool', 'Cognition.Clients.Tools.Fiction.LoreKeeperTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='LoreKeeperTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'OutlinerTool', 'Cognition.Clients.Tools.Fiction.OutlinerTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='OutlinerTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'SceneDraftTool', 'Cognition.Clients.Tools.Fiction.SceneDraftTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='SceneDraftTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'FactCheckerTool', 'Cognition.Clients.Tools.Fiction.FactCheckerTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='FactCheckerTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'RewriterTool', 'Cognition.Clients.Tools.Fiction.RewriterTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='RewriterTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'NPCDesignerTool', 'Cognition.Clients.Tools.Fiction.NPCDesignerTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='NPCDesignerTool');

INSERT INTO tools (id, name, class_path, is_active, created_at_utc)
SELECT gen_random_uuid(), 'NPCSimulatorTool', 'Cognition.Clients.Tools.Fiction.NPCSimulatorTool, Cognition.Clients', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM tools WHERE name='NPCSimulatorTool');

-- Parameters for WorldbuilderTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'type','string','Input', true, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='type');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'name','string','Input', true, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='name');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'personaId','guid','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='personaId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'description','string','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='description');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'content','object','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='content');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'autoSeedGlossary','bool','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='autoSeedGlossary');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'providerId','guid','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='providerId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'modelId','guid','Input', false, NOW() FROM tools t WHERE t.name='WorldbuilderTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='modelId');

-- Parameters for LoreKeeperTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'styleRules','object','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='styleRules');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'terms','object','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='terms');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'canon','object','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='canon');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'extractText','string','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='extractText');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'providerId','guid','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='providerId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'modelId','guid','Input', false, NOW() FROM tools t WHERE t.name='LoreKeeperTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='modelId');

-- Parameters for OutlinerTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'nodeType','string','Input', true, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='nodeType');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'title','string','Input', true, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='title');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'parentId','guid','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='parentId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'plotArcId','guid','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='plotArcId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'sequenceIndex','int','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='sequenceIndex');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'beats','object','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='beats');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'providerId','guid','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='providerId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'modelId','guid','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='modelId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'autoBeats','bool','Input', false, NOW() FROM tools t WHERE t.name='OutlinerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='autoBeats');

-- Parameters for SceneDraftTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'outlineNodeId','guid','Input', false, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='outlineNodeId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'draftSegmentId','guid','Input', false, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='draftSegmentId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'prompt','string','Input', false, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='prompt');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'providerId','guid','Input', false, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='providerId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'modelId','guid','Input', false, NOW() FROM tools t WHERE t.name='SceneDraftTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='modelId');

-- Parameters for FactCheckerTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='FactCheckerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'draftSegmentVersionId','guid','Input', true, NOW() FROM tools t WHERE t.name='FactCheckerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='draftSegmentVersionId');

-- Parameters for RewriterTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'draftSegmentId','guid','Input', false, NOW() FROM tools t WHERE t.name='RewriterTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='draftSegmentId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'draftSegmentVersionId','guid','Input', false, NOW() FROM tools t WHERE t.name='RewriterTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='draftSegmentVersionId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'mode','string','Input', false, NOW() FROM tools t WHERE t.name='RewriterTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='mode');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'providerId','guid','Input', false, NOW() FROM tools t WHERE t.name='RewriterTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='providerId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'modelId','guid','Input', false, NOW() FROM tools t WHERE t.name='RewriterTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='modelId');

-- Parameters for NPCDesignerTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='NPCDesignerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'personaId','guid','Input', true, NOW() FROM tools t WHERE t.name='NPCDesignerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='personaId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'name','string','Input', false, NOW() FROM tools t WHERE t.name='NPCDesignerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='name');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'content','object','Input', false, NOW() FROM tools t WHERE t.name='NPCDesignerTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='content');

-- Parameters for NPCSimulatorTool
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'projectId','guid','Input', true, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='projectId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'characterAssetId','guid','Input', true, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='characterAssetId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'outlineNodeId','guid','Input', false, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='outlineNodeId');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'goal','string','Input', false, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='goal');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'constraints','string','Input', false, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='constraints');
INSERT INTO tool_parameters (id, tool_id, name, type, direction, required, created_at_utc)
SELECT gen_random_uuid(), t.id, 'stakes','string','Input', false, NOW() FROM tools t WHERE t.name='NPCSimulatorTool' AND NOT EXISTS (SELECT 1 FROM tool_parameters p WHERE p.tool_id=t.id AND p.name='stakes');

-- Default client profile to tools where missing (OpenAI gpt-4o)
UPDATE tools SET client_profile_id = cp.id
FROM client_profiles cp
JOIN providers p ON p.id=cp.provider_id
JOIN models m ON m.id=cp.model_id
WHERE lower(p.name)='openai' AND lower(m.name)='gpt-4o' AND tools.client_profile_id IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM tool_parameters WHERE tool_id IN (SELECT id FROM tools WHERE name IN ('WorldbuilderTool','LoreKeeperTool','OutlinerTool','SceneDraftTool','FactCheckerTool','RewriterTool','NPCDesignerTool','NPCSimulatorTool'));
DELETE FROM tools WHERE name IN ('WorldbuilderTool','LoreKeeperTool','OutlinerTool','SceneDraftTool','FactCheckerTool','RewriterTool','NPCDesignerTool','NPCSimulatorTool');");
        }
    }
}
