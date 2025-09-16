using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace Cognition.Data.Relational.Modules.Conversations
{
    [Table("conversation_workflow_states")]
    public class ConversationWorkflowState
    {
        [Key]
        [ForeignKey("Conversation")]
        public Guid ConversationId { get; set; }
        public string Stage { get; set; } = "";
        public int Pointer { get; set; }
        public JObject Blackboard { get; set; } = new JObject();
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Conversation? Conversation { get; set; }
    }
}
