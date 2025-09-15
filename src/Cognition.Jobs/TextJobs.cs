using Cognition.Clients.Agents;

namespace Cognition.Jobs;

public class TextJobs
{
    private readonly IAgentService _agents;

    public TextJobs(IAgentService agents)
    {
        _agents = agents;
    }

    public async Task<string> ChatOnce(Guid conversationId, Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay, CancellationToken ct = default)
    {
        // Delegate to agent service which persists messages and returns reply
        var reply = await _agents.ChatAsync(conversationId, personaId, providerId, modelId, input, rolePlay, ct);
        return reply;
    }
}

