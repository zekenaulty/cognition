using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Models.Conversations;

namespace Cognition.Api.Services;

public interface IConversationFactory
{
    Task<Guid> CreateAsync(CreateConversationRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken);
}
