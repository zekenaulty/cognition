using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Api.Models.Conversations;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Api.Services;

public interface IConversationMessagesService
{
    Task<bool> ConversationExistsAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<bool> ValidateUserMessageAsync(Guid userId, AddMessageRequest req, CancellationToken cancellationToken);
    Task<Guid> AddMessageAsync(Guid conversationId, Guid userId, AddMessageRequest req, CancellationToken cancellationToken);
    Task<(Guid messageId, int versionIndex)> AddVersionAsync(Guid conversationId, Guid messageId, AddVersionRequest req, CancellationToken cancellationToken);
    Task<(Guid messageId, int versionIndex)> SetActiveVersionAsync(Guid conversationId, Guid messageId, int targetIndex, CancellationToken cancellationToken);
}
