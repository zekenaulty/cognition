using System.Text;
using Cognition.Clients.Tools;
using Cognition.Clients.LLM;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Tools.Fiction;

public class SceneDraftTool : ITool
{
    public string Name => "SceneDraftTool";
    public string ClassPath => typeof(SceneDraftTool).FullName! + ", " + typeof(SceneDraftTool).Assembly.GetName().Name;

    // Args: projectId (Guid), outlineNodeId? (Guid), draftSegmentId? (Guid), prompt? (string), providerId? (Guid), modelId? (Guid)
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;

        var projectId = GetRequiredGuid(args, "projectId");
        var outlineNodeId = GetGuid(args, "outlineNodeId");
        var draftSegmentId = GetGuid(args, "draftSegmentId");
        var prompt = GetString(args, "prompt") ?? string.Empty;
        var providerId = GetGuid(args, "providerId");
        var modelId = GetGuid(args, "modelId");

        DraftSegment segment;
        if (draftSegmentId.HasValue)
        {
            segment = await db.DraftSegments.FirstAsync(x => x.Id == draftSegmentId.Value, ctx.Ct);
        }
        else
        {
            segment = new DraftSegment
            {
                FictionProjectId = projectId,
                OutlineNodeId = outlineNodeId,
                Title = await ResolveSegmentTitleAsync(db, outlineNodeId, ctx.Ct) ?? "Untitled Scene",
                ActiveVersionIndex = 0,
                CreatedAtUtc = now
            };
            db.DraftSegments.Add(segment);
            await db.SaveChangesAsync(ctx.Ct);
        }

        // Cross-project guard: OutlineNode (if provided) must belong to project
        if (outlineNodeId.HasValue)
        {
            var on = await db.OutlineNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == outlineNodeId.Value, ctx.Ct);
            if (on == null || on.FictionProjectId != projectId) throw new ArgumentException("outlineNodeId does not belong to project");
        }
        var beats = await LoadBeatsAsync(db, outlineNodeId, ctx.Ct);
        var style = await LoadStyleAsync(db, projectId, ctx.Ct);
        var canon = await LoadCanonAsync(db, projectId, ctx.Ct, take: 8);
        var glossary = await LoadGlossaryAsync(db, projectId, ctx.Ct, take: 10);

        // Micro-CoT: self-check constraints (POV/tense present?)
        var issues = new List<string>();
        if (style is not null)
        {
            if (!style.TryGetValue("pov", out _)) issues.Add(FictionAnnotationCodes.StylePovMissing);
            if (!style.TryGetValue("tense", out _)) issues.Add(FictionAnnotationCodes.StyleTenseMissing);
        }

        // Execute: call LLM to generate scene content (fallback to deterministic stub)
        var resolver = ctx.Services.GetRequiredService<ILLMProviderResolver>();
        var (client, resolvedProviderId, providerName, resolvedModelId, modelName) = await resolver.ResolveAsync(providerId, modelId, ctx.Ct);
        var llmPrompt = BuildDraftPrompt(style, beats, canon, glossary, prompt);
        var body = await client.GenerateAsync(llmPrompt, track: false);
        if (string.IsNullOrWhiteSpace(body)) body = await GenerateDraftFallbackAsync(beats, prompt);

        // Transaction: add version + bump ActiveVersionIndex atomically
        Guid newVersionId;
        await using (var tx = await db.Database.BeginTransactionAsync(ctx.Ct))
        {
            var versionIndex = segment.ActiveVersionIndex >= 0 ? segment.ActiveVersionIndex + 1 : 0;
            var dsv = new DraftSegmentVersion
            {
                DraftSegmentId = segment.Id,
                VersionIndex = versionIndex,
                BodyMarkdown = body,
                Metrics = new Dictionary<string, object?>
                {
                    [FictionMetricKeys.Chars] = body.Length,
                    [FictionMetricKeys.Issues] = issues.ToArray(),
                    [FictionMetricKeys.Engine] = "llm",
                    [FictionMetricKeys.Provider] = providerName,
                    [FictionMetricKeys.Model] = modelName,
                    [FictionMetricKeys.InputOutlineNodeId] = outlineNodeId,
                    [FictionMetricKeys.InputStyleGuideId] = await GetStyleGuideIdAsync(db, projectId, ctx.Ct),
                    [FictionMetricKeys.InputGlossarySample] = glossary.Select(g => g.Term).Take(5).ToArray(),
                    [FictionMetricKeys.InputCanonSample] = canon.Select(c => c.Key).Take(5).ToArray()
                },
                CreatedAtUtc = now
            };
            db.DraftSegmentVersions.Add(dsv);
            segment.ActiveVersionIndex = versionIndex;
            await db.SaveChangesAsync(ctx.Ct);
            newVersionId = dsv.Id;
            await tx.CommitAsync(ctx.Ct);
        }

        // Post-check: simple continuity/style heuristics; write annotation if needed
        var warnings = new List<string>();
        if (body.Length < 200) warnings.Add(FictionAnnotationCodes.LengthTooShort);
        if (issues.Count > 0)
        {
            foreach (var iss in issues)
            {
                db.Annotations.Add(new Annotation
                {
                    FictionProjectId = projectId,
                    TargetType = nameof(DraftSegment),
                    TargetId = segment.Id,
                    Type = AnnotationType.StyleViolation,
                    Severity = AnnotationSeverity.Warning,
                    Message = iss,
                    Details = iss,
                    CreatedAtUtc = now
                });
            }
            await db.SaveChangesAsync(ctx.Ct);
        }

        if (warnings.Count > 0)
        {
            foreach (var w in warnings)
            {
                db.Annotations.Add(new Annotation
                {
                    FictionProjectId = projectId,
                    TargetType = nameof(DraftSegmentVersion),
                    TargetId = newVersionId,
                    Type = AnnotationType.StyleViolation,
                    Severity = AnnotationSeverity.Warning,
                    Message = w,
                    Details = w,
                    CreatedAtUtc = now
                });
            }
            await db.SaveChangesAsync(ctx.Ct);
        }

        await LogThoughtAsync(db, ctx, "Drafted scene from outline + style.");
        LogEvent(db, ctx, "ToolExecutionCompleted", new
        {
            tool = Name,
            args,
            inputVersionIds = Array.Empty<Guid>(),
            outputVersionIds = new[] { newVersionId },
            checks = warnings,
            status = warnings.Count == 0 ? "Success" : "SuccessWithWarnings"
        });
        await db.SaveChangesAsync(ctx.Ct);

        return new { draftSegmentId = segment.Id, versionId = newVersionId };
    }

    private static async Task<string?> ResolveSegmentTitleAsync(CognitionDbContext db, Guid? outlineNodeId, CancellationToken ct)
    {
        if (!outlineNodeId.HasValue) return null;
        var node = await db.OutlineNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == outlineNodeId.Value, ct);
        return node?.Title;
    }

    private static async Task<Dictionary<string, object?>?> LoadBeatsAsync(CognitionDbContext db, Guid? outlineNodeId, CancellationToken ct)
    {
        if (!outlineNodeId.HasValue) return null;
        var node = await db.OutlineNodes.Include(n => n.Versions).FirstOrDefaultAsync(n => n.Id == outlineNodeId.Value, ct);
        if (node is null || node.ActiveVersionIndex < 0) return null;
        var onv = node.Versions.FirstOrDefault(v => v.VersionIndex == node.ActiveVersionIndex);
        return onv?.Beats;
    }

    private static async Task<Dictionary<string, object?>?> LoadStyleAsync(CognitionDbContext db, Guid projectId, CancellationToken ct)
    {
        var guide = await db.StyleGuides.AsNoTracking().FirstOrDefaultAsync(g => g.FictionProjectId == projectId, ct);
        return guide?.Rules;
    }

    private static async Task<Guid?> GetStyleGuideIdAsync(CognitionDbContext db, Guid projectId, CancellationToken ct)
    {
        var guide = await db.StyleGuides.AsNoTracking().FirstOrDefaultAsync(g => g.FictionProjectId == projectId, ct);
        return guide?.Id;
    }

    private static string BuildDraftPrompt(Dictionary<string, object?>? style, Dictionary<string, object?>? beats, List<(string Key, string Val)> canon, List<(string Term, string Def)> glossary, string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SYSTEM: You are an expert fiction author.");
        if (style is not null && style.Count > 0)
        {
            sb.AppendLine("STYLE GUIDE (must enforce):");
            foreach (var kv in style)
            {
                sb.AppendLine($"- {kv.Key}: {kv.Value}");
            }
        }
        if (canon.Count > 0)
        {
            sb.AppendLine("CANON (do not violate):");
            foreach (var c in canon) sb.AppendLine($"- {c.Key}: {c.Val}");
        }
        if (glossary.Count > 0)
        {
            sb.AppendLine("GLOSSARY (use terms consistently):");
            foreach (var g in glossary) sb.AppendLine($"- {g.Term}: {g.Def}");
        }
        sb.AppendLine();
        sb.AppendLine("TASK: Write a single narrative scene in raw Markdown only (no headings, no lists). Use deep POV, show-don't-tell, vivid sensory detail. Target 1200–2000 words.");
        if (!string.IsNullOrWhiteSpace(userPrompt)) sb.AppendLine($"USER NOTES: {userPrompt}");
        if (beats is not null && beats.Count > 0)
        {
            sb.AppendLine("SCENE BEATS (promise → progress → payoff):");
            if (beats.TryGetValue("promise", out var pr)) sb.AppendLine($"- Promise: {pr}");
            if (beats.TryGetValue("progress", out var pg)) sb.AppendLine($"- Progress: {pg}");
            if (beats.TryGetValue("payoff", out var pf)) sb.AppendLine($"- Payoff: {pf}");
        }
        sb.AppendLine();
        sb.AppendLine("Constraints:");
        sb.AppendLine("- No headings or bullet lists.");
        sb.AppendLine("- Keep tense/POV consistent with style.");
        sb.AppendLine("- Return only the scene body in Markdown.");
        return sb.ToString();
    }

    private static async Task<string> GenerateDraftFallbackAsync(Dictionary<string, object?>? beats, string prompt)
    {
        var sb = new StringBuilder();
        if (beats is not null)
        {
            if (beats.TryGetValue("promise", out var pr)) sb.AppendLine($"Promise: {pr}");
            if (beats.TryGetValue("progress", out var pg)) sb.AppendLine($"Progress: {pg}");
            if (beats.TryGetValue("payoff", out var pf)) sb.AppendLine($"Payoff: {pf}");
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(prompt)) sb.AppendLine(prompt);
        sb.AppendLine();
        sb.AppendLine("[LLM unavailable: placeholder scene stub]");
        return await Task.FromResult(sb.ToString());
    }

    private static async Task<List<(string Key, string Val)>> LoadCanonAsync(CognitionDbContext db, Guid projectId, CancellationToken ct, int take)
    {
        var rules = await db.CanonRules.AsNoTracking()
            .Where(r => r.FictionProjectId == projectId)
            .OrderByDescending(r => r.Confidence)
            .Take(take)
            .ToListAsync(ct);
        var list = new List<(string, string)>();
        foreach (var r in rules)
        {
            var v = r.Value != null && r.Value.TryGetValue("value", out var val) && val != null ? val.ToString()! : Newtonsoft.Json.JsonConvert.SerializeObject(r.Value ?? new());
            list.Add((r.Key, v));
        }
        return list;
    }

    private static async Task<List<(string Term, string Def)>> LoadGlossaryAsync(CognitionDbContext db, Guid projectId, CancellationToken ct, int take)
    {
        var items = await db.GlossaryTerms.AsNoTracking()
            .Where(t => t.FictionProjectId == projectId)
            .OrderBy(t => t.Term)
            .Take(take)
            .ToListAsync(ct);
        return items.Select(x => (x.Term, x.Definition)).ToList();
    }

    private static async Task<ILLMClient> ResolveClientAsync(CognitionDbContext db, ToolContext ctx, Guid? providerId, Guid? modelId)
    {
        var factory = ctx.Services.GetRequiredService<ILLMClientFactory>();
        if (providerId.HasValue)
            return await factory.CreateAsync(providerId.Value, modelId);
        // Default preference order: Ollama -> OpenAI -> Gemini
        var providers = await db.Providers.AsNoTracking().Where(p => p.IsActive).ToListAsync(ctx.Ct);
        Guid pid = providers.FirstOrDefault(p => p.Name.Equals("ollama", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? providers.FirstOrDefault(p => p.Name.Equals("openai", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? providers.FirstOrDefault(p => p.Name.Equals("gemini", StringComparison.OrdinalIgnoreCase))?.Id
                   ?? throw new InvalidOperationException("No active LLM provider configured");
        return await factory.CreateAsync(pid, modelId);
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
    private static string? GetString(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        return v.ToString();
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
