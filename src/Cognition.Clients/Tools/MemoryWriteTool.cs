using Cognition.Data.Relational;

namespace Cognition.Clients.Tools;

public class MemoryWriteTool : ITool
{
    public string Name => "Memory Write";
    public string ClassPath => typeof(MemoryWriteTool).FullName! + ", " + typeof(MemoryWriteTool).Assembly.GetName().Name;

    private readonly CognitionDbContext _db;
    public MemoryWriteTool(CognitionDbContext db) => _db = db;

    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var text = (args.TryGetValue("text", out var v) ? v as string : null) ?? string.Empty;
        if (ctx.PersonaId is null) throw new InvalidOperationException("PersonaId required");
        _db.ConversationSummaries.Add(new Cognition.Data.Relational.Modules.Conversations.ConversationSummary
        {
            ByPersonaId = ctx.PersonaId.Value,
            Content = text,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ctx.Ct);
        return new { ok = true };
    }
}

