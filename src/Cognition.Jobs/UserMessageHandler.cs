using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Rebus.Handlers;

namespace Cognition.Jobs
{
    public class UserMessageHandler : IHandleMessages<UserMessageAppended>
    {
        private readonly CognitionDbContext _db;
        private readonly Rebus.Bus.IBus _bus;
        private readonly WorkflowEventLogger _logger;

        public UserMessageHandler(CognitionDbContext db, Rebus.Bus.IBus bus, WorkflowEventLogger logger)
        {
            _db = db;
            _bus = bus;
            _logger = logger;
        }

        public async Task Handle(UserMessageAppended message)
        {
            await _logger.LogAsync(message.ConversationId, nameof(UserMessageAppended), JObject.FromObject(message)).ConfigureAwait(false);

            var conversation = await _db.Conversations
                .Include(c => c.Agent)
                .FirstOrDefaultAsync(c => c.Id == message.ConversationId)
                .ConfigureAwait(false);
            if (conversation is null || conversation.Metadata is null)
            {
                return;
            }

            if (!TryReadGuid(conversation.Metadata, "fictionPlanId", out var fictionPlanId))
            {
                return;
            }

            var branchSlug = TryReadString(conversation.Metadata, "fictionBranchSlug") ?? "main";
            Guid? providerId = null;
            if (TryReadGuid(conversation.Metadata, "plannerProviderId", out var providerValue))
            {
                providerId = providerValue;
            }
            else
            {
                providerId = conversation.Agent?.ClientProfile?.ProviderId;
            }

            Guid? modelId = null;
            if (TryReadGuid(conversation.Metadata, "plannerModelId", out var modelValue))
            {
                modelId = modelValue;
            }
            else
            {
                modelId = conversation.Agent?.ClientProfile?.ModelId;
            }

            if (!providerId.HasValue)
            {
                return;
            }

            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["source"] = "user-message-handler"
            };

            var planRequested = new PlanRequested(
                message.ConversationId,
                conversation.AgentId,
                message.PersonaId,
                providerId.Value,
                modelId,
                message.Input,
                1,
                1,
                fictionPlanId,
                branchSlug,
                metadata);

            await _bus.Publish(planRequested).ConfigureAwait(false);
        }

        private static bool TryReadGuid(IReadOnlyDictionary<string, object?> metadata, string key, out Guid value)
        {
            value = Guid.Empty;
            if (!metadata.TryGetValue(key, out var raw) || raw is null)
            {
                return false;
            }

            if (raw is Guid guid)
            {
                value = guid;
                return true;
            }

            if (Guid.TryParse(raw.ToString(), out guid))
            {
                value = guid;
                return true;
            }

            return false;
        }

        private static string? TryReadString(IReadOnlyDictionary<string, object?> metadata, string key)
        {
            if (!metadata.TryGetValue(key, out var raw) || raw is null)
            {
                return null;
            }

            return raw.ToString();
        }

    }
}
