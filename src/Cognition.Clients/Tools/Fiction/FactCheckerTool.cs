using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Tools.Fiction;

public class FactCheckerTool : ITool
{
    public string Name => "FactCheckerTool";
    public string ClassPath => typeof(FactCheckerTool).FullName! + ", " + typeof(FactCheckerTool).Assembly.GetName().Name;

    // Args: projectId, draftSegmentVersionId
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var projectId = GetRequiredGuid(args, "projectId");
        var versionId = GetRequiredGuid(args, "draftSegmentVersionId");
        var now = DateTime.UtcNow;

        var dsv = await db.DraftSegmentVersions.Include(v => v.DraftSegment).FirstOrDefaultAsync(v => v.Id == versionId, ctx.Ct);
        if (dsv is null) throw new ArgumentException("DraftSegmentVersion not found");
        if (dsv.DraftSegment.FictionProjectId != projectId) throw new ArgumentException("draftSegmentVersionId does not belong to project");

        var text = dsv.BodyMarkdown ?? string.Empty;
        var issues = new List<string>();
        var glossary = await db.GlossaryTerms.Where(t => t.FictionProjectId == projectId).Select(t => t.Term).ToListAsync(ctx.Ct);
        var canon = await db.CanonRules.Where(r => r.FictionProjectId == projectId).ToListAsync(ctx.Ct);
        var style = await db.StyleGuides.AsNoTracking().FirstOrDefaultAsync(g => g.FictionProjectId == projectId, ctx.Ct);

        // P1: Term presence — detect capitalized words not in glossary (simple heuristic)
        var unknownCaps = DetectUnknownCapitalizedTerms(text, glossary);
        foreach (var term in unknownCaps)
        {
            issues.Add($"{FictionAnnotationCodes.GlossaryUnknownTerm}:{term}");
            db.Annotations.Add(new Annotation
            {
                FictionProjectId = projectId,
                TargetType = nameof(DraftSegmentVersion),
                TargetId = dsv.Id,
                Type = AnnotationType.FactCheck,
                Severity = AnnotationSeverity.Warning,
                Message = "Unknown term not in glossary",
                Details = FictionAnnotationCodes.GlossaryUnknownTerm,
                CreatedAtUtc = now
            });
        }

        // P1: Canon potential contradiction — naive negation check against keys
        foreach (var rule in canon)
        {
            var key = rule.Key;
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (AppearsNegated(text, key))
            {
                var msg = $"Canon contradiction suspected for key '{key}'";
                issues.Add(FictionAnnotationCodes.CanonContradiction);
                db.Annotations.Add(new Annotation
                {
                    FictionProjectId = projectId,
                    TargetType = nameof(DraftSegmentVersion),
                    TargetId = dsv.Id,
                    Type = AnnotationType.Continuity,
                    Severity = AnnotationSeverity.Warning,
                    Message = msg,
                    Details = FictionAnnotationCodes.CanonContradiction,
                    CreatedAtUtc = now
                });
            }
        }

        // P1: Style compliance (POV heuristic)
        if (style?.Rules != null && style.Rules.TryGetValue("pov", out var pov) && pov is string povs)
        {
            var povLower = povs.Trim().ToLowerInvariant();
            if (povLower.Contains("first") && !text.Contains(" I "))
            {
                issues.Add(FictionAnnotationCodes.StylePovMismatchFirst);
                db.Annotations.Add(new Annotation
                {
                    FictionProjectId = projectId,
                    TargetType = nameof(DraftSegmentVersion),
                    TargetId = dsv.Id,
                    Type = AnnotationType.StyleViolation,
                    Severity = AnnotationSeverity.Warning,
                    Message = "POV mismatch: expected first-person",
                    Details = FictionAnnotationCodes.StylePovMismatchFirst,
                    CreatedAtUtc = now
                });
            }
        }

        if (text.Length < 100) issues.Add(FictionAnnotationCodes.LengthTooShort);

        foreach (var iss in issues)
        {
            db.Annotations.Add(new Annotation
            {
                FictionProjectId = projectId,
                TargetType = nameof(DraftSegmentVersion),
                TargetId = dsv.Id,
                Type = AnnotationType.Continuity,
                Severity = AnnotationSeverity.Warning,
                Message = iss,
                CreatedAtUtc = now
            });
        }
        await db.SaveChangesAsync(ctx.Ct);

        await LogThoughtAsync(db, ctx, issues.Count == 0 ? "Fact check passed" : $"Fact check found {issues.Count} issues");
        LogEvent(db, ctx, "ToolExecutionCompleted", new { tool = Name, args, issues, status = issues.Count == 0 ? "Pass" : "Warn" });
        await db.SaveChangesAsync(ctx.Ct);

        return new { ok = true, issues };
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


    private static IEnumerable<string> DetectUnknownCapitalizedTerms(string text, List<string> glossary)
    {
        var set = new HashSet<string>(glossary, StringComparer.OrdinalIgnoreCase);
        var words = System.Text.RegularExpressions.Regex.Matches(text, @"\b[A-Z][a-zA-Z]{2,}\b")
            .Select(m => m.Value)
            .Where(w => !set.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return words;
    }

    private static bool AppearsNegated(string text, string key)
    {
        // naive window check: "not <key>" / "no <key>" / "never <key>"
        var lower = text.ToLowerInvariant();
        var k = key.ToLowerInvariant();
        return lower.Contains($"not {k}") || lower.Contains($"no {k}") || lower.Contains($"never {k}");
    }
    private static Guid GetRequiredGuid(IDictionary<string, object?> args, string key)
    {
        var g = GetGuid(args, key, required: true);
        return g!.Value;
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
