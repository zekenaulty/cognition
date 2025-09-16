using System.Text;
using System.Text.Json;
using Cognition.Clients.LLM;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Tools;
using Cognition.Data.Relational.Modules.FeatureFlags;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.IO;

namespace Cognition.Clients.Agents;

public interface IAgentService
{
    Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default);
    Task<string> AskWithToolsAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default);
    Task<string> ChatAsync(Guid conversationId, Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default);
    Task<string> AskWithPlanAsync(Guid conversationId, Guid personaId, Guid providerId, Guid? modelId, string input, int minSteps, int maxSteps, bool rolePlay = false, CancellationToken ct = default);
}


public class AgentService : IAgentService
{
    // P5: Helpers for CoT v2 loop
    private static string CompactObservation(string? s, int max = 200)
    {
        if (string.IsNullOrWhiteSpace(s)) return "<no output>";
        var t = s.Trim().Replace("\r", " ").Replace("\n", " ");
        return t.Length <= max ? t : t[..max] + "…";
    }
    // P1: Prompt layer helpers for strict JSON contracts and tool-first cognition
    private static string BuildOutlinePlanPrompt(string input, string compactHistory, string toolIndex)
    {
        return $"You have tools. All work MUST be done by tools. Return JSON only for the current MODE. No prose.\n" +
               "Process: Plan → one tool call → observe → iterate; finish with final_answer.\n" +
               "Constraints: one tool per step; validate args; be concise; no clarifications unless essential.\n" +
               "MODE: OUTLINE_PLAN\n" +
               $"UserRequest: \"{input}\"\n" +
               $"History: {compactHistory}\n" +
               $"Tools: {toolIndex}\n" +
               "Return ONLY a JSON array of tasks: [ {\"goal\":\"...\",\"suggestedTool\":\"nameOrId\",\"notes\":\"...\"} ]";
    }

    private static string BuildStepPlanPrompt(string goalCompact, int maxSteps, string outlineJson, string completedJson, string toolIndex)
    {
        return $"You have tools. All work MUST be done by tools. Return JSON only for the current MODE. No prose.\n" +
                     "Process: Plan → one tool call → observe → iterate; finish with final_answer.\n" +
                     "Constraints: one tool per step; validate args; be concise; no clarifications unless essential.\n" +
                     "MODE: PLAN\n" +
                     $"STATE: Goal: \"{goalCompact}\"; Constraints: [\"Tools only\", \"Max {maxSteps}\", \"One tool per step\"]; OutlinePlan: {outlineJson}; Completed: {completedJson}\n" +
                     "RESPONSE_SCHEMA: { \"step\": 1, \"goal\": \"this step goal\", \"rationale\": \"<=2 sentences\", \"action\": {\"toolId\":\"GUID\",\"args\":{}}, \"finish\": false, \"final_answer\": null }\n" +
                     "Return ONLY JSON matching schema.";
    }

    private static string BuildFinalizePrompt(string finalAnswer, string respondToolId)
    {
        return $"You have tools. All work MUST be done by tools. Return JSON only for the current MODE. No prose.\n" +
               "MODE: FINALIZE\n" +
               $"Return ONLY: {{\"toolId\":\"{respondToolId}\",\"args\":{{\"text\":\"{finalAnswer}\"}}}}";
    }

    private readonly CognitionDbContext _db;
    private readonly ILLMClientFactory _factory;
    private readonly Cognition.Clients.Tools.IToolDispatcher _dispatcher;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AgentService> _logger;

    public AgentService(CognitionDbContext db, ILLMClientFactory factory, Cognition.Clients.Tools.IToolDispatcher dispatcher, IServiceProvider sp, ILogger<AgentService> logger)
    {
        _db = db;
        _factory = factory;
        _dispatcher = dispatcher;
        _sp = sp;
        _logger = logger;
    }

    public async Task<string> AskAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default)
    {
        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId, ct);
        var sys = persona is null ? "" : BuildSystemMessage(persona, rolePlay);
        var client = await _factory.CreateAsync(providerId, modelId);
        var prompt = string.IsNullOrWhiteSpace(sys) ? input : $"{sys}\n\nUser: {input}";
        try
        {
            return await client.GenerateAsync(prompt, track: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM GenerateAsync failed for provider {ProviderId} model {ModelId}", providerId, modelId);
            return "Sorry, I couldn’t complete that request.";
        }
    }

    public async Task<string> AskWithPlanAsync(Guid conversationId, Guid personaId, Guid providerId, Guid? modelId, string input, int minSteps, int maxSteps, bool rolePlay = false, CancellationToken ct = default)
    {
        // 1. Save original message
        var userMsg = new ConversationMessage
        {
            ConversationId = conversationId,
            FromPersonaId = personaId,
            Role = Cognition.Data.Relational.Modules.Common.ChatRole.User,
            Content = input,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "TextResponse"
        };
        _db.ConversationMessages.Add(userMsg);
        await _db.SaveChangesAsync(ct);

        // 2. Create ConversationPlan and discover tools
        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId, ct);
        var tools = await _db.Tools
            .Include(t => t.Parameters)
            .Where(t => t.IsActive)
            .Where(t => _db.ToolProviderSupports.Any(s => s.ToolId == t.Id
                                                        && s.ProviderId == providerId
                                                        && (s.ModelId == null || (modelId != null && s.ModelId == modelId))
                                                        && s.SupportLevel != Cognition.Data.Relational.Modules.Common.SupportLevel.Unsupported))
            .AsNoTracking()
            .ToListAsync(ct);
        var toolIndex = BuildToolIndexSection(tools);
        var sys = persona is null ? "" : BuildSystemMessage(persona, rolePlay);
        var client = await _factory.CreateAsync(providerId, modelId);

        // 3. Generate OUTLINE_PLAN
        var compactHistory = ""; // TODO: build compact history from recent messages
        var outlinePrompt = BuildOutlinePlanPrompt(input, compactHistory, toolIndex);
        var outlineJson = await client.GenerateAsync(outlinePrompt, track: false);

        // Validate plan JSON against schema
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "schemas", "plan.schema.json");
        var schemaText = await File.ReadAllTextAsync(schemaPath, ct);
        var schema = Newtonsoft.Json.Schema.JSchema.Parse(schemaText);
        var planObj = Newtonsoft.Json.Linq.JObject.Parse(outlineJson);
        var errors = new List<string>();
        planObj.Validate(schema, (o, a) => errors.Add(a.Message));
        if (errors.Count > 0)
        {
            _logger.LogWarning("Plan JSON failed validation: {Errors}", string.Join("; ", errors));
            throw new InvalidOperationException($"Plan JSON is invalid: {string.Join("; ", errors)}");
        }

        var plan = new ConversationPlan
        {
            ConversationId = conversationId,
            PersonaId = personaId,
            Title = $"Plan for: {input.Substring(0, Math.Min(40, input.Length))}",
            Description = $"CoT v2 plan generated for message {userMsg.Id}",
            CreatedAt = DateTime.UtcNow,
            OutlineJson = outlineJson,
            Tasks = new List<ConversationTask>()
        };
        _db.ConversationPlans.Add(plan);
        await _db.SaveChangesAsync(ct);

        // 4. Step loop: PLAN → tool → observe → persist
        var results = new List<string>();
        var completedSteps = new List<object>(); // for state
        for (int step = 1; step <= maxSteps; step++)
        {
            // Build state for PLAN prompt
            var goalCompact = input; // TODO: refine
            var outlineState = outlineJson ?? "[]";
            var completedJson = System.Text.Json.JsonSerializer.Serialize(completedSteps);
            var planPrompt = BuildStepPlanPrompt(goalCompact, maxSteps, outlineState, completedJson, toolIndex);
            var planStepJson = await client.GenerateAsync(planPrompt, track: false);
            // TODO: Parse/repair planStepJson, fallback to error if invalid

            // Save ConversationThought (step, rationale, plan snapshot, prompt)
            var thoughtEntity = new ConversationThought
            {
                ConversationId = conversationId,
                PersonaId = personaId,
                Thought = planStepJson,
                StepNumber = step,
                Rationale = null, // TODO: extract from parsed JSON
                PlanSnapshotJson = outlineJson,
                Prompt = planPrompt,
                Timestamp = DateTime.UtcNow
            };
            _db.ConversationThoughts.Add(thoughtEntity);
            await _db.SaveChangesAsync(ct);

            // Parse planStepJson to get toolId, args, finish, final_answer
            Guid? toolId = null;
            string? toolName = null;
            string? argsJson = null;
            bool finish = false;
            string? finalAnswer = null;
            string? rationale = null;
            string? goal = null;
            string? observation = null;
            string? error = null;
            string status = "Pending";
            try
            {
                using var doc = JsonDocument.Parse(planStepJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("action", out var action))
                {
                    if (action.TryGetProperty("toolId", out var tId))
                    {
                        var tIdStr = tId.GetString();
                        if (Guid.TryParse(tIdStr, out var guid)) toolId = guid;
                        else toolName = tIdStr;
                    }
                    if (action.TryGetProperty("args", out var args))
                    {
                        argsJson = args.GetRawText();
                    }
                }
                finish = root.TryGetProperty("finish", out var f) && f.GetBoolean();
                finalAnswer = root.TryGetProperty("final_answer", out var fa) ? fa.GetString() : null;
                rationale = root.TryGetProperty("rationale", out var r) ? r.GetString() : null;
                goal = root.TryGetProperty("goal", out var g) ? g.GetString() : null;
            }
            catch (Exception ex)
            {
                error = $"Invalid PLAN JSON: {ex.Message}";
                status = "Failure";
            }

            // If finish, call Respond.Answer tool
            string result = "";
            if (finish && !string.IsNullOrWhiteSpace(finalAnswer))
            {
                // TODO: get Respond.Answer toolId
                var respondToolId = toolId ?? Guid.Empty;
                var finalizePrompt = BuildFinalizePrompt(finalAnswer, respondToolId.ToString());
                result = await client.GenerateAsync(finalizePrompt, track: false);
                status = "Success";
            }
            else if (toolId.HasValue || !string.IsNullOrWhiteSpace(toolName))
            {
                // Dispatch tool
                try
                {
                    var ctx = new Cognition.Clients.Tools.ToolContext(null, null, personaId, _sp, ct);
                    var execId = toolId ?? tools.FirstOrDefault(t => t.Name == toolName)?.Id ?? Guid.Empty;
                    var argsDict = !string.IsNullOrWhiteSpace(argsJson)
                        ? JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? new()
                        : new Dictionary<string, object?>();
                    var exec = await _dispatcher.ExecuteAsync(execId, ctx, argsDict, log: true);
                    if (!exec.ok)
                    {
                        result = $"Tool error: {exec.error}";
                        error = exec.error;
                        status = "Failure";
                    }
                    else
                    {
                        result = exec.result is string s ? s : JsonSerializer.Serialize(exec.result);
                        status = "Success";
                    }
                }
                catch (Exception ex)
                {
                    result = $"Tool dispatch error: {ex.Message}";
                    error = ex.Message;
                    status = "Failure";
                }
            }
            else
            {
                result = "No valid tool selected.";
                error = "No valid tool selected.";
                status = "Failure";
            }

            // Compact observation
            observation = CompactObservation(result, 200);
            results.Add(result);

            // Persist ConversationTask
            var task = new ConversationTask
            {
                ConversationPlanId = plan.Id,
                StepNumber = step,
                Thought = planStepJson,
                Goal = goal,
                Rationale = rationale,
                ToolId = toolId,
                ToolName = toolName,
                ArgsJson = argsJson,
                Observation = observation,
                Error = error,
                Finish = finish,
                FinalAnswer = finalAnswer,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            _db.ConversationTasks.Add(task);
            plan.Tasks.Add(task);
            await _db.SaveChangesAsync(ct);

            // Add completed step for next PLAN state
            completedSteps.Add(new
            {
                step,
                action = new { toolId = toolId?.ToString() ?? toolName, args = argsJson },
                ok = status == "Success",
                observation
            });

            // Optionally, add a 'thought' message to ConversationMessage
            var thoughtMsg = new ConversationMessage
            {
                ConversationId = conversationId,
                FromPersonaId = personaId,
                Role = Cognition.Data.Relational.Modules.Common.ChatRole.Assistant,
                Content = result,
                Timestamp = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                Metatype = "PlanThought"
            };
            _db.ConversationMessages.Add(thoughtMsg);
            await _db.SaveChangesAsync(ct);

            // Early exit if finish or maxSteps
            if (finish || (step >= minSteps && status == "Success" && !string.IsNullOrWhiteSpace(finalAnswer))) break;
        }

        // 5. Aggregate and return final response
        var finalResponse = string.Join("\n", results);
        var assistantMsg = new ConversationMessage
        {
            ConversationId = conversationId,
            FromPersonaId = personaId,
            Role = Cognition.Data.Relational.Modules.Common.ChatRole.Assistant,
            Content = finalResponse,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "TextResponse"
        };
        _db.ConversationMessages.Add(assistantMsg);
        await _db.SaveChangesAsync(ct);

        return finalResponse;
    }

    public async Task<string> AskWithToolsAsync(Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default)
    {
        // Feature-flag: if ToolsEnabled exists and is disabled, fall back to AskAsync
        var flag = await _db.FeatureFlags.AsNoTracking().FirstOrDefaultAsync(f => f.Key == "ToolsEnabled", ct);
        if (flag != null && !flag.IsEnabled)
        {
            return await AskAsync(personaId, providerId, modelId, input, rolePlay, ct);
        }
        // Discover available tools for this provider/model and expose a compact schema
        var tools = await _db.Tools
            .Include(t => t.Parameters)
            .Where(t => t.IsActive)
            .Where(t => _db.ToolProviderSupports.Any(s => s.ToolId == t.Id
                                                        && s.ProviderId == providerId
                                                        && (s.ModelId == null || (modelId != null && s.ModelId == modelId))
                                                        && s.SupportLevel != Cognition.Data.Relational.Modules.Common.SupportLevel.Unsupported))
            .AsNoTracking()
            .ToListAsync(ct);

        var draft = await AskWithToolIndexAsync(personaId, providerId, modelId, input, tools, rolePlay);

        // Try to parse a tool plan (by id or name)
        try
        {
            if (!TryGetJsonObjectText(draft, out var jsonText))
            {
                return draft; // not JSON -> direct answer
            }
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return draft;
            }
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            Guid? toolId = null;
            if (root.TryGetProperty("toolId", out var t))
            {
                var ts = t.GetString();
                if (Guid.TryParse(ts, out var tmp)) toolId = tmp;
            }

            if (toolId.HasValue)
            {
                var args = root.TryGetProperty("args", out var a)
                    ? JsonSerializer.Deserialize<Dictionary<string, object?>>(a.GetRawText()) ?? new()
                    : new Dictionary<string, object?>();

                var ctx = new Cognition.Clients.Tools.ToolContext(null, null, personaId, _sp, ct);
                _logger.LogInformation("Agent invoking tool {ToolId} for persona {PersonaId}", toolId, personaId);
                var exec = await _dispatcher.ExecuteAsync(toolId.Value, ctx, args, log: true);
                if (!exec.ok) return $"Tool error: {exec.error}";
                return exec.result is string s ? s : JsonSerializer.Serialize(exec.result);
            }
        }
        catch
        {
            // not JSON => direct answer
        }

        return draft;
    }

    // Chat with conversation history and summaries; persists user/assistant messages
    public async Task<string> ChatAsync(Guid conversationId, Guid personaId, Guid providerId, Guid? modelId, string input, bool rolePlay = false, CancellationToken ct = default)
    {
        // Ensure the conversation exists without loading heavy navigations
        var exists = await _db.Conversations.AsNoTracking().AnyAsync(c => c.Id == conversationId, ct);
        if (!exists) throw new InvalidOperationException("Conversation not found");

        var persona = await _db.Personas.FirstAsync(p => p.Id == personaId, ct);
        var system = BuildSystemMessage(persona, rolePlay);

        // Persist user message first
        var userMsg = new ConversationMessage
        {
            ConversationId = conversationId,
            FromPersonaId = personaId,
            Role = Cognition.Data.Relational.Modules.Common.ChatRole.User,
            Content = input,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
                ,
            Metatype = "TextResponse"
        };
        _db.ConversationMessages.Add(userMsg);
        await _db.SaveChangesAsync(ct);

        // Determine a rough turn window from model context (fallback to 20)
        var maxTurns = 20;
        if (modelId.HasValue)
        {
            var m = await _db.Models.AsNoTracking().FirstOrDefaultAsync(mm => mm.Id == modelId.Value);
            if (m?.ContextWindow is int cw && cw > 0)
            {
                maxTurns = Math.Clamp(cw / 256, 6, 40);
            }
        }
        // Fetch just the most recent N messages from DB, then order ascending in-memory
        var window = await _db.ConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp)
            .Take(maxTurns)
            .ToListAsync(ct);
        window.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        var chat = new List<ChatMessage>();
        // 1) Instruction sets as distinct system messages (scoped: global/provider/model/persona)
        var instructionMsgs = await BuildInstructionMessages(personaId, providerId, modelId, rolePlay, ct);
        chat.AddRange(instructionMsgs);
        // 2) Persona baseline system message
        if (!string.IsNullOrWhiteSpace(system)) chat.Add(new ChatMessage("system", system));
        // Optional: include latest summary as system supplement (DB limited)
        var summary = await _db.ConversationSummaries.AsNoTracking()
            .Where(s => s.ConversationId == conversationId)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefaultAsync(ct);
        if (summary != null)
            chat.Add(new ChatMessage("system", "Conversation Summary: " + summary.Content));
        foreach (var m in window)
        {
            var role = m.Role switch
            {
                Cognition.Data.Relational.Modules.Common.ChatRole.Assistant => "assistant",
                Cognition.Data.Relational.Modules.Common.ChatRole.System => "system",
                _ => "user"
            };
            chat.Add(new ChatMessage(role, m.Content));
        }
        // Append the new input (also in window now via persisted user message)
        chat.Add(new ChatMessage("user", input));

        var client = await _factory.CreateAsync(providerId, modelId);
        string reply;
        try
        {
            reply = await client.ChatAsync(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM ChatAsync failed for provider {ProviderId} model {ModelId} conversation {ConversationId}", providerId, modelId, conversationId);
            reply = "Sorry, I couldn’t complete that request.";
        }

        // Persist assistant reply
        var assistantMsg = new ConversationMessage
        {
            ConversationId = conversationId,
            FromPersonaId = personaId,
            Role = Cognition.Data.Relational.Modules.Common.ChatRole.Assistant,
            Content = reply,
            Timestamp = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
                ,
            Metatype = "TextResponse"
        };
        _db.ConversationMessages.Add(assistantMsg);
        await _db.SaveChangesAsync(ct);

        // If conversation has no title yet, ask the LLM to propose a concise title and save it
        try
        {
            var convo = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            if (convo != null && string.IsNullOrWhiteSpace(convo.Title))
            {
                var recent = window.TakeLast(8).Select(m => $"{m.Role}: {m.Content}");
                var titlePrompt = new List<ChatMessage>
                {
                    new ChatMessage("system", "You generate concise, descriptive conversation titles (3–6 words). No quotes, no punctuation at the end."),
                    new ChatMessage("user", string.Join("\n", recent))
                };
                var title = await client.ChatAsync(titlePrompt);
                title = (title ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    // Truncate overly long titles
                    if (title.Length > 80) title = title.Substring(0, 80);
                    convo.Title = title;
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-title conversation {ConversationId}", conversationId);
        }

        // Optional summarization trigger when window is dense
        try
        {
            if (window.Count >= maxTurns)
            {
                var toSummarize = string.Join("\n", window.Select(m => $"{m.Role}: {m.Content}"));
                var sumPrompt = new List<ChatMessage>
                {
                    new ChatMessage("system", "Summarize the conversation succinctly in 5-7 bullet points, focusing on decisions, facts, and tasks. Be concise."),
                    new ChatMessage("user", toSummarize)
                };
                var summaryText = await client.ChatAsync(sumPrompt, track: false);
                if (!string.IsNullOrWhiteSpace(summaryText))
                {
                    _db.ConversationSummaries.Add(new ConversationSummary
                    {
                        ConversationId = conversationId,
                        ByPersonaId = personaId,
                        Content = summaryText,
                        Timestamp = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Conversation summarization failed for conversation {ConversationId}", conversationId);
        }

        return reply;
    }

    private async Task<IEnumerable<ChatMessage>> BuildInstructionMessages(Guid personaId, Guid providerId, Guid? modelId, bool rolePlay, CancellationToken ct = default)
    {
        // Pull active instruction sets for relevant scopes
        var q = _db.InstructionSets
            .AsNoTracking()
            .Where(s => s.IsActive);

        // Note: Scope values are conventions: "global", "provider", "model", "persona" (case-insensitive)
        q = q.Where(s =>
            (s.Scope == null || s.Scope!.ToLower() == "global")
            || (s.Scope!.ToLower() == "provider" && s.ScopeRefId == providerId)
            || (modelId != null && s.Scope!.ToLower() == "model" && s.ScopeRefId == modelId)
            || (s.Scope!.ToLower() == "persona" && s.ScopeRefId == personaId)
        );

        var sets = await q
            .Include(s => s.Items)
                .ThenInclude(i => i.Instruction)
            .ToListAsync(ct);

        var items = sets
            .SelectMany(s => s.Items)
            .Where(i => i.Enabled && i.Instruction.IsActive)
            .OrderBy(i => i.Order)
            .ToList();

        // Prioritize by kind to keep important rules first
        int KindRank(Cognition.Data.Relational.Modules.Common.InstructionKind k) => k switch
        {
            Cognition.Data.Relational.Modules.Common.InstructionKind.MissionCritical => 0,
            Cognition.Data.Relational.Modules.Common.InstructionKind.CoreRules => 1,
            Cognition.Data.Relational.Modules.Common.InstructionKind.Tool => 2,
            Cognition.Data.Relational.Modules.Common.InstructionKind.SystemInstruction => 3,
            Cognition.Data.Relational.Modules.Common.InstructionKind.Persona => 4,
            _ => 5
        };

        var filtered = items
            .Where(i => !i.Instruction.RolePlay || rolePlay)
            .OrderBy(i => KindRank(i.Instruction.Kind))
            .ThenBy(i => i.Order)
            .ThenBy(i => i.Instruction.Name)
            .Select(i => new ChatMessage("system", i.Instruction.Content))
            .ToList();

        return filtered;
    }

    private static bool TryGetJsonObjectText(string text, out string? json)
    {
        json = null;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        // Strip code fences if present
        if (trimmed.StartsWith("```"))
        {
            var startFence = trimmed.IndexOf("```", StringComparison.Ordinal);
            var endFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (startFence >= 0 && endFence > startFence)
            {
                var inner = trimmed.Substring(startFence + 3, endFence - (startFence + 3)).Trim();
                // Drop optional language hint like "json"
                var firstNewline = inner.IndexOf('\n');
                if (firstNewline > 0)
                {
                    var lang = inner.Substring(0, firstNewline).Trim();
                    if (lang.Equals("json", StringComparison.OrdinalIgnoreCase))
                        inner = inner.Substring(firstNewline + 1).Trim();
                }
                trimmed = inner;
            }
        }
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}")) { json = trimmed; return true; }

        int start = trimmed.IndexOf('{');
        while (start >= 0 && start < trimmed.Length)
        {
            int depth = 0;
            bool inString = false;
            for (int i = start; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (c == '"')
                {
                    bool escaped = i > 0 && trimmed[i - 1] == '\\';
                    if (!escaped) inString = !inString;
                }
                if (inString) continue;
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        json = trimmed.Substring(start, i - start + 1);
                        return true;
                    }
                }
            }
            start = trimmed.IndexOf('{', start + 1);
        }
        return false;
    }

    // Non-interface helper: include a Tool Index in the system message
    public async Task<string> AskWithToolIndexAsync(Guid personaId, Guid providerId, Guid? modelId, string input, IEnumerable<Tool> tools, bool rolePlay = false)
    {
        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId);
        var sys = persona is null ? "" : BuildSystemMessage(persona, rolePlay);
        var toolIndex = BuildToolIndexSection(tools);
        var fullSys = string.IsNullOrWhiteSpace(toolIndex) ? sys : (string.IsNullOrWhiteSpace(sys) ? toolIndex : $"{sys}\n\n{toolIndex}");
        var client = await _factory.CreateAsync(providerId, modelId);
        var prompt = string.IsNullOrWhiteSpace(fullSys) ? input : $"{fullSys}\n\nUser: {input}";
        try
        {
            return await client.GenerateAsync(prompt, track: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM GenerateAsync (ToolIndex) failed for provider {ProviderId} model {ModelId}", providerId, modelId);
            return "Sorry, I couldn’t complete that request.";
        }
    }

    private static string BuildSystemMessage(Persona p, bool rolePlay)
    {
        var sb = new StringBuilder();
        if (rolePlay)
        {
            sb.AppendLine($"You are role-playing as {p.Name} ({p.Nickname}).");
        }
        if (!string.IsNullOrWhiteSpace(p.Role)) sb.AppendLine($"Role: {p.Role}");
        if (!string.IsNullOrWhiteSpace(p.Gender)) sb.AppendLine($"Gender: {p.Gender}");
        if (!string.IsNullOrWhiteSpace(p.Essence)) sb.AppendLine($"Essence: {p.Essence}");
        if (!string.IsNullOrWhiteSpace(p.Beliefs)) sb.AppendLine($"Beliefs: {p.Beliefs}");
        if (!string.IsNullOrWhiteSpace(p.Background)) sb.AppendLine($"Background: {p.Background}");
        if (!string.IsNullOrWhiteSpace(p.CommunicationStyle)) sb.AppendLine($"CommunicationStyle: {p.CommunicationStyle}");
        if (!string.IsNullOrWhiteSpace(p.EmotionalDrivers)) sb.AppendLine($"EmotionalDrivers: {p.EmotionalDrivers}");
        if (p.SignatureTraits is { Length: > 0 }) sb.AppendLine($"SignatureTraits: {string.Join(", ", p.SignatureTraits)}");
        if (p.NarrativeThemes is { Length: > 0 }) sb.AppendLine($"NarrativeThemes: {string.Join(", ", p.NarrativeThemes)}");
        if (p.DomainExpertise is { Length: > 0 }) sb.AppendLine($"DomainExpertise: {string.Join(", ", p.DomainExpertise)}");
        return sb.ToString();
    }

    private static string BuildToolIndexSection(IEnumerable<Tool> tools)
    {
        var list = tools?.ToList() ?? new List<Tool>();
        if (list.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine("Tool Index:");
        sb.AppendLine("- You MUST select exactly one tool per step (unless finishing). Return JSON only. No text or prose allowed.");
        sb.AppendLine("- For each step, return: {\"toolId\":\"<GUID or Name>\",\"args\":{...}} (toolId can be GUID or name)");
        sb.AppendLine("- Arguments must match the schema below (required marked with *). Example JSON for each tool:");
        sb.AppendLine("Available tools:");
        foreach (var t in list)
        {
            sb.Append("- ").Append(t.Name).Append(" (id: ").Append(t.Id).Append(")");
            if (!string.IsNullOrWhiteSpace(t.Description)) sb.Append(" - ").Append(t.Description);
            var inputs = t.Parameters.Where(p => p.Direction == Cognition.Data.Relational.Modules.Common.ToolParamDirection.Input).ToList();
            if (inputs.Count > 0)
            {
                sb.Append(" | args: ");
                sb.Append(string.Join(", ", inputs.Select(p => $"{p.Name}:{p.Type}{(p.Required ? "*" : "")}")));
            }
            // Example JSON
            var exampleArgs = string.Join(", ", inputs.Select(p => $"\"{p.Name}\":{GetExampleValue(p.Type)}"));
            var exampleJson = $"{{\"toolId\":\"{t.Id}\",\"args\":{{{exampleArgs}}}}}";
            sb.Append(" | example: ").Append(exampleJson);
            sb.AppendLine();
        }
        return sb.ToString();

        // Helper for example values
        static string GetExampleValue(string type)
        {
            return type.ToLower() switch
            {
                "string" => "\"example\"",
                "int" => "1",
                "float" => "1.0",
                "bool" => "true",
                _ => "null"
            };
        }
    }

}
