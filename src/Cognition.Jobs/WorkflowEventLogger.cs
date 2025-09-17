using System;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs
{
    public class WorkflowEventLogger
    {
        private readonly CognitionDbContext _db;
        private readonly bool _enabled;
        public WorkflowEventLogger(CognitionDbContext db, bool enabled)
        {
            _db = db;
            _enabled = enabled;
        }

        public async Task LogAsync(Guid conversationId, string kind, JObject payload)
        {
            if (!_enabled) return;
            var evt = new WorkflowEvent
            {
                ConversationId = conversationId,
                Kind = kind,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };
            _db.WorkflowEvents.Add(evt);
            await _db.SaveChangesAsync();
        }
    }
}
