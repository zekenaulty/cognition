using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Data.Relational;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? configuration["ConnectionStrings:Postgres"]
                               ?? "Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres";

        services.AddDbContext<CognitionDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
        });

        // DbContextFactory can be added later if needed; avoid mixing lifetimes here.

        return services;
    }
}
