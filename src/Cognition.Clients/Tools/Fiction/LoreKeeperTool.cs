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

public class LoreKeeperTool : ITool
{
    public string Name => "LoreKeeperTool";
    public string ClassPath => typeof(LoreKeeperTool).FullName! + ", " + typeof(LoreKeeperTool).Assembly.GetName().Name;

    // Args: projectId (Guid), terms: [{term,definition,aliases,domain}], canon:[{scope,key,value,evidence,confidence}], styleRules: {..}, extractText?, providerId?, modelId?
    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var projectId = GetGuid(args, "projectId", required: true);
        var providerId = GetGuid(args, "providerId");
        var modelId = GetGuid(args, "modelId");
        var extractText = args.TryGetValue("extractText", out var et) ? et?.ToString() : null;
        var now = DateTime.UtcNow;

        var project = await db.FictionProjects.FirstAsync(p => p.Id == projectId, ctx.Ct);

        // Ingest style rules (upsert default style guide if provided)
        if (args.TryGetValue("styleRules", out var sObj) && sObj is Dictionary<string, object?> rules)
        {
            var guide = await db.StyleGuides.FirstOrDefaultAsync(g => g.FictionProjectId == project.Id && g.Name == "Default", ctx.Ct)
                        ?? new StyleGuide { FictionProjectId = project.Id, Name = "Default", CreatedAtUtc = now };
            guide.Rules = rules;
            if (guide.Id == Guid.Empty) db.StyleGuides.Add(guide);
            project.PrimaryStyleGuideId ??= guide.Id;
        }

        // If extraction text provided and no explicit terms/canon, ask LLM to extract
        if (!string.IsNullOrWhiteSpace(extractText) && !args.ContainsKey("terms") && !args.ContainsKey("canon"))
        {
            try
            {
                var resolver = ctx.Services.GetRequiredService<ILLMProviderResolver>();
                var (client, _, _, _, _) = await resolver.ResolveAsync(providerId, modelId, ctx.Ct);
                var prompt = BuildExtractionPrompt();
                var json = await client.GenerateAsync(prompt + "\n\nTEXT:\n" + extractText, track: false);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    if (TryParseExtraction(json, out var termsFromLlm, out var canonFromLlm))
                    {
                        args["terms"] = termsFromLlm;
                        args["canon"] = canonFromLlm;
                    }
                }
            }
            catch { }
        }

        // Ingest glossary terms
        if (args.TryGetValue("terms", out var tObj) && tObj is IEnumerable<object?> terms)
        {
            foreach (var t in terms)
            {
                if (t is not Dictionary<string, object?> d) continue;
                var term = d.TryGetValue("term", out var tv) ? tv?.ToString() : null;
                if (string.IsNullOrWhiteSpace(term)) continue;
                var gt = await db.GlossaryTerms.FirstOrDefaultAsync(x => x.FictionProjectId == project.Id && x.Term == term, ctx.Ct);
                if (gt is null)
                {
                    gt = new GlossaryTerm { FictionProjectId = project.Id, Term = term!, CreatedAtUtc = now };
                    db.GlossaryTerms.Add(gt);
                }
                gt.Definition = d.TryGetValue("definition", out var def) ? def?.ToString() ?? string.Empty : string.Empty;
                gt.Aliases = d.TryGetValue("aliases", out var al) && al is IEnumerable<object?> ao ? ao.Select(x => x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray() : null;
                gt.Domain = d.TryGetValue("domain", out var dom) ? dom?.ToString() : null;
            }
        }

        // Ingest canon rules
        if (args.TryGetValue("canon", out var cObj) && cObj is IEnumerable<object?> canon)
        {
            foreach (var c in canon)
            {
                if (c is not Dictionary<string, object?> d) continue;
                var key = d.TryGetValue("key", out var kv) ? kv?.ToString() : null;
                if (string.IsNullOrWhiteSpace(key)) continue;
                var scopeStr = d.TryGetValue("scope", out var sc) ? sc?.ToString() ?? "Global" : "Global";
                Enum.TryParse<CanonScope>(scopeStr, true, out var scope);
                var rule = await db.CanonRules.FirstOrDefaultAsync(x => x.FictionProjectId == project.Id && x.Key == key, ctx.Ct)
                           ?? new CanonRule { FictionProjectId = project.Id, Key = key!, CreatedAtUtc = now };
                rule.Scope = scope;
                rule.Value = d.TryGetValue("value", out var vv) && vv is Dictionary<string, object?> vvd ? vvd : new Dictionary<string, object?> { ["value"] = d.TryGetValue("value", out var v) ? v : null };
                rule.Evidence = d.TryGetValue("evidence", out var ev) ? ev?.ToString() : null;
                rule.Confidence = d.TryGetValue("confidence", out var cf) && double.TryParse(cf?.ToString(), out var cfd) ? cfd : 0.9;
                if (rule.Id == Guid.Empty) db.CanonRules.Add(rule);
            }
        }

        await db.SaveChangesAsync(ctx.Ct);

        await LogThoughtAsync(db, ctx, "Lore curated: style/glossary/canon updated.");
        LogEvent(db, ctx, "ToolExecutionCompleted", new { tool = Name, args, status = "Success" });
        await db.SaveChangesAsync(ctx.Ct);

        return new { ok = true };
    }

    private static Guid GetGuid(IDictionary<string, object?> args, string key, bool required = false)
    {
        if (!args.TryGetValue(key, out var v) || v is null)
        {
            if (required) throw new ArgumentException($"Missing required '{key}'");
            return Guid.Empty;
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

    private static string BuildExtractionPrompt()
    {
        return "Extract glossary terms and canon rules. Return ONLY minified JSON: {\"terms\":[{\"term\":\"...\",\"definition\":\"...\"}],\"canon\":[{\"scope\":\"Global|Arc|Character|Location|System\",\"key\":\"...\",\"value\":{...},\"evidence\":\"...\",\"confidence\":0.9}]}";
    }

    private static bool TryParseExtraction(string json, out List<Dictionary<string, object?>> terms, out List<Dictionary<string, object?>> canon)
    {
        terms = new(); canon = new();
        try
        {
            var txt = json.Trim();
            int firstBrace = txt.IndexOf('{');
            int lastBrace = txt.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace) txt = txt.Substring(firstBrace, lastBrace - firstBrace + 1);
            using var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;
            if (root.TryGetProperty("terms", out var ta) && ta.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in ta.EnumerateArray())
                {
                    var d = new Dictionary<string, object?>();
                    if (el.TryGetProperty("term", out var t)) d["term"] = t.GetString();
                    if (el.TryGetProperty("definition", out var def)) d["definition"] = def.GetString();
                    terms.Add(d);
                }
            }
            if (root.TryGetProperty("canon", out var ca) && ca.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in ca.EnumerateArray())
                {
                    var d = new Dictionary<string, object?>();
                    if (el.TryGetProperty("scope", out var sc)) d["scope"] = sc.GetString();
                    if (el.TryGetProperty("key", out var k)) d["key"] = k.GetString();
                    if (el.TryGetProperty("value", out var v)) d["value"] = JsonDocument.Parse(v.GetRawText()).RootElement.ToString();
                    if (el.TryGetProperty("evidence", out var ev)) d["evidence"] = ev.GetString();
                    if (el.TryGetProperty("confidence", out var cf)) d["confidence"] = cf.TryGetDouble(out var cfd) ? cfd : 0.9;
                    canon.Add(d);
                }
            }
            return terms.Count + canon.Count > 0;
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
}
