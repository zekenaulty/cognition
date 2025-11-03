using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/config/data-sources")]
public class DataSourcesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public DataSourcesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters.")]
        string Name,
        DataSourceType DataSourceType = DataSourceType.JsonStore,
        [property: StringLength(128)]
        string? CollectionName = null,
        object? Config = null);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.DataSources.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        var ds = new DataSource
        {
            Name = req.Name.Trim(),
            DataSourceType = req.DataSourceType,
            CollectionName = string.IsNullOrWhiteSpace(req.CollectionName) ? string.Empty : req.CollectionName.Trim(),
            Config = req.Config as System.Collections.Generic.Dictionary<string, object?>
        };
        _db.DataSources.Add(ds);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { ds.Id });
    }
}

[ApiController]
[Route("api/config/system-variables")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public class SystemVariablesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public SystemVariablesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Key must contain non-whitespace characters.")]
        string Key,
        [property: StringLength(64)]
        string? Type = null,
        object? Value = null);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.SystemVariables.AsNoTracking().OrderBy(x => x.Key).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        var sv = new SystemVariable
        {
            Key = req.Key.Trim(),
            Type = string.IsNullOrWhiteSpace(req.Type) ? null : req.Type.Trim(),
            Value = req.Value as System.Collections.Generic.Dictionary<string, object?>
        };
        _db.SystemVariables.Add(sv);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { sv.Id });
    }
}
