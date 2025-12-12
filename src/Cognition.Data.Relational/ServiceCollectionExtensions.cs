using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cognition.Data.Relational;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionDb(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? configuration["ConnectionStrings:Postgres"]
                               ?? "Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres";

        // Build a shared Npgsql data source so dynamic JSON is enabled once.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton<NpgsqlDataSource>(dataSource);

        services.AddDbContext<CognitionDbContext>((provider, options) =>
        {
            var sharedDataSource = provider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(sharedDataSource, o => o.EnableRetryOnFailure());
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        // DbContextFactory can be added later if needed; avoid mixing lifetimes here.

        return services;
    }
}
