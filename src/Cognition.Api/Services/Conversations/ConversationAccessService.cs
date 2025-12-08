using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Api.Models.Conversations;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Services.Conversations;

public sealed class ConversationAccessService : IConversationAccessService
{
    private readonly CognitionDbContext _db;
    private readonly IConversationSettingsService _settings;

    public ConversationAccessService(CognitionDbContext db, IConversationSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IReadOnlyList<ConversationListItem>> ListAsync(Guid callerUserId, bool isAdmin, Guid? participantId, Guid? agentId, CancellationToken cancellationToken)
    {
        var q = _db.Conversations.AsNoTracking().AsQueryable();

        var linkedIds = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == callerUserId)
            .Select(up => up.PersonaId)
            .ToListAsync(cancellationToken);
        var primaryId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == callerUserId)
            .Select(u => u.PrimaryPersonaId)
            .FirstOrDefaultAsync(cancellationToken);
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);

        Guid? defaultSystemId = await _db.Personas.AsNoTracking()
            .Where(p => p.OwnedBy == Cognition.Data.Relational.Modules.Personas.OwnedBy.System && p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.Assistant)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (agentId.HasValue)
        {
            q = q.Where(c => c.AgentId == agentId.Value);
        }
        else if (participantId.HasValue)
        {
            var pid = participantId.Value;
            var pidIsAllowed = allowedPersonaIds.Contains(pid) || (defaultSystemId.HasValue && pid == defaultSystemId.Value);
            if (!pidIsAllowed) return Array.Empty<ConversationListItem>();

            if (!isAdmin && defaultSystemId.HasValue && pid == defaultSystemId.Value)
            {
                q = q.Where(c =>
                    _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.PersonaId == pid)
                    && _db.ConversationMessages.Any(m => m.ConversationId == c.Id && m.CreatedByUserId == callerUserId));
            }
            else
            {
                q = q.Where(c => _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.PersonaId == pid));
            }
        }
        else
        {
            q = q.Where(c =>
                _db.ConversationMessages.Any(m => m.ConversationId == c.Id && m.CreatedByUserId == callerUserId)
                || _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && allowedPersonaIds.Contains(p.PersonaId))
            );
        }

        var conversations = await q
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = conversations
            .Select(c =>
            {
                var provider = _settings.TryReadMetadataGuid(c.Metadata, "providerId");
                var model = _settings.TryReadMetadataGuid(c.Metadata, "modelId");
                return new ConversationListItem(c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc, provider, model);
            })
            .ToList();

        return items;
    }
}
