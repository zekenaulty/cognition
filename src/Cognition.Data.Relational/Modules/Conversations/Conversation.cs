using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class Conversation : BaseEntity
{
    public string? Title { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }

    public List<ConversationParticipant> Participants { get; set; } = [];
    public List<ConversationMessage> Messages { get; set; } = [];
    public List<ConversationSummary> Summaries { get; set; } = [];
}
