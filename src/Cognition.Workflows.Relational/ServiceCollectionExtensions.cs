using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Workflows.Relational;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionWorkflowsDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WorkflowsPostgres")
                               ?? configuration["ConnectionStrings:WorkflowsPostgres"]
                               ?? "Host=localhost;Port=5434;Database=cognition_workflows;Username=postgres;Password=postgres";

        services.AddDbContext<CognitionWorkflowsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
