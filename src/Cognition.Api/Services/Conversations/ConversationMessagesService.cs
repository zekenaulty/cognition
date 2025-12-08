using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Models.Conversations;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Services.Conversations;

public sealed class ConversationMessagesService : IConversationMessagesService
{
    private readonly CognitionDbContext _db;

    public ConversationMessagesService(CognitionDbContext db)
    {
        _db = db;
    }

    public Task<bool> ConversationExistsAsync(Guid conversationId, CancellationToken cancellationToken) =>
        _db.Conversations.AsNoTracking().AnyAsync(c => c.Id == conversationId, cancellationToken);

    public async Task<bool> ValidateUserMessageAsync(Guid userId, AddMessageRequest req, CancellationToken cancellationToken)
    {
        if (req.Role != ChatRole.User) return true;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null || user.PrimaryPersonaId == null) return false;
        return user.PrimaryPersonaId.Value == req.FromPersonaId;
    }

    public async Task<Guid> AddMessageAsync(Guid conversationId, Guid userId, AddMessageRequest req, CancellationToken cancellationToken)
    {
        var content = req.Content.Trim();
        var metatype = string.IsNullOrWhiteSpace(req.Metatype) ? null : req.Metatype.Trim();
        var fromAgentId = await _db.Agents.AsNoTracking()
            .Where(a => a.PersonaId == req.FromPersonaId)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var msg = new ConversationMessage
        {
            ConversationId = conversationId,
            FromPersonaId = req.FromPersonaId,
            FromAgentId = fromAgentId,
            ToPersonaId = req.ToPersonaId,
            Role = req.Role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            CreatedByUserId = userId,
            Metatype = metatype
        };
        _db.ConversationMessages.Add(msg);
        await _db.SaveChangesAsync(cancellationToken);

        var versionEntity = new ConversationMessageVersion
        {
            ConversationMessageId = msg.Id,
            VersionIndex = 0,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ConversationMessageVersions.Add(versionEntity);
        msg.ActiveVersionIndex = 0;
        await _db.SaveChangesAsync(cancellationToken);

        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conv != null)
        {
            conv.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return msg.Id;
    }

    public async Task<(Guid messageId, int versionIndex)> AddVersionAsync(Guid conversationId, Guid messageId, AddVersionRequest req, CancellationToken cancellationToken)
    {
        var msg = await _db.ConversationMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);
        if (msg == null) throw new InvalidOperationException("conversation_message_not_found");
        var maxIndex = await _db.ConversationMessageVersions.Where(v => v.ConversationMessageId == messageId).Select(v => (int?)v.VersionIndex).MaxAsync(cancellationToken) ?? -1;
        var next = maxIndex + 1;
        var content = req.Content.Trim();
        var v = new ConversationMessageVersion
        {
            ConversationMessageId = messageId,
            VersionIndex = next,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ConversationMessageVersions.Add(v);
        msg.ActiveVersionIndex = next;
        msg.Content = content;
        await _db.SaveChangesAsync(cancellationToken);
        return (msg.Id, next);
    }

    public async Task<(Guid messageId, int versionIndex)> SetActiveVersionAsync(Guid conversationId, Guid messageId, int targetIndex, CancellationToken cancellationToken)
    {
        var msg = await _db.ConversationMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);
        if (msg == null) throw new InvalidOperationException("conversation_message_not_found");
        var target = await _db.ConversationMessageVersions.AsNoTracking().FirstOrDefaultAsync(v => v.ConversationMessageId == messageId && v.VersionIndex == targetIndex, cancellationToken);
        if (target == null) throw new InvalidOperationException("version_not_found");
        msg.ActiveVersionIndex = targetIndex;
        msg.Content = target.Content;
        await _db.SaveChangesAsync(cancellationToken);
        return (msg.Id, targetIndex);
    }
}
