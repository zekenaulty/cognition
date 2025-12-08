using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Cognition.Api.Models.Conversations;

namespace Cognition.Api.Services.Conversations;

public sealed class ConversationFactory : IConversationFactory
{
    private readonly CognitionDbContext _db;
    private readonly IConversationSettingsService _settings;

    public ConversationFactory(CognitionDbContext db, IConversationSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<Guid> CreateAsync(CreateConversationRequest req, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var agentExists = await _db.Agents.AsNoTracking().AnyAsync(a => a.Id == req.AgentId, cancellationToken).ConfigureAwait(false);
        if (!agentExists)
        {
            throw new InvalidOperationException("agent_not_found");
        }

        var conv = new Conversation
        {
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            AgentId = req.AgentId,
            Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };

        // Seed provider/model from agent's client profile if available, otherwise global defaults
        var agentProfile = await _db.Agents
            .AsNoTracking()
            .Include(a => a.ClientProfile)
            .Where(a => a.Id == req.AgentId)
            .Select(a => a.ClientProfile)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agentProfile is not null)
        {
            conv.Metadata["providerId"] = agentProfile.ProviderId;
            if (agentProfile.ModelId.HasValue)
            {
                conv.Metadata["modelId"] = agentProfile.ModelId.Value;
            }
        }
        else
        {
            var defaults = await _settings.ResolveSettingsAsync(conv, cancellationToken).ConfigureAwait(false);
            if (defaults.ProviderId.HasValue)
            {
                conv.Metadata["providerId"] = defaults.ProviderId.Value;
            }
            if (defaults.ModelId.HasValue)
            {
                conv.Metadata["modelId"] = defaults.ModelId.Value;
            }
        }

        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(cancellationToken);

        // participants (optional)
        var participants = (req.ParticipantIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        // Always include the caller's primary persona as a participant if available
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var caller))
        {
            var primaryId = await _db.Users.AsNoTracking().Where(u => u.Id == caller).Select(u => u.PrimaryPersonaId).FirstOrDefaultAsync(cancellationToken);
            if (primaryId.HasValue && !participants.Contains(primaryId.Value)) participants.Add(primaryId.Value);
        }
        if (participants.Count == 0)
        {
            var defaultPid = await _db.Personas
                .AsNoTracking()
                .Where(p => p.OwnedBy == Cognition.Data.Relational.Modules.Personas.OwnedBy.System && p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.Assistant)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (defaultPid.HasValue) participants.Add(defaultPid.Value);
        }
        foreach (var pid in participants)
        {
            _db.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conv.Id,
                PersonaId = pid,
                JoinedAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return conv.Id;
    }
}
