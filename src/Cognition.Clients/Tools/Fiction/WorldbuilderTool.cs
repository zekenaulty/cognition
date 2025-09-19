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

public class WorldbuilderTool : ITool
{
    public string Name => "WorldbuilderTool";
    public string ClassPath => typeof(WorldbuilderTool).FullName! + ", " + typeof(WorldbuilderTool).Assembly.GetName().Name;

    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var db = ctx.Services.GetRequiredService<CognitionDbContext>();
        var now = DateTime.UtcNow;

    // Required args
    var projectId = GetRequiredGuid(args, "projectId");
    var typeStr = GetString(args, "type", required: true);
    var name = GetString(args, "name", required: true);

    // Optional
    var personaId = GetGuid(args, "personaId");
    var content = args.TryGetValue("content", out var c) && c is Dictionary<string, object?> d ? d : new Dictionary<string, object?>();
    var autoSeedGlossary = GetBool(args, "autoSeedGlossary") ?? false;
    var providerId = GetGuid(args, "providerId");
    var modelId = GetGuid(args, "modelId");
    var description = GetString(args, "description");

        if (!Enum.TryParse<WorldAssetType>(typeStr, ignoreCase: true, out var assetType))
            throw new ArgumentException($"Unknown world asset type: {typeStr}");

        var project = await db.FictionProjects.FirstAsync(p => p.Id == projectId, ctx.Ct);

        // Create world asset
        var asset = new WorldAsset
        {
            FictionProjectId = project.Id,
            Type = assetType,
            Name = name!,
            PersonaId = personaId,
            ActiveVersionIndex = 0,
            CreatedAtUtc = now
        };
        db.WorldAssets.Add(asset);
        await db.SaveChangesAsync(ctx.Ct);

    // Optionally auto-expand content via LLM
    if (content.Count == 0 && !string.IsNullOrWhiteSpace(description))
    {
        try
        {
            var resolver = ctx.Services.GetRequiredService<ILLMProviderResolver>();
            var (client, _, _, _, _) = await resolver.ResolveAsync(providerId, modelId, ctx.Ct);
            var prompt = BuildWorldAssetPrompt(assetType.ToString(), name!, description!);
            var json = await client.GenerateAsync(prompt, track: false);
            if (!string.IsNullOrWhiteSpace(json) && TryParseContent(json, out var gen)) content = gen;
        }
        catch { }
    }

    var version = new WorldAssetVersion
    {
        WorldAssetId = asset.Id,
        VersionIndex = 0,
        Content = content,
        CreatedAtUtc = now
    };
        db.WorldAssetVersions.Add(version);

        if (autoSeedGlossary && !string.IsNullOrWhiteSpace(name))
        {
            db.GlossaryTerms.Add(new GlossaryTerm
            {
                FictionProjectId = project.Id,
                Term = name!,
                Definition = content.TryGetValue("summary", out var s) ? s?.ToString() ?? name! : name!,
                CreatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(ctx.Ct);

        // Micro-CoT: Thought + Event
        await LogThoughtAsync(db, ctx, $"Created {assetType} '{name}' with v0.");
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

        return new { assetId = asset.Id, versionId = version.Id };
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
    private static bool? GetBool(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        if (v is bool b) return b;
        var s = v.ToString()!.Trim().ToLowerInvariant();
        if (s is "1" or "true" or "yes" or "y") return true;
        if (s is "0" or "false" or "no" or "n") return false;
        return bool.Parse(s);
    }

    private static string BuildWorldAssetPrompt(string type, string name, string description)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Return ONLY minified JSON object with keys summarizing the asset, include 'summary' and key attributes.");
        sb.AppendLine($"Design a {type} named '{name}'. Description: {description}");
        return sb.ToString();
    }

    private static bool TryParseContent(string json, out Dictionary<string, object?> content)
    {
        content = new Dictionary<string, object?>();
        try
        {
            var txt = json.Trim();
            int firstBrace = txt.IndexOf('{');
            int lastBrace = txt.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace) txt = txt.Substring(firstBrace, lastBrace - firstBrace + 1);
            using var doc = JsonDocument.Parse(txt);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                content[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
            }
            return content.Count > 0;
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
