using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cognition.Clients.Agents;
using Hangfire;
using Cognition.Api.Infrastructure.Hangfire;
using Cognition.Data.Relational;
using Microsoft.EntityFrameworkCore;
using Cognition.Clients.Tools;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly IBackgroundJobClient _jobs;
    private readonly IHangfireRunner _runner;
    private readonly CognitionDbContext _db;
    public ChatController(IAgentService agents, IBackgroundJobClient jobs, IHangfireRunner runner, CognitionDbContext db)
    { _agents = agents; _jobs = jobs; _runner = runner; _db = db; }

    public record AskRequest(
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        bool RolePlay = false);

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        try
        {
            var started = DateTime.UtcNow.AddSeconds(-1);
            var jobId = _jobs.Enqueue<Cognition.Jobs.TextJobs>(j => j.ChatOnce(Guid.Empty, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, CancellationToken.None));
            var ok = await _runner.WaitForCompletionAsync(jobId, TimeSpan.FromSeconds(30*5), TimeSpan.FromMilliseconds(50), HttpContext.RequestAborted);
            // We used conversationId=Guid.Empty (single-shot AskAsync equivalent). Directly invoke fallback if not using conversation.
            if (!ok)
            {
                // Fallback to inline to avoid total failure
                var replyInline = await _agents.AskAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
                return Ok(new { reply = replyInline });
            }
            // AskAsync returns a string; TextJobs returns it but we don't fetch returns; return best-effort inline again to provide value.
            var reply = await _agents.AskAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_failed", message = ex.Message });
        }
    }

    [HttpPost("ask-with-tools")]
    public async Task<IActionResult> AskWithTools([FromBody] AskRequest req)
    {
        try
        {
            var reply = await _agents.AskWithToolsAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_with_tools_failed", message = ex.Message });
        }
    }

    public record ChatRequest(
        Guid ConversationId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        bool RolePlay = false);

    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req)
    {
        try
        {
            var started = DateTime.UtcNow.AddSeconds(-1);
            // Enqueue Chat job and wait
            var jobId = _jobs.Enqueue<Cognition.Jobs.TextJobs>(j => j.ChatOnce(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, CancellationToken.None));
            var ok = await _runner.WaitForCompletionAsync(jobId, TimeSpan.FromSeconds(60*5), TimeSpan.FromMilliseconds(50), HttpContext.RequestAborted);
            // After success (or even if timed out), fetch the latest assistant message persisted after we started
            var msg = await _db.ConversationMessages.AsNoTracking()
                .Where(m => m.ConversationId == req.ConversationId
                         && m.FromPersonaId == req.PersonaId
                         && m.Role == Cognition.Data.Relational.Modules.Common.ChatRole.Assistant
                         && m.Timestamp >= started)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            var reply = msg?.Content ?? (ok ? string.Empty : "") ;
            if (string.IsNullOrWhiteSpace(reply))
            {
                // Final fallback: run inline to preserve UX
                reply = await _agents.ChatAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
            }
            return Ok(new { reply });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { code = "conversation_not_found", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "chat_failed", message = ex.Message });
        }
    }
}
