using System.Linq;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Planning;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Planning;

public sealed class PlannerTranscriptStore : IPlannerTranscriptStore
{
    private readonly CognitionDbContext _db;
    private readonly ILogger<PlannerTranscriptStore> _logger;

    public PlannerTranscriptStore(CognitionDbContext db, ILogger<PlannerTranscriptStore> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StoreAsync(PlannerContext context, PlannerMetadata metadata, PlannerResult result, CancellationToken ct)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (metadata is null) throw new ArgumentNullException(nameof(metadata));
        if (result is null) throw new ArgumentNullException(nameof(result));

        try
        {
            var record = new PlannerExecution
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                PlannerName = metadata.Name,
                Outcome = result.Outcome.ToString(),
                ToolId = context.ToolId,
                AgentId = context.ToolContext.AgentId,
                ConversationId = context.ToolContext.ConversationId,
                PrimaryAgentId = context.PrimaryAgentId,
                Environment = context.Environment,
                ScopePath = context.ScopePath?.ToString(),
                ConversationState = context.ConversationState.Count == 0 ? null : context.ConversationState.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                Artifacts = result.Artifacts.Count == 0 ? null : result.Artifacts.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                Metrics = result.Metrics.Count == 0 ? null : result.Metrics.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                Diagnostics = result.Diagnostics.Count == 0 ? null : result.Diagnostics.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                Transcript = result.Transcript.Count == 0
                    ? null
                    : result.Transcript.Select(entry => new PlannerExecutionTranscriptEntry
                    {
                        TimestampUtc = entry.TimestampUtc,
                        Role = entry.Role,
                        Message = entry.Message,
                        Metadata = entry.Metadata is null ? null : entry.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                    }).ToList()
            };

            _db.PlannerExecutions.Add(record);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist planner transcript for {PlannerName}.", metadata.Name);
        }
    }
}
