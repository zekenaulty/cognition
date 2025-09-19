using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using Cognition.Clients.LLM;
using System.Text.Json;

namespace Cognition.Clients.Tools.Fiction;

public class OutlinerTool : ITool
{
    public string Name => "OutlinerTool";
    public string ClassPath => typeof(OutlinerTool).FullName! + ", " + typeof(OutlinerTool).Assembly.GetName().Name;

    // Args: projectId (Guid), nodeType (Act/Part/Chapter/Scene), title, parentId?, plotArcId?, beats? (dict), seq?, providerId?, modelId?, autoBeats?
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;

        var projectId = GetRequiredGuid(args, "projectId");
        var typeStr = GetString(args, "nodeType", required: true);
        var title = GetString(args, "title", required: true);
        var parentId = GetGuid(args, "parentId");
        var plotArcId = GetGuid(args, "plotArcId");
        var seq = GetInt(args, "sequenceIndex") ?? 0;
        var beats = args.TryGetValue("beats", out var b) && b is Dictionary<string, object?> bd ? bd : new Dictionary<string, object?>();
        var providerId = GetGuid(args, "providerId");
        var modelId = GetGuid(args, "modelId");
        var autoBeats = GetBool(args, "autoBeats") ?? true;

        if (!Enum.TryParse<OutlineNodeType>(typeStr, true, out var nodeType))
            throw new ArgumentException($"Unknown OutlineNodeType: {typeStr}");

        // Cross-project guards for parent/plotArc
        if (parentId.HasValue)
        {
            var parent = await db.OutlineNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == parentId.Value, ctx.Ct);
            if (parent == null || parent.FictionProjectId != projectId) throw new ArgumentException("parentId does not belong to project");
        }
        if (plotArcId.HasValue)
        {
            var arc = await db.PlotArcs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == plotArcId.Value, ctx.Ct);
            if (arc == null || arc.FictionProjectId != projectId) throw new ArgumentException("plotArcId does not belong to project");
        }

        var node = new OutlineNode
        {
            FictionProjectId = projectId,
            PlotArcId = plotArcId,
            ParentId = parentId,
            Type = nodeType,
            Title = title!,
            SequenceIndex = seq,
            ActiveVersionIndex = 0,
            CreatedAtUtc = now
        };
        await using var tx = await db.Database.BeginTransactionAsync(ctx.Ct);
        db.OutlineNodes.Add(node);
        await db.SaveChangesAsync(ctx.Ct);

        if (autoBeats && beats.Count == 0)
        {
            try
            {
                var style = await db.StyleGuides.AsNoTracking().FirstOrDefaultAsync(g => g.FictionProjectId == projectId, ctx.Ct);
                var resolver = ctx.Services.GetRequiredService<ILLMProviderResolver>();
                var (client, _, _, _, _) = await resolver.ResolveAsync(providerId, modelId, ctx.Ct);
                var prompt = BuildBeatsPrompt(title!, nodeType.ToString(), style?.Rules);
                var json = await client.GenerateAsync(prompt, track: false);
                if (!string.IsNullOrWhiteSpace(json) && TryParseBeats(json, out var genBeats)) beats = genBeats;
            }
            catch
            {
                db.Annotations.Add(new Annotation
                {
                    FictionProjectId = projectId,
                    TargetType = nameof(OutlineNode),
                    TargetId = node.Id,
                    Type = AnnotationType.StyleViolation,
                    Severity = AnnotationSeverity.Warning,
                    Message = "Auto beats generation failed or returned invalid JSON",
                    Details = "LLM parse failure",
                    CreatedAtUtc = now
                });
            }
        }

        var version = new OutlineNodeVersion
        {
            OutlineNodeId = node.Id,
            VersionIndex = 0,
            Beats = beats,
            Status = "Draft",
            CreatedAtUtc = now
        };
        db.OutlineNodeVersions.Add(version);
        await db.SaveChangesAsync(ctx.Ct);
        await tx.CommitAsync(ctx.Ct);

        await LogThoughtAsync(db, ctx, $"Created outline {nodeType} '{title}' with v0.");
        LogEvent(db, ctx, "ToolExecutionCompleted", new
        {
            tool = Name,
            args,
            inputVersionIds = Array.Empty<Guid>(),
            outputVersionIds = new[] { version.Id },
            checks = new[] { "basic-args-validated" },
            status = "Success"
        });
        await db.SaveChangesAsync(ctx.Ct);

        return new { outlineNodeId = node.Id, versionId = version.Id };
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
    private static Guid GetRequiredGuid(IDictionary<string, object?> args, string key)
    {
        var g = GetGuid(args, key, required: true);
        return g!.Value;
    }
    private static string? GetString(IDictionary<string, object?> args, string key, bool required = false)
    {
        if (!args.TryGetValue(key, out var v) || v is null)
        {
            if (required) throw new ArgumentException($"Missing required '{key}'");
            return null;
        }
        return v.ToString();
    }
    private static int? GetInt(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        if (v is int i) return i;
        if (int.TryParse(v.ToString(), out var ii)) return ii;
        return null;
    }

    private static bool? GetBool(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        if (v is bool b) return b;
        var s = v.ToString()!.Trim().ToLowerInvariant();
        if (s is "1" or "true" or "yes" or "y") return true;
        if (s is "0" or "false" or "no" or "n") return false;
        return bool.TryParse(s, out var val) ? val : null;
    }

    private static string BuildBeatsPrompt(string title, string type, Dictionary<string, object?>? style)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Return ONLY minified JSON: {\"promise\":\"...\",\"progress\":\"...\",\"payoff\":\"...\"}");
        sb.AppendLine($"Design beats for a {type} titled '{title}'.");
        if (style is not null)
        {
            sb.AppendLine("Style hints:");
            foreach (var kv in style) sb.AppendLine($"- {kv.Key}: {kv.Value}");
        }
        return sb.ToString();
    }

    private static bool TryParseBeats(string json, out Dictionary<string, object?> beats)
    {
        beats = new Dictionary<string, object?>();
        try
        {
            var txt = json.Trim();
            int firstBrace = txt.IndexOf('{');
            int lastBrace = txt.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace) txt = txt.Substring(firstBrace, lastBrace - firstBrace + 1);
            var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;
            if (root.TryGetProperty("promise", out var pr)) beats["promise"] = pr.GetString();
            if (root.TryGetProperty("progress", out var pg)) beats["progress"] = pg.GetString();
            if (root.TryGetProperty("payoff", out var pf)) beats["payoff"] = pf.GetString();
            return beats.Count > 0;
        }
        catch { return false; }
    }

    private static async Task<ILLMClient> ResolveClientAsync(CognitionDbContext db, ToolContext ctx, Guid? providerId, Guid? modelId)
    {
        var factory = ctx.Services.GetRequiredService<ILLMClientFactory>();
        if (providerId.HasValue)
            return await factory.CreateAsync(providerId.Value, modelId);
        var providers = await db.Providers.AsNoTracking().Where(p => p.IsActive).ToListAsync(ctx.Ct);
        Guid pid = providers.FirstOrDefault(p => p.Name.Equals("ollama", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? providers.FirstOrDefault(p => p.Name.Equals("openai", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? providers.FirstOrDefault(p => p.Name.Equals("gemini", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? throw new InvalidOperationException("No active LLM provider configured");
        return await factory.CreateAsync(pid, modelId);
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
