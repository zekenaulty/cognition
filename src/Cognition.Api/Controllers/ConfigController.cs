using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
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

    public record CreateRequest(string Name, string DataSourceType = "JsonStore", string CollectionName = "", object? Config = null);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.DataSources.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.DataSourceType>(req.DataSourceType, true, out var kind)) kind = Cognition.Data.Relational.Modules.Common.DataSourceType.JsonStore;
        var ds = new DataSource { Name = req.Name, DataSourceType = kind, CollectionName = req.CollectionName, Config = req.Config as System.Collections.Generic.Dictionary<string, object?> };
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

    public record CreateRequest(string Key, string? Type = null, object? Value = null);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.SystemVariables.AsNoTracking().OrderBy(x => x.Key).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        var sv = new SystemVariable { Key = req.Key, Type = req.Type, Value = req.Value as System.Collections.Generic.Dictionary<string, object?> };
        _db.SystemVariables.Add(sv);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { sv.Id });
    }
}
