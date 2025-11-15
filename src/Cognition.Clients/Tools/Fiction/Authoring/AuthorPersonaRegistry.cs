using System.Collections.ObjectModel;
using System.Text;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Fiction.Authoring;

public sealed record AuthorPersonaContext(
    Guid PersonaId,
    string PersonaName,
    string Summary,
    IReadOnlyList<string> Memories,
    IReadOnlyList<string> WorldNotes);

public sealed record AuthorPersonaMemoryEntry(
    string Title,
    string Content,
    Guid PlanId,
    Guid? ScrollId = null,
    Guid? SceneId = null,
    string SourcePhase = "unknown");

public interface IAuthorPersonaRegistry
{
    Task<AuthorPersonaContext?> GetForPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task AppendMemoryAsync(Guid personaId, AuthorPersonaMemoryEntry entry, CancellationToken cancellationToken = default);
}

public sealed class AuthorPersonaRegistry : IAuthorPersonaRegistry
{
    private readonly CognitionDbContext _db;
    private readonly ILogger<AuthorPersonaRegistry> _logger;
    private readonly AuthorPersonaOptions _options;

    public AuthorPersonaRegistry(
        CognitionDbContext db,
        IOptions<AuthorPersonaOptions> options,
        ILogger<AuthorPersonaRegistry> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AuthorPersonaContext?> GetForPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .Include(p => p.FictionProject)
            .Include(p => p.CurrentConversationPlan!)
                .ThenInclude(cp => cp.Persona)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan?.CurrentConversationPlan?.Persona is null)
        {
            _logger.LogWarning("Author persona context unavailable for plan {PlanId}.", planId);
            return null;
        }

        var persona = plan.CurrentConversationPlan.Persona;
        var summary = BuildPersonaSummary(persona, plan.FictionProject?.Title ?? plan.Name);
        var memories = await LoadPersonaMemoriesAsync(persona.Id, cancellationToken).ConfigureAwait(false);
        var worldNotes = await LoadWorldNotesAsync(planId, cancellationToken).ConfigureAwait(false);

        return new AuthorPersonaContext(
            persona.Id,
            persona.Name,
            summary,
            memories,
            worldNotes);
    }

    public async Task AppendMemoryAsync(Guid personaId, AuthorPersonaMemoryEntry entry, CancellationToken cancellationToken = default)
    {
        var memory = new PersonaMemory
        {
            PersonaId = personaId,
            Title = entry.Title,
            Content = entry.Content,
            Source = entry.SourcePhase,
            RecordedAtUtc = DateTime.UtcNow,
            Properties = BuildProperties(entry)
        };

        _db.PersonaMemories.Add(memory);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<string>> LoadPersonaMemoriesAsync(Guid personaId, CancellationToken cancellationToken)
    {
        var query = _db.PersonaMemories
            .AsNoTracking()
            .Where(m => m.PersonaId == personaId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .Take(Math.Max(1, _options.MemoryWindow));

        var results = await query
            .Select(m => new { m.Title, m.Content })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (results.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = results
            .Select(m => string.IsNullOrWhiteSpace(m.Title)
                ? TrimMemoryText(m.Content)
                : $"{m.Title}: {TrimMemoryText(m.Content)}")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return new ReadOnlyCollection<string>(lines);
    }

    private async Task<IReadOnlyList<string>> LoadWorldNotesAsync(Guid planId, CancellationToken cancellationToken)
    {
        var entries = await _db.FictionWorldBibleEntries
            .AsNoTracking()
            .Where(entry => entry.FictionWorldBible != null && entry.FictionWorldBible.FictionPlanId == planId)
            .OrderByDescending(entry => entry.UpdatedAtUtc)
            .Take(Math.Max(1, _options.WorldNotesWindow))
            .Select(entry => new { entry.EntryName, entry.Content.Summary })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var lines = entries
            .Select(e => $"{e.EntryName}: {e.Summary}")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        return new ReadOnlyCollection<string>(lines);
    }

    private static string BuildPersonaSummary(Persona persona, string projectTitle)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Author persona \"{persona.Name}\" for project \"{projectTitle}\".");

        if (!string.IsNullOrWhiteSpace(persona.Voice))
        {
            builder.AppendLine($"Voice: {persona.Voice}");
        }

        if (!string.IsNullOrWhiteSpace(persona.CommunicationStyle))
        {
            builder.AppendLine($"Style: {persona.CommunicationStyle}");
        }

        if (!string.IsNullOrWhiteSpace(persona.Essence))
        {
            builder.AppendLine($"Essence: {persona.Essence}");
        }

        if (!string.IsNullOrWhiteSpace(persona.Background))
        {
            builder.AppendLine($"Background: {persona.Background}");
        }

        if (!string.IsNullOrWhiteSpace(persona.Beliefs))
        {
            builder.AppendLine($"Beliefs: {persona.Beliefs}");
        }

        return builder.ToString().Trim();
    }

    private static string TrimMemoryText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const int limit = 280;
        return value.Length <= limit ? value : value[..limit] + "...";
    }

    private static Dictionary<string, object?>? BuildProperties(AuthorPersonaMemoryEntry entry)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = entry.PlanId
        };

        if (entry.ScrollId.HasValue)
        {
            properties["scrollId"] = entry.ScrollId;
        }

        if (entry.SceneId.HasValue)
        {
            properties["sceneId"] = entry.SceneId;
        }

        return properties.Count > 0 ? properties : null;
    }
}
