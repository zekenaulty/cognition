using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Config;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/config/data-sources")]
public class DataSourcesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public DataSourcesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(string Name, string DataSourceType = "JsonStore", string CollectionName = "", object? Config = null);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.DataSources.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.DataSourceType>(req.DataSourceType, true, out var kind)) kind = Cognition.Data.Relational.Modules.Common.DataSourceType.JsonStore;
        var ds = new DataSource { Name = req.Name, DataSourceType = kind, CollectionName = req.CollectionName, Config = req.Config as System.Collections.Generic.Dictionary<string, object?> };
        _db.DataSources.Add(ds);
        await _db.SaveChangesAsync();
        return Ok(new { ds.Id });
    }
}

[ApiController]
[Route("api/config/system-variables")]
public class SystemVariablesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public SystemVariablesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(string Key, string? Type = null, object? Value = null);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.SystemVariables.AsNoTracking().OrderBy(x => x.Key).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        var sv = new SystemVariable { Key = req.Key, Type = req.Type, Value = req.Value as System.Collections.Generic.Dictionary<string, object?> };
        _db.SystemVariables.Add(sv);
        await _db.SaveChangesAsync();
        return Ok(new { sv.Id });
    }
}
