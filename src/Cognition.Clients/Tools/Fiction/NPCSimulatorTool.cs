using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Tools.Fiction;

public class NPCSimulatorTool : ITool
{
    public string Name => "NPCSimulatorTool";
    public string ClassPath => typeof(NPCSimulatorTool).FullName! + ", " + typeof(NPCSimulatorTool).Assembly.GetName().Name;

    // Args: projectId, characterAssetId, outlineNodeId?, goal?, constraints?, stakes?
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;
        var projectId = GetGuid(args, "projectId", required: true)!.Value;
        var characterAssetId = GetGuid(args, "characterAssetId", required: true)!.Value;
        var outlineNodeId = GetGuid(args, "outlineNodeId");
        var goal = (args.TryGetValue("goal", out var gv) ? gv?.ToString() : null) ?? "pursue objective";
        var constraints = (args.TryGetValue("constraints", out var cv) ? cv?.ToString() : null);
        var stakes = (args.TryGetValue("stakes", out var sv) ? sv?.ToString() : null);

        var asset = await db.WorldAssets.FirstAsync(a => a.Id == characterAssetId && a.Type == WorldAssetType.Character, ctx.Ct);
        if (asset.FictionProjectId != projectId) throw new ArgumentException("characterAssetId does not belong to project");
        if (outlineNodeId.HasValue)
        {
            var node = await db.OutlineNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == outlineNodeId.Value, ctx.Ct);
            if (node == null || node.FictionProjectId != projectId) throw new ArgumentException("outlineNodeId does not belong to project");
        }

        // Minimal simulation: write a timeline event expressing the intent
        var details = new Dictionary<string, object?>
        {
            ["goal"] = goal,
            ["constraints"] = constraints,
            ["stakes"] = stakes
        };
        var ev = new TimelineEvent
        {
            FictionProjectId = projectId,
            OutlineNodeId = outlineNodeId,
            InWorldDate = null,
            Index = null,
            Title = $"{asset.Name} acts",
            Description = System.Text.Json.JsonSerializer.Serialize(details),
            CreatedAtUtc = now
        };
        db.TimelineEvents.Add(ev);
        await db.SaveChangesAsync(ctx.Ct);

        db.Add(new TimelineEventAsset { TimelineEventId = ev.Id, WorldAssetId = asset.Id, CreatedAtUtc = now });
        await db.SaveChangesAsync(ctx.Ct);

        await LogThoughtAsync(db, ctx, $"Simulated NPC '{asset.Name}' action: {goal}");
        LogEvent(db, ctx, "ToolExecutionCompleted", new { tool = Name, args, timelineEventId = ev.Id, details, status = "Success" });
        await db.SaveChangesAsync(ctx.Ct);

        return new { timelineEventId = ev.Id };
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
