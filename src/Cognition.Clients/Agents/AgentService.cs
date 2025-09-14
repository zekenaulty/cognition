using System.Text;
using Cognition.Clients.LLM;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Clients.Agents;

public interface IAgentService
{
    Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false);
}

public class AgentService : IAgentService
{
    private readonly CognitionDbContext _db;
    private readonly ILLMClientFactory _factory;

    public AgentService(CognitionDbContext db, ILLMClientFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    public async Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false)
    {
        var persona = await _db.Personas.FirstAsync(p => p.Id == personaId);
        var sys = BuildSystemMessage(persona, rolePlay);
        var client = await _factory.CreateAsync(providerId, modelId);
        var prompt = string.IsNullOrWhiteSpace(sys) ? input : $"{sys}\n\nUser: {input}";
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
}

