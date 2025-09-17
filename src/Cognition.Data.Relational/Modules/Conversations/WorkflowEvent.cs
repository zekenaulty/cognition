using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace Cognition.Data.Relational.Modules.Conversations
{
    [Table("workflow_events")]
    public class WorkflowEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public string Kind { get; set; } = "";
        public JObject Payload { get; set; } = new JObject();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
