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

            // 2. Create ConversationPlan
            var plan = new ConversationPlan
            {
                ConversationId = conversationId,
                PersonaId = personaId,
                Title = $"Plan for: {input.Substring(0, Math.Min(40, input.Length))}",
                Description = $"CoT plan generated for message {userMsg.Id}",
                CreatedAt = DateTime.UtcNow,
                Tasks = new List<ConversationTask>()
            };
            _db.ConversationPlans.Add(plan);
            await _db.SaveChangesAsync(ct);

            var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId, ct);
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
            var toolIndex = BuildToolIndexSection(tools);
            var sys = persona is null ? "" : BuildSystemMessage(persona, rolePlay);
            var fullSys = string.IsNullOrWhiteSpace(toolIndex) ? sys : (string.IsNullOrWhiteSpace(sys) ? toolIndex : $"{sys}\n\n{toolIndex}");
            var client = await _factory.CreateAsync(providerId, modelId);
            var thoughts = new List<string>();
            var results = new List<string>();

            // 3. Planning loop: generate thoughts, tasks, and execute with tool awareness
            int step = 1;
            for (; step <= maxSteps; step++)
            {
                // Generate next thought with full tool/system context
                var planPrompt = $"{fullSys}\n\nCurrent plan: {plan.Title}\nStep {step}: What should happen next?";
                var thought = await client.GenerateAsync(planPrompt, track: false);
                thoughts.Add(thought);

                // Save thought
                var thoughtEntity = new ConversationThought
                {
                    ConversationId = conversationId,
                    PersonaId = personaId,
                    Thought = thought,
                    Timestamp = DateTime.UtcNow
                };
                _db.ConversationThoughts.Add(thoughtEntity);
                await _db.SaveChangesAsync(ct);

                // Create task
                var task = new ConversationTask
                {
                    ConversationPlanId = plan.Id,
                    StepNumber = step,
                    Thought = thought,
                    Action = $"Action for step {step}",
                    CreatedAt = DateTime.UtcNow
                };
                _db.ConversationTasks.Add(task);
                plan.Tasks.Add(task);
                await _db.SaveChangesAsync(ct);

                // Execute task with full tool awareness
                var execPrompt = $"{fullSys}\n\nExecute: {thought}";
                var result = await AskWithToolsAsync(personaId, providerId, modelId, execPrompt, rolePlay, ct);
                task.Result = result;
                results.Add(result);
                await _db.SaveChangesAsync(ct);

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

                // Early exit if minSteps reached and some stop condition (not implemented)
                if (step >= minSteps && thought.Contains("[END]")) break;
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
                , Metatype = "TextResponse"
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
                , Metatype = "TextResponse"
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
        sb.AppendLine("- You may optionally call exactly one tool by returning pure JSON only on a single line:");
        sb.AppendLine("  {\"toolId\":\"<GUID>\",\"args\":{...}}");
        sb.AppendLine("- If no tool is needed, answer normally without JSON.");
        sb.AppendLine("- Arguments must match the schema below (required marked with *).");
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
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
