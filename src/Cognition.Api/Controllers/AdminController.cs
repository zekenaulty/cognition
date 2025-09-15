using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/admin")] 
[Authorize(Roles = nameof(UserRole.Administrator))]
public class AdminController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public AdminController(CognitionDbContext db) => _db = db;

    [HttpPost("sync-models")]
    public async Task<IActionResult> SyncModels()
    {
        // Minimal placeholder: ensure providers exist; models are already seeded by migrations
        var changed = false;
        async Task EnsureProvider(string name, string display, string? baseUrl)
        {
            var p = await _db.Providers.FirstOrDefaultAsync(x => x.Name == name);
            if (p == null)
            {
                _db.Providers.Add(new Data.Relational.Modules.LLM.Provider { Name = name, DisplayName = display, BaseUrl = baseUrl, IsActive = true, CreatedAtUtc = System.DateTime.UtcNow });
                changed = true;
            }
        }
        await EnsureProvider("OpenAI", "OpenAI", null);
        await EnsureProvider("Gemini", "Google Gemini", null);
        await EnsureProvider("Ollama", "Ollama", "http://localhost:11434");
        if (changed) await _db.SaveChangesAsync();
        return Ok(new { ok = true, changed });
    }

    [HttpPost("sync-credentials")]
    public async Task<IActionResult> SyncCredentials()
    {
        // Ensure API credentials exist for known providers based on env var names
        var openai = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "OpenAI");
        var gemini = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Gemini");
        if (openai != null && !await _db.ApiCredentials.AnyAsync(c => c.ProviderId == openai.Id && c.KeyRef == "OPENAI_KEY"))
        {
            _db.ApiCredentials.Add(new Data.Relational.Modules.LLM.ApiCredential { ProviderId = openai.Id, KeyRef = "OPENAI_KEY", IsValid = true, Notes = "Synced" });
        }
        if (gemini != null && !await _db.ApiCredentials.AnyAsync(c => c.ProviderId == gemini.Id && c.KeyRef == "GOOGLE_API_KEY"))
        {
            _db.ApiCredentials.Add(new Data.Relational.Modules.LLM.ApiCredential { ProviderId = gemini.Id, KeyRef = "GOOGLE_API_KEY", IsValid = true, Notes = "Synced" });
        }
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
