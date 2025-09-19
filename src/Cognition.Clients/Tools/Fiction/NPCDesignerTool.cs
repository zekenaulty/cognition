using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Tools.Fiction;

public class NPCDesignerTool : ITool
{
    public string Name => "NPCDesignerTool";
    public string ClassPath => typeof(NPCDesignerTool).FullName! + ", " + typeof(NPCDesignerTool).Assembly.GetName().Name;

    // Args: projectId, personaId, name, content?
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;
        var projectId = GetGuid(args, "projectId", required: true)!.Value;
        var personaId = GetGuid(args, "personaId", required: true)!.Value;
        var name = (args.TryGetValue("name", out var nv) ? nv?.ToString() : null) ?? "NPC";
        var content = args.TryGetValue("content", out var c) && c is Dictionary<string, object?> d ? d : new Dictionary<string, object?>();

        var project = await db.FictionProjects.FirstAsync(p => p.Id == projectId, ctx.Ct);

        var asset = new WorldAsset
        {
            FictionProjectId = project.Id,
            Type = WorldAssetType.Character,
            Name = name,
            PersonaId = personaId,
            ActiveVersionIndex = 0,
            CreatedAtUtc = now
        };
        db.WorldAssets.Add(asset);
        await db.SaveChangesAsync(ctx.Ct);

        var wav = new WorldAssetVersion
        {
            WorldAssetId = asset.Id,
            VersionIndex = 0,
            Content = content,
            CreatedAtUtc = now
        };
        db.WorldAssetVersions.Add(wav);
        await db.SaveChangesAsync(ctx.Ct);

        await LogThoughtAsync(db, ctx, $"NPC '{name}' linked to Persona {personaId}");
        LogEvent(db, ctx, "ToolExecutionCompleted", new { tool = Name, args, outputVersionIds = new[] { wav.Id }, status = "Success" });
        await db.SaveChangesAsync(ctx.Ct);

        return new { assetId = asset.Id, versionId = wav.Id };
    }

    private static Guid? GetGuid(IDictionary<string, object?> args, string key, bool required = false)
    {
        if (!args.TryGetValue(key, out var v) || v is null)
        {
            if (required) throw new ArgumentException($"Missing required '{key}'");
            return null;
        }
        return v is Guid g ? g : Guid.Parse(v.ToString()!);
    }

    private static async Task LogThoughtAsync(CognitionDbContext db, ToolContext ctx, string thought)
    {
        if (ctx.ConversationId is null || ctx.PersonaId is null) return;
        db.ConversationThoughts.Add(new ConversationThought
        {
            ConversationId = ctx.ConversationId.Value,
            PersonaId = ctx.PersonaId.Value,
            Thought = thought,
            StepNumber = 0,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ctx.Ct);
    }

    private static void LogEvent(CognitionDbContext db, ToolContext ctx, string kind, object payload)
    {
        if (ctx.ConversationId is null) return;
        db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = ctx.ConversationId.Value,
            Kind = kind,
            Payload = JObject.FromObject(payload),
            Timestamp = DateTime.UtcNow
        });
    }
}
