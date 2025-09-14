using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cognition.Data.Relational.DesignTime;

public class CognitionDbContextFactory : IDesignTimeDbContextFactory<Cognition.Data.Relational.CognitionDbContext>
{
    public Cognition.Data.Relational.CognitionDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Build a lightweight configuration to pick up connection strings if available
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("Postgres")
                 ?? config["ConnectionStrings:Postgres"]
                 ?? "Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<Cognition.Data.Relational.CognitionDbContext>()
            .UseNpgsql(cs, o => o.EnableRetryOnFailure());

        return new Cognition.Data.Relational.CognitionDbContext(optionsBuilder.Options);
    }
}

