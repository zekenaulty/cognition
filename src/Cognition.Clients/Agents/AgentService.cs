using System.Text;
using System.Text.Json;
using Cognition.Clients.LLM;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Clients.Agents;

public interface IAgentService
{
    Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false);
    Task<string> AskWithToolsAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false);
}

public class AgentService : IAgentService
{
    private readonly CognitionDbContext _db;
    private readonly ILLMClientFactory _factory;
    private readonly Cognition.Clients.Tools.IToolDispatcher _dispatcher;
    private readonly IServiceProvider _sp;

    public AgentService(CognitionDbContext db, ILLMClientFactory factory, Cognition.Clients.Tools.IToolDispatcher dispatcher, IServiceProvider sp)
    {
        _db = db;
        _factory = factory;
        _dispatcher = dispatcher;
        _sp = sp;
    }

    public async Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false)
    {
        var persona = await _db.Personas.FirstAsync(p => p.Id == personaId);
        var sys = BuildSystemMessage(persona, rolePlay);
        var client = await _factory.CreateAsync(providerId, modelId);
        var prompt = string.IsNullOrWhiteSpace(sys) ? input : $"{sys}\n\nUser: {input}";
        return await client.GenerateAsync(prompt, track: false);
    }

    public async Task<string> AskWithToolsAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false)
    {
        // Discover available tools for this provider/model and expose a compact schema
        var tools = await _db.Tools
            .Include(t => t.Parameters)
            .Where(t => t.IsActive)
            .Where(t => _db.ToolProviderSupports.Any(s => s.ToolId == t.Id
                                                        && s.ProviderId == providerId
                                                        && (s.ModelId == null || (modelId != null && s.ModelId == modelId))
                                                        && s.SupportLevel != Cognition.Data.Relational.Modules.Common.SupportLevel.Unsupported))
            .AsNoTracking()
            .ToListAsync();

        var draft = await AskWithToolIndexAsync(personaId, providerId, modelId, input, tools, rolePlay);

        // Try to parse a tool plan (by id or name)
        try
        {
            using var doc = JsonDocument.Parse(draft);
            var root = doc.RootElement;
            Guid? toolId = null;
            if (root.TryGetProperty("toolId", out var t))
            {
                var ts = t.GetString();
                if (Guid.TryParse(ts, out var tmp)) toolId = tmp;
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

                var ctx = new Cognition.Clients.Tools.ToolContext(null, null, personaId, _sp, CancellationToken.None);
                var exec = await _dispatcher.ExecuteAsync(toolId.Value, ctx, args, log: true);
                if (!exec.ok) return $"Tool error: {exec.error}";
                return exec.result is string s ? s : JsonSerializer.Serialize(exec.result);
            }
        }
        catch
        {
            // not JSON => direct answer
        }

        return draft;
    }

    // Non-interface helper: include a Tool Index in the system message
    public async Task<string> AskWithToolIndexAsync(Guid personaId, Guid providerId, Guid? modelId, string input, IEnumerable<Tool> tools, bool rolePlay = false)
    {
        var persona = await _db.Personas.FirstAsync(p => p.Id == personaId);
        var sys = BuildSystemMessage(persona, rolePlay);
        var toolIndex = BuildToolIndexSection(tools);
        var fullSys = string.IsNullOrWhiteSpace(toolIndex) ? sys : (string.IsNullOrWhiteSpace(sys) ? toolIndex : $"{sys}\n\n{toolIndex}");
        var client = await _factory.CreateAsync(providerId, modelId);
        var prompt = string.IsNullOrWhiteSpace(fullSys) ? input : $"{fullSys}\n\nUser: {input}";
        return await client.GenerateAsync(prompt, track: false);
    }

    private static string BuildSystemMessage(Persona p, bool rolePlay)
    {
        var sb = new StringBuilder();
        if (rolePlay)
        {
            sb.AppendLine($"You are role-playing as {p.Name} ({p.Nickname}).");
        }
        if (!string.IsNullOrWhiteSpace(p.Role)) sb.AppendLine($"Role: {p.Role}");
        if (!string.IsNullOrWhiteSpace(p.Gender)) sb.AppendLine($"Gender: {p.Gender}");
        if (!string.IsNullOrWhiteSpace(p.Essence)) sb.AppendLine($"Essence: {p.Essence}");
        if (!string.IsNullOrWhiteSpace(p.Beliefs)) sb.AppendLine($"Beliefs: {p.Beliefs}");
        if (!string.IsNullOrWhiteSpace(p.Background)) sb.AppendLine($"Background: {p.Background}");
        if (!string.IsNullOrWhiteSpace(p.CommunicationStyle)) sb.AppendLine($"CommunicationStyle: {p.CommunicationStyle}");
        if (!string.IsNullOrWhiteSpace(p.EmotionalDrivers)) sb.AppendLine($"EmotionalDrivers: {p.EmotionalDrivers}");
        if (p.SignatureTraits is { Length: > 0 }) sb.AppendLine($"SignatureTraits: {string.Join(", ", p.SignatureTraits)}");
        if (p.NarrativeThemes is { Length: > 0 }) sb.AppendLine($"NarrativeThemes: {string.Join(", ", p.NarrativeThemes)}");
        if (p.DomainExpertise is { Length: > 0 }) sb.AppendLine($"DomainExpertise: {string.Join(", ", p.DomainExpertise)}");
        return sb.ToString();
    }

    private static string BuildToolIndexSection(IEnumerable<Tool> tools)
    {
        var list = tools?.ToList() ?? new List<Tool>();
        if (list.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine("Tool Index:");
        sb.AppendLine("- You may optionally call exactly one tool by returning pure JSON only on a single line:");
        sb.AppendLine("  {\"toolId\":\"<GUID>\",\"args\":{...}} OR {\"toolName\":\"<NAME>\",\"args\":{...}}");
        sb.AppendLine("- If no tool is needed, answer normally without JSON.");
        sb.AppendLine("- Arguments must match the schema below (required marked with *).");
        sb.AppendLine("Available tools:");
        foreach (var t in list)
        {
            sb.Append("- ").Append(t.Name).Append(" (id: ").Append(t.Id).Append(")");
            if (!string.IsNullOrWhiteSpace(t.Description)) sb.Append(" - ").Append(t.Description);
            var inputs = t.Parameters.Where(p => p.Direction == Cognition.Data.Relational.Modules.Common.ToolParamDirection.Input).ToList();
            if (inputs.Count > 0)
            {
                sb.Append(" | args: ");
                sb.Append(string.Join(", ", inputs.Select(p => $"{p.Name}:{p.Type}{(p.Required ? "*" : "")}")));
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
