using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ViewerOrHigher)]
[ApiController]
[Route("api/fiction/projects")]
public sealed class FictionProjectsController : ControllerBase
{
    private readonly CognitionDbContext _db;

    public FictionProjectsController(CognitionDbContext db)
        => _db = db ?? throw new ArgumentNullException(nameof(db));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FictionProjectSummary>>> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await _db.FictionProjects
            .AsNoTracking()
            .OrderBy(p => p.Title)
            .Select(p => new FictionProjectSummary(
                p.Id,
                p.Title,
                p.Logline,
                p.Status,
                p.FictionPlans.Count,
                p.FictionPlans.Count(plan => plan.Status != FictionPlanStatus.Archived)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(projects);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
    public async Task<ActionResult<FictionProjectSummary>> CreateProject(
        [FromBody] CreateFictionProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Project title is required.");
        }

        var project = new FictionProject
        {
            Id = Guid.NewGuid(),
            Title = title,
            Logline = string.IsNullOrWhiteSpace(request.Logline) ? null : request.Logline.Trim(),
            Status = FictionProjectStatus.Active
        };

        _db.FictionProjects.Add(project);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var summary = new FictionProjectSummary(project.Id, project.Title, project.Logline, project.Status, 0, 0);
        return CreatedAtAction(nameof(GetProjects), null, summary);
    }

    public sealed record FictionProjectSummary(
        Guid Id,
        string Title,
        string? Logline,
        FictionProjectStatus Status,
        int PlanCount,
        int ActivePlanCount);

    public sealed record CreateFictionProjectRequest([property: Required] string Title, string? Logline);
}
