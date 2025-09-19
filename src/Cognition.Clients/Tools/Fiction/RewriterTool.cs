using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using Cognition.Clients.LLM;

namespace Cognition.Clients.Tools.Fiction;

public class RewriterTool : ITool
{
    public string Name => "RewriterTool";
    public string ClassPath => typeof(RewriterTool).FullName! + ", " + typeof(RewriterTool).Assembly.GetName().Name;

    // Args: draftSegmentId or draftSegmentVersionId, mode? (tighten/deep-pov/reading-level), note?, providerId?, modelId?
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;
        var mode = (args.TryGetValue("mode", out var mv) ? mv?.ToString() : null) ?? "tighten";
        var providerId = GetGuid(args, "providerId");
        var modelId = GetGuid(args, "modelId");

        // Resolve version target
        Guid? versionId = GetGuid(args, "draftSegmentVersionId");
        DraftSegmentVersion src;
        if (versionId.HasValue)
        {
            src = await db.DraftSegmentVersions.Include(v => v.DraftSegment).FirstAsync(v => v.Id == versionId.Value, ctx.Ct);
        }
        else
        {
            var segmentId = GetGuid(args, "draftSegmentId", required: true)!.Value;
            var seg = await db.DraftSegments.Include(s => s.Versions).FirstAsync(s => s.Id == segmentId, ctx.Ct);
            var active = seg.Versions.FirstOrDefault(v => v.VersionIndex == seg.ActiveVersionIndex);
            if (active is null) throw new InvalidOperationException("No active version to rewrite");
            src = active;
        }

        var resolver = ctx.Services.GetRequiredService<ILLMProviderResolver>();
        // derive client to annotate provenance even if we fall back
        ILLMClient? client = null; string? providerName = null; string? modelName = null; string engineHint = "heuristic";
        try
        {
            var r = await resolver.ResolveAsync(providerId, modelId, ctx.Ct);
            client = r.Client; providerName = r.ProviderName; modelName = r.ModelName; engineHint = "llm";
        }
        catch { }

        var rewrite = await RewriteAsync(db, ctx, src.BodyMarkdown, mode, providerId, modelId);
        var nextIndex = src.VersionIndex + 1;

        Guid newVersionId;
        await using (var tx = await db.Database.BeginTransactionAsync(ctx.Ct))
        {
            var dsv = new DraftSegmentVersion
            {
                DraftSegmentId = src.DraftSegmentId,
                VersionIndex = nextIndex,
                BodyMarkdown = rewrite.Text,
                Metrics = new Dictionary<string, object?>
                {
                    [FictionMetricKeys.Mode] = mode,
                    [FictionMetricKeys.Engine] = rewrite.Engine ?? engineHint,
                    [FictionMetricKeys.Provider] = rewrite.Provider ?? providerName,
                    [FictionMetricKeys.Model] = rewrite.Model ?? modelName,
                    [FictionMetricKeys.Chars] = rewrite.Text.Length,
                    [FictionMetricKeys.BasedOnVersion] = src.Id
                },
                CreatedAtUtc = now
            };
            db.DraftSegmentVersions.Add(dsv);

            var segUpdate = await db.DraftSegments.FirstAsync(s => s.Id == src.DraftSegmentId, ctx.Ct);
            segUpdate.ActiveVersionIndex = nextIndex;
            await db.SaveChangesAsync(ctx.Ct);
            newVersionId = dsv.Id;
            await tx.CommitAsync(ctx.Ct);
        }

        await LogThoughtAsync(db, ctx, $"Rewrote scene ({mode}) v{src.VersionIndex} -> v{nextIndex}");
        LogEvent(db, ctx, "ToolExecutionCompleted", new { tool = Name, args, outputVersionIds = new[] { newVersionId }, status = "Success" });
        await db.SaveChangesAsync(ctx.Ct);

        return new { versionId = newVersionId };
    }

    private static string Transform(string input, string mode)
    {
        if (string.Equals(mode, "tighten", StringComparison.OrdinalIgnoreCase))
        {
            // naive tighten: collapse multiple spaces and trim lines
            var lines = (input ?? string.Empty).Split('\n');
            var trimmed = lines.Select(l => string.Join(' ', l.Split(' ', StringSplitOptions.RemoveEmptyEntries))).ToArray();
            return string.Join('\n', trimmed);
        }
        if (string.Equals(mode, "deep-pov", StringComparison.OrdinalIgnoreCase))
        {
            return input + "\n\n[Deep POV emphasis added in v1 placeholder]";
        }
        if (string.Equals(mode, "reading-level", StringComparison.OrdinalIgnoreCase))
        {
            return input + "\n\n[Reading level adjusted placeholder]";
        }
        return input;
    }

    private static async Task<(string Text, string Engine, string? Provider, string? Model)> RewriteAsync(CognitionDbContext db, ToolContext ctx, string input, string mode, Guid? providerId, Guid? modelId)
    {
        try
        {
            var factory = ctx.Services.GetRequiredService<ILLMClientFactory>();
            Guid pid;
            if (providerId.HasValue)
            {
                pid = providerId.Value;
            }
            else
            {
                var providers = await db.Providers.AsNoTracking().Where(p => p.IsActive).ToListAsync(ctx.Ct);
                pid = providers.FirstOrDefault(p => p.Name.Equals("ollama", StringComparison.OrdinalIgnoreCase))?.Id
                      ?? providers.FirstOrDefault(p => p.Name.Equals("openai", StringComparison.OrdinalIgnoreCase))?.Id
                      ?? providers.FirstOrDefault(p => p.Name.Equals("gemini", StringComparison.OrdinalIgnoreCase))?.Id
                      ?? Guid.Empty;
                if (pid == Guid.Empty) return (Transform(input, mode), "heuristic", null, null);
            }
            var client = await factory.CreateAsync(pid, modelId);
            var prompt = $"Rewrite the following scene with mode='{mode}'. Preserve meaning, improve flow, clarity, and style. Return only the rewritten markdown.\n\n---\n{input}\n---";
            var res = await client.GenerateAsync(prompt, track: false);
            if (string.IsNullOrWhiteSpace(res)) return (Transform(input, mode), "heuristic", null, null);
            // Look up provider/model names for provenance
            var providerName = await db.Providers.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Name).FirstOrDefaultAsync(ctx.Ct);
            var modelName = modelId.HasValue ? await db.Models.AsNoTracking().Where(m => m.Id == modelId.Value).Select(m => m.Name).FirstOrDefaultAsync(ctx.Ct) : null;
            return (res, "llm", providerName, modelName);
        }
        catch
        {
            return (Transform(input, mode), "heuristic", null, null);
        }
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
