using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Services;

public interface ILlmDefaultService
{
    Task<(Guid? providerId, Guid? modelId)> GetActiveDefaultAsync(CancellationToken ct);
    Task<LlmGlobalDefault?> GetAsync(CancellationToken ct);
    Task<LlmGlobalDefault> UpsertAsync(Guid modelId, Guid? updatedByUserId, int priority, bool isActive, CancellationToken ct);
}

public sealed class LlmDefaultService : ILlmDefaultService
{
    private readonly CognitionDbContext _db;

    public LlmDefaultService(CognitionDbContext db)
    {
        _db = db;
    }

    public async Task<(Guid? providerId, Guid? modelId)> GetActiveDefaultAsync(CancellationToken ct)
    {
        var row = await _db.LlmGlobalDefaults
            .AsNoTracking()
            .Include(d => d.Model)
            .ThenInclude(m => m.Provider)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Priority)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row?.Model?.Provider is null || !row.Model.Provider.IsActive)
        {
            return (null, null);
        }

        return (row.Model.ProviderId, row.ModelId);
    }

    public async Task<LlmGlobalDefault?> GetAsync(CancellationToken ct)
    {
        return await _db.LlmGlobalDefaults
            .AsNoTracking()
            .Include(d => d.Model)
            .ThenInclude(m => m.Provider)
            .OrderBy(d => d.Priority)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<LlmGlobalDefault> UpsertAsync(Guid modelId, Guid? updatedByUserId, int priority, bool isActive, CancellationToken ct)
    {
        var model = await _db.Models.Include(m => m.Provider).FirstOrDefaultAsync(m => m.Id == modelId, ct).ConfigureAwait(false);
        if (model == null) throw new InvalidOperationException("Model not found.");
        if (!model.Provider.IsActive) throw new InvalidOperationException("Provider is inactive.");
        if (model.IsDeprecated) throw new InvalidOperationException("Model is deprecated.");

        var current = await _db.LlmGlobalDefaults.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (current == null)
        {
            current = new LlmGlobalDefault { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
            _db.LlmGlobalDefaults.Add(current);
        }

        current.ModelId = modelId;
        current.IsActive = isActive;
        current.Priority = priority;
        current.UpdatedByUserId = updatedByUserId;
        current.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return current;
    }
}
