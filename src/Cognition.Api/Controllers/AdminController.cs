using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Cognition.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/admin")] 
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public class AdminController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public AdminController(CognitionDbContext db) => _db = db;

    [HttpPost("sync-models")]
    public async Task<IActionResult> SyncModels(CancellationToken cancellationToken)
    {
        // Minimal placeholder: ensure providers exist; models are already seeded by migrations
        var changed = false;
        async Task EnsureProvider(string name, string display, string? baseUrl)
        {
            var p = await _db.Providers.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
            if (p == null)
            {
                _db.Providers.Add(new Data.Relational.Modules.LLM.Provider { Name = name, DisplayName = display, BaseUrl = baseUrl, IsActive = true, CreatedAtUtc = System.DateTime.UtcNow });
                changed = true;
            }
        }
        await EnsureProvider("OpenAI", "OpenAI", null);
        await EnsureProvider("Gemini", "Google Gemini", null);
        await EnsureProvider("Ollama", "Ollama", "http://localhost:11434");
        if (changed) await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { ok = true, changed });
    }

    [HttpPost("sync-credentials")]
    public async Task<IActionResult> SyncCredentials(CancellationToken cancellationToken)
    {
        // Ensure API credentials exist for known providers based on env var names
        var openai = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "OpenAI", cancellationToken);
        var gemini = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Gemini", cancellationToken);
        if (openai != null && !await _db.ApiCredentials.AnyAsync(c => c.ProviderId == openai.Id && c.KeyRef == "OPENAI_KEY", cancellationToken))
        {
            _db.ApiCredentials.Add(new Data.Relational.Modules.LLM.ApiCredential { ProviderId = openai.Id, KeyRef = "OPENAI_KEY", IsValid = true, Notes = "Synced" });
        }
        if (gemini != null && !await _db.ApiCredentials.AnyAsync(c => c.ProviderId == gemini.Id && c.KeyRef == "GOOGLE_API_KEY", cancellationToken))
        {
            _db.ApiCredentials.Add(new Data.Relational.Modules.LLM.ApiCredential { ProviderId = gemini.Id, KeyRef = "GOOGLE_API_KEY", IsValid = true, Notes = "Synced" });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { ok = true });
    }
}
