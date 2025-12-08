using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Models.Conversations;

namespace Cognition.Api.Services;

public interface IConversationAccessService
{
    Task<IReadOnlyList<ConversationListItem>> ListAsync(Guid callerUserId, bool isAdmin, Guid? participantId, Guid? agentId, CancellationToken cancellationToken);
}
