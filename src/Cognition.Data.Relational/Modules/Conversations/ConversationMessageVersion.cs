using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationMessageVersion : BaseEntity
{
    public Guid ConversationMessageId { get; set; }
    public ConversationMessage ConversationMessage { get; set; } = null!;

    public int VersionIndex { get; set; }
    public string Content { get; set; } = string.Empty;
}

