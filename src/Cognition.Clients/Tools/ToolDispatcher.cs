using System.Diagnostics;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using Cognition.Data.Relational.Modules.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools;

public class ToolDispatcher : IToolDispatcher
{
    private readonly CognitionDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly IToolRegistry _registry;
    private readonly ILogger<ToolDispatcher> _logger;
    private readonly IScopePathBuilder _scopePathBuilder;

    public ToolDispatcher(
        CognitionDbContext db,
        IServiceProvider sp,
        IToolRegistry registry,
        ILogger<ToolDispatcher> logger,
        IScopePathBuilder scopePathBuilder)
    {
        _db = db;
        _sp = sp;
        _registry = registry;
        _logger = logger;
        _scopePathBuilder = scopePathBuilder ?? throw new ArgumentNullException(nameof(scopePathBuilder));
    }

    public async Task<(bool ok, PlannerResult? result, string? error)> ExecutePlannerAsync(
        Guid toolId,
        PlannerContext plannerContext,
        PlannerParameters parameters,
        bool log = true)
    {
        if (plannerContext is null) throw new ArgumentNullException(nameof(plannerContext));
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));

        var tool = await _db.Tools
            .Include(t => t.Parameters)
            .Include(t => t.ClientProfile)
            .FirstAsync(t => t.Id == toolId);

        var args = new Dictionary<string, object?>(parameters.AsReadOnlyDictionary(), StringComparer.OrdinalIgnoreCase);
        var sw = Stopwatch.StartNew();
        PlannerResult? result = null;
        string? error = null;
        var ok = true;

        try
        {
            if (!_registry.TryResolveByClassPath(tool.ClassPath, out var implType))
                throw new InvalidOperationException($"Tool impl not registered/known: {tool.ClassPath}");

            if (!typeof(IPlannerTool).IsAssignableFrom(implType))
                throw new InvalidOperationException($"Tool {tool.ClassPath} does not implement IPlannerTool.");

            var impl = (IPlannerTool?)_sp.GetService(implType);
            if (impl is null)
                throw new InvalidOperationException($"Tool impl not registered in DI: {tool.ClassPath}");

            var inputParams = tool.Parameters.Where(p => p.Direction == ToolParamDirection.Input).ToList();
            var allowed = new HashSet<string>(inputParams.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var extras = args.Keys.Where(k => !allowed.Contains(k)).ToList();
            if (extras.Count > 0)
                throw new ArgumentException($"Unknown parameter(s): {string.Join(", ", extras)}");

            foreach (var p in inputParams)
            {
                if (!args.ContainsKey(p.Name) || args[p.Name] is null)
                {
                    if (p.Required) throw new ArgumentException($"Missing required parameter '{p.Name}'");
                    if (p.DefaultValue is not null)
                    {
                        args[p.Name] = p.DefaultValue.Count == 1 && p.DefaultValue.ContainsKey("value")
                            ? p.DefaultValue["value"]
                            : p.DefaultValue;
                    }
                }

                if (args.ContainsKey(p.Name))
                {
                    args[p.Name] = CoerceToType(JsonToDotNet(args[p.Name]), p.Type);
                }
            }

            EnsureProviderModelArgs(tool, plannerContext.ToolContext, args);

            var plannerParameters = new PlannerParameters(args);
            var scopePath = plannerContext.ScopePath;
            if (scopePath is null &&
                _scopePathBuilder.TryBuild(
                    tenantId: null,
                    appId: null,
                    personaId: plannerContext.ToolContext.PersonaId,
                    agentId: plannerContext.ToolContext.AgentId,
                    conversationId: plannerContext.ToolContext.ConversationId,
                    projectId: null,
                    worldId: null,
                    out var inferredPath))
            {
                scopePath = inferredPath;
            }

            var enrichedContext = scopePath is null ? plannerContext : plannerContext with { ScopePath = scopePath };

            var ctxWithToolId = enrichedContext.ToolId == tool.Id
                ? enrichedContext
                : enrichedContext with { ToolId = tool.Id };

            _logger.LogInformation("Executing planner {ToolName} ({ToolId}) class {ClassPath} conv {ConversationId}", tool.Name, tool.Id, tool.ClassPath, plannerContext.ToolContext.ConversationId);
            result = await impl.PlanAsync(ctxWithToolId, plannerParameters, plannerContext.ToolContext.Ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ok = false;
            error = ex.Message;
            result = null;
            _logger.LogError(ex, "Planner execution failed for {ToolName} ({ToolId})", tool.Name, tool.Id);
        }

        sw.Stop();

        if (log)
        {
            var startedAt = DateTime.UtcNow - sw.Elapsed;
            var redacted = RedactArgs(args);
            _db.ToolExecutionLogs.Add(new ToolExecutionLog
            {
                ToolId = tool.Id,
                AgentId = plannerContext.ToolContext.AgentId,
                StartedAtUtc = startedAt,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = ok,
                Request = redacted,
                Response = ok ? result?.ToDictionary() : null,
                Error = ok ? null : error
            });
            await _db.SaveChangesAsync(plannerContext.ToolContext.Ct);
        }

        _logger.LogInformation("Planner {ToolName} ({ToolId}) completed success={Success} durationMs={Duration}", tool.Name, tool.Id, ok, (int)sw.ElapsedMilliseconds);
        return (ok, result, error);
    }

    public async Task<(bool ok, object? result, string? error)> ExecuteAsync(
        Guid toolId, ToolContext ctx, IDictionary<string, object?> args, bool log = true)
    {
        var tool = await _db.Tools
            .Include(t => t.Parameters)
            .Include(t => t.ClientProfile)
            .FirstAsync(t => t.Id == toolId);
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
            var inputParams = tool.Parameters.Where(p => p.Direction == ToolParamDirection.Input).ToList();
            // Reject unknown args early
            var allowed = new HashSet<string>(inputParams.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
            var extras = args.Keys.Where(k => !allowed.Contains(k)).ToList();
            if (extras.Count > 0)
                throw new ArgumentException($"Unknown parameter(s): {string.Join(", ", extras)}");

            foreach (var p in inputParams)
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
                // Light type coercion from declared param type
                if (args.ContainsKey(p.Name))
                {
                    args[p.Name] = CoerceToType(JsonToDotNet(args[p.Name]), p.Type);
                    // Enforce enum/options if provided
                    if (p.Options is not null && p.Options.TryGetValue("values", out var optsObj) && optsObj is not null)
                    {
                        var allowedValues = FlattenValues(optsObj);
                        var vStr = args[p.Name]?.ToString();
                        if (allowedValues.Count > 0 && vStr is not null && !allowedValues.Contains(vStr, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new ArgumentException($"Parameter '{p.Name}' must be one of: {string.Join(", ", allowedValues)}");
                        }
                    }
                }
            }

            // Default provider/model selection: prefer Tool.ClientProfile, then Agent.ClientProfile, then OpenAI gpt-4o
            EnsureProviderModelArgs(tool, ctx, args);

            _logger.LogInformation("Executing tool {ToolName} ({ToolId}) class {ClassPath} conv {ConversationId} persona {PersonaId}", tool.Name, tool.Id, tool.ClassPath, ctx.ConversationId, ctx.PersonaId);
            result = await impl.ExecuteAsync(ctx, args);
            ok = true; error = null;
        }
        catch (Exception ex)
        {
            ok = false; error = ex.Message; result = null;
            _logger.LogError(ex, "Tool execution failed for {ToolName} ({ToolId})", tool.Name, tool.Id);
        }
        // log and return
        if (log)
        {
            var startedAt = DateTime.UtcNow - sw.Elapsed;
            var redacted = RedactArgs(args);
            _db.ToolExecutionLogs.Add(new ToolExecutionLog
            {
                ToolId = tool.Id,
                AgentId = ctx.AgentId,
                StartedAtUtc = startedAt,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = ok,
                Request = redacted,
                Response = ok ? (result as Dictionary<string, object?>) ?? new() { { "value", result } } : null,
                Error = error,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ctx.Ct);
        }
        _logger.LogInformation("Tool {ToolName} ({ToolId}) completed success={Success} durationMs={Duration}", tool.Name, tool.Id, ok, (int)sw.ElapsedMilliseconds);

        return (ok, result, error);
    }

    private void EnsureProviderModelArgs(Tool tool, ToolContext ctx, IDictionary<string, object?> args)
    {
        bool hasProvider = args.ContainsKey("providerId") && args["providerId"] != null;
        bool hasModel = args.ContainsKey("modelId") && args["modelId"] != null;
        if (hasProvider && hasModel) return;

        Guid? providerId = null; Guid? modelId = null;
        // Tool-level profile
        if (tool.ClientProfileId.HasValue && tool.ClientProfile is not null)
        {
            providerId = tool.ClientProfile.ProviderId;
            modelId = tool.ClientProfile.ModelId;
        }
        // Agent-level profile
        if ((!providerId.HasValue || !modelId.HasValue) && ctx.AgentId.HasValue)
        {
            var agent = _db.Agents.Include(a => a.ClientProfile).FirstOrDefault(a => a.Id == ctx.AgentId.Value);
            if (agent?.ClientProfileId != null && agent.ClientProfile is not null)
            {
                providerId ??= agent.ClientProfile.ProviderId;
                modelId ??= agent.ClientProfile.ModelId;
            }
        }
        // Global default: OpenAI gpt-4o
        if (!providerId.HasValue || !modelId.HasValue)
        {
            var openai = _db.Providers.AsNoTracking().FirstOrDefault(p => p.Name.ToLower() == "openai");
            if (openai != null)
            {
                providerId ??= openai.Id;
                var gpt4o = _db.Models.AsNoTracking().FirstOrDefault(m => m.ProviderId == openai.Id && m.Name.ToLower() == "gpt-4o");
                if (gpt4o != null) modelId ??= gpt4o.Id;
            }
        }

        if (providerId.HasValue && !args.ContainsKey("providerId")) args["providerId"] = providerId.Value;
        if (modelId.HasValue && !args.ContainsKey("modelId")) args["modelId"] = modelId.Value;
    }

    private static List<string> FlattenValues(object opts)
    {
        // Normalize various shapes (array, list, JsonElement) into strings for comparison
        var list = new List<string>();
        switch (opts)
        {
            case string s:
                list.Add(s);
                break;
            case IEnumerable<object?> en:
                foreach (var o in en) if (o != null) list.Add(o.ToString()!);
                break;
            case System.Text.Json.JsonElement je:
                if (je.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var el in je.EnumerateArray()) list.Add(el.ToString());
                }
                else if (je.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    list.Add(je.ToString());
                }
                break;
        }
        return list;
    }

    private static Dictionary<string, object?>? RedactArgs(IDictionary<string, object?>? src)
    {
        if (src is null) return null;
        var copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in src)
        {
            copy[kv.Key] = RedactValue(kv.Key, kv.Value);
        }
        return copy;
    }

    private static object? RedactValue(string key, object? value)
    {
        if (value is null) return null;
        var k = key.ToLowerInvariant();
        bool sensitive = k.Contains("apikey") || k.Contains("api_key") || k.Contains("token") || k.Contains("password") || k.Contains("secret") || k == "authorization" || k == "auth";
        if (sensitive)
            return "***";
        // Recurse into nested structures
        if (value is Dictionary<string, object?> d)
        {
            var inner = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in d) inner[kv.Key] = RedactValue(kv.Key, kv.Value);
            return inner;
        }
        if (value is System.Text.Json.JsonElement je)
        {
            return JsonToDotNet(je);
        }
        return value;
    }

    private static object? JsonToDotNet(object? value)
    {
        if (value is System.Text.Json.JsonElement je)
        {
            switch (je.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    return je.GetString();
                case System.Text.Json.JsonValueKind.Number:
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetRawText();
                case System.Text.Json.JsonValueKind.True: return true;
                case System.Text.Json.JsonValueKind.False: return false;
                case System.Text.Json.JsonValueKind.Null: return null;
                case System.Text.Json.JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var el in je.EnumerateArray()) list.Add(JsonToDotNet(el));
                    return list.ToArray();
                case System.Text.Json.JsonValueKind.Object:
                    return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(je.GetRawText());
                default:
                    return je.GetRawText();
            }
        }
        return value;
    }

    private static object? CoerceToType(object? value, string type)
    {
        if (value is null) return null;
        var t = (type ?? string.Empty).Trim().ToLowerInvariant();
        try
        {
            return t switch
            {
                "string" => value.ToString(),
                "int" or "int32" => ToInt(value),
                "long" or "int64" => ToLong(value),
                "float" or "single" => ToFloat(value),
                "double" => ToDouble(value),
                "bool" or "boolean" => ToBool(value),
                "guid" => ToGuid(value),
                _ => value
            };
        }
        catch { return value; }
    }

    private static int ToInt(object v) => v is int i ? i : int.Parse(v.ToString()!);
    private static long ToLong(object v) => v is long l ? l : long.Parse(v.ToString()!);
    private static float ToFloat(object v) => v is float f ? f : float.Parse(v.ToString()!);
    private static double ToDouble(object v) => v is double d ? d : double.Parse(v.ToString()!);
    private static bool ToBool(object v)
    {
        if (v is bool b) return b;
        var s = v.ToString()!.Trim().ToLowerInvariant();
        if (s is "1" or "true" or "yes" or "y") return true;
        if (s is "0" or "false" or "no" or "n") return false;
        return bool.Parse(s);
    }
    private static Guid ToGuid(object v) => v is Guid g ? g : Guid.Parse(v.ToString()!);
}
