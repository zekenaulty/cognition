using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Cognition.Clients.Images;
using Hangfire;
using Cognition.Api.Infrastructure.Hangfire;
using Cognition.Data.Relational;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IImageService _images;
    private readonly IBackgroundJobClient _jobs;
    private readonly IHangfireRunner _runner;
    public ImagesController(CognitionDbContext db, IImageService images, IBackgroundJobClient jobs, IHangfireRunner runner)
    { _db = db; _images = images; _jobs = jobs; _runner = runner; }

    public record GenerateImageRequest(Guid? ConversationId, Guid? PersonaId, string Prompt,
        int Width = 1024, int Height = 1024, string? StyleName = null, Guid? StyleId = null,
        string? NegativePrompt = null, int Steps = 30, float Guidance = 7.5f, int? Seed = null,
        string Provider = "OpenAI", string Model = "dall-e-3");

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateImageRequest req)
    {
        var style = req.StyleId.HasValue
            ? await _db.ImageStyles.AsNoTracking().FirstOrDefaultAsync(s => s.Id == req.StyleId.Value)
            : (!string.IsNullOrWhiteSpace(req.StyleName)
                ? await _db.ImageStyles.AsNoTracking().FirstOrDefaultAsync(s => s.Name == req.StyleName)
                : null);

        var started = DateTime.UtcNow.AddSeconds(-1);
        // Enqueue job and wait for completion
        var jobId = _jobs.Enqueue<Cognition.Jobs.ImageJobs>(j => j.Generate(req.ConversationId, req.PersonaId, req.Prompt, req.Width, req.Height, req.StyleId, req.NegativePrompt, req.Provider, req.Model, CancellationToken.None));
        var ok = await _runner.WaitForCompletionAsync(jobId, TimeSpan.FromSeconds(90), TimeSpan.FromMilliseconds(500));
        // Fetch the asset created after we started
        var asset = await _db.ImageAssets.AsNoTracking()
            .Where(x => x.ConversationId == req.ConversationId && x.CreatedAtUtc >= started)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (asset == null)
        {
            return StatusCode(500, ok ? "Image generated but record not found." : "Image generation failed or timed out");
        }

        if (style != null)
        {
            asset.StyleId = style.Id;
            await _db.SaveChangesAsync();
        }
        return Ok(new { asset.Id });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var a = await _db.ImageAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        return File(a.Bytes, a.MimeType);
    }

    // Querystring-friendly variant for use in <img src="..."> tags
    [AllowAnonymous]
    [HttpGet("content")]
    public async Task<IActionResult> ContentById([FromQuery] Guid id)
    {
        var a = await _db.ImageAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        return File(a.Bytes, a.MimeType);
    }

    [HttpGet("by-conversation/{conversationId:guid}")]
    public async Task<IActionResult> ListByConversation(Guid conversationId)
    {
        var items = await _db.ImageAssets.AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Provider, x.Model, x.Width, x.Height, x.MimeType, x.CreatedAtUtc, x.StyleId, x.Prompt, StyleName = x.Style != null ? x.Style.Name : null })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("by-persona/{personaId:guid}")]
    public async Task<IActionResult> ListByPersona(Guid personaId)
    {
        var items = await _db.ImageAssets.AsNoTracking()
            .Where(x => x.CreatedByPersonaId == personaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Provider, x.Model, x.Width, x.Height, x.MimeType, x.CreatedAtUtc, x.StyleId, x.ConversationId })
            .ToListAsync();
        return Ok(items);
    }
}
