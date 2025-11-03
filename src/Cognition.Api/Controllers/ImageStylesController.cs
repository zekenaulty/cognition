using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Images;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/image-styles")]
public class ImageStylesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ImageStylesController(CognitionDbContext db) => _db = db;

    public record CreateStyleRequest(string Name, string? Description, string? PromptPrefix, string? NegativePrompt, bool IsActive = true);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? active, CancellationToken cancellationToken = default)
    {
        var q = _db.ImageStyles.AsNoTracking().AsQueryable();
        if (active.HasValue) q = q.Where(s => s.IsActive == active.Value);
        var styles = await q.OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return Ok(styles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStyleRequest req, CancellationToken cancellationToken = default)
    {
        var exists = await _db.ImageStyles.AnyAsync(s => s.Name == req.Name, cancellationToken);
        if (exists) return Conflict(ApiErrorResponse.Create("image_style_conflict", "Style name already exists."));
        var s = new ImageStyle
        {
            Name = req.Name,
            Description = req.Description,
            PromptPrefix = req.PromptPrefix,
            NegativePrompt = req.NegativePrompt,
            IsActive = req.IsActive
        };
        _db.ImageStyles.Add(s);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { s.Id });
    }
}
