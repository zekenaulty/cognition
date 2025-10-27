using System.Collections.Generic;
using Cognition.Data.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Cognition.Data.Relational.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCognitionDb_UsesConnectionStringFromConfigurationSection()
    {
        var connectionString = "Host=config;Port=5432;Database=cognition;Username=postgres;Password=secret";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString
            })
            .Build();

        configuration.GetConnectionString("Postgres").Should().Be(connectionString);

        var services = new ServiceCollection();
        services.AddCognitionDb(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CognitionDbContext>();
        var resolvedBuilder = new NpgsqlConnectionStringBuilder(dbContext.Database.GetDbConnection().ConnectionString);

        resolvedBuilder.Host.Should().Be("config");
        resolvedBuilder.Username.Should().Be("postgres");
        resolvedBuilder.Database.Should().Be("cognition");
    }

    [Fact]
    public void AddCognitionDb_FallsBackToDefaultConnectionString_WhenNoneProvided()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCognitionDb(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CognitionDbContext>();
        var resolvedBuilder = new NpgsqlConnectionStringBuilder(dbContext.Database.GetDbConnection().ConnectionString);

        resolvedBuilder.Host.Should().Be("localhost");
        resolvedBuilder.Username.Should().Be("postgres");
        resolvedBuilder.Database.Should().Be("cognition");
    }
}
