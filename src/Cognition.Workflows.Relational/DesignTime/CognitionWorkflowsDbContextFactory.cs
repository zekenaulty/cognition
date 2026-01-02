using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cognition.Workflows.Relational.DesignTime;

public class CognitionWorkflowsDbContextFactory : IDesignTimeDbContextFactory<Cognition.Workflows.Relational.CognitionWorkflowsDbContext>
{
    public Cognition.Workflows.Relational.CognitionWorkflowsDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("WorkflowsPostgres")
                 ?? config["ConnectionStrings:WorkflowsPostgres"]
                 ?? "Host=localhost;Port=5434;Database=cognition_workflows;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<Cognition.Workflows.Relational.CognitionWorkflowsDbContext>()
            .UseNpgsql(cs, o => o.EnableRetryOnFailure());

        return new Cognition.Workflows.Relational.CognitionWorkflowsDbContext(optionsBuilder.Options);
    }
}
