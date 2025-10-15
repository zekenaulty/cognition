using System.Linq;
using Cognition.Data.Relational;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Planning;

public sealed class PlannerTemplateRepository : IPlannerTemplateRepository
{
    private readonly CognitionDbContext _db;
    private readonly ILogger<PlannerTemplateRepository> _logger;

    public PlannerTemplateRepository(CognitionDbContext db, ILogger<PlannerTemplateRepository> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GetTemplateAsync(string templateId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        try
        {
            return await _db.PromptTemplates
                .AsNoTracking()
                .Where(t => t.Name == templateId && t.IsActive)
                .Select(t => t.Template)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load planner template {TemplateId}.", templateId);
            return null;
        }
    }
}
