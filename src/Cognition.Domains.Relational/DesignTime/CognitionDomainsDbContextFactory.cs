using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cognition.Domains.Relational.DesignTime;

public class CognitionDomainsDbContextFactory : IDesignTimeDbContextFactory<Cognition.Domains.Relational.CognitionDomainsDbContext>
{
    public Cognition.Domains.Relational.CognitionDomainsDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("DomainsPostgres")
                 ?? config["ConnectionStrings:DomainsPostgres"]
                 ?? "Host=localhost;Port=5433;Database=cognition_dod;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<Cognition.Domains.Relational.CognitionDomainsDbContext>()
            .UseNpgsql(cs, o => o.EnableRetryOnFailure());

        return new Cognition.Domains.Relational.CognitionDomainsDbContext(optionsBuilder.Options);
    }
}
