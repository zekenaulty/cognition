using System.Text.Json;
using System.Text;
using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Agents;

public static class AgentServiceToolsExtensions
{
    public static async Task<string> AskWithToolsAsync(
        this AgentService svc,
        Guid personaId, Guid providerId, Guid? modelId,
        string input, IToolDispatcher dispatcher, IServiceProvider sp, bool rolePlay = false)
    {
        // Discover available tools for this provider/model and expose a compact schema
        var db = sp.GetRequiredService<CognitionDbContext>();
        var tools = await db.Tools
            .Include(t => t.Parameters)
            .Where(t => t.IsActive)
            .Where(t => db.ToolProviderSupports.Any(s => s.ToolId == t.Id
                                                        && s.ProviderId == providerId
                                                        && (s.ModelId == null || (modelId != null && s.ModelId == modelId))
                                                        && s.SupportLevel != Cognition.Data.Relational.Modules.Common.SupportLevel.Unsupported))
            .AsNoTracking()
            .ToListAsync();

        // Ask with the tool index embedded in the system message
        var draft = await svc.AskWithToolIndexAsync(personaId, providerId, modelId, input, tools, rolePlay);

        // Try to parse a tool plan
        try
        {
            using var doc = JsonDocument.Parse(draft);
            var root = doc.RootElement;
            Guid? toolId = null;
            // Accept by ID
            if (root.TryGetProperty("toolId", out var t) && Guid.TryParse(t.GetString(), out var parsed))
            {
                toolId = parsed;
            }
            else if (root.TryGetProperty("toolName", out var tn))
            {
                var name = tn.GetString();
                var match = tools.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (match != null) toolId = match.Id;
            }

            if (toolId.HasValue)
            {
                var args = root.TryGetProperty("args", out var a)
                    ? JsonSerializer.Deserialize<Dictionary<string, object?>>(a.GetRawText()) ?? new()
                    : new Dictionary<string, object?>();

                var ctx = new ToolContext(null, null, personaId, sp, CancellationToken.None);
                var (ok, result, error) = await dispatcher.ExecuteAsync(toolId.Value, ctx, args, log: true);
                if (!ok) return $"Tool error: {error}";
                return result is string s ? s : JsonSerializer.Serialize(result);
            }
        }
        catch
        {
            // not JSON => direct answer
        }

        return draft;
    }
}
