using System.Diagnostics;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Clients.Tools;

public class ToolDispatcher : IToolDispatcher
{
    private readonly CognitionDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly IToolRegistry _registry;

    public ToolDispatcher(CognitionDbContext db, IServiceProvider sp, IToolRegistry registry)
    {
        _db = db; _sp = sp; _registry = registry;
    }

    public async Task<(bool ok, object? result, string? error)> ExecuteAsync(
        Guid toolId, ToolContext ctx, IDictionary<string, object?> args, bool log = true)
    {
        var tool = await _db.Tools.Include(t => t.Parameters).FirstAsync(t => t.Id == toolId);
        var sw = Stopwatch.StartNew();
        object? result = null; string? error = null; var ok = true;

        try
        {
            // Resolve implementation by ClassPath through the safe registry
            if (!_registry.TryResolveByClassPath(tool.ClassPath, out var implType))
                throw new InvalidOperationException($"Tool impl not registered/known: {tool.ClassPath}");
            var impl = (ITool?)_sp.GetService(implType);
            if (impl is null)
                throw new InvalidOperationException($"Tool impl not registered in DI: {tool.ClassPath}");

            // Validate/prepare args from DB parameter schema
            foreach (var p in tool.Parameters.Where(p => p.Direction.ToString() == "Input"))
            {
                if (!args.ContainsKey(p.Name) || args[p.Name] is null)
                {
                    if (p.Required) throw new ArgumentException($"Missing required parameter '{p.Name}'");
                    if (p.DefaultValue is not null)
                    {
                        // DefaultValue stored as JSONB -> Dictionary<string, object?> or scalar
                        args[p.Name] = p.DefaultValue.Count == 1 && p.DefaultValue.ContainsKey("value")
                            ? p.DefaultValue["value"]
                            : p.DefaultValue;
                    }
                }
            }

            result = await impl.ExecuteAsync(ctx, args);
            ok = true; error = null;
        }
        catch (Exception ex)
        {
            ok = false; error = ex.Message; result = null;
        }
        // log and return
        if (log)
        {
            _db.ToolExecutionLogs.Add(new ToolExecutionLog
            {
                ToolId = tool.Id,
                AgentId = ctx.AgentId,
                StartedAtUtc = DateTime.UtcNow,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = ok,
                Request = args as Dictionary<string, object?>,
                Response = ok ? (result as Dictionary<string, object?>) ?? new() { { "value", result } } : null,
                Error = error,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return (ok, result, error);
    }
}
