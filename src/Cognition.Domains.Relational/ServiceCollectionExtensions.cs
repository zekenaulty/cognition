using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Domains.Relational;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionDomainsDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DomainsPostgres")
                               ?? configuration["ConnectionStrings:DomainsPostgres"]
                               ?? "Host=localhost;Port=5433;Database=cognition_dod;Username=postgres;Password=postgres";

        services.AddDbContext<CognitionDomainsDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
