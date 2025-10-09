using System.Collections.Generic;
using System.Linq;
using Cognition.Data.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
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

        var services = new ServiceCollection();
        services.AddCognitionDb(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DbContextOptions<CognitionDbContext>>();

        GetConfiguredConnectionString(options).Should().Be(connectionString);
    }

    [Fact]
    public void AddCognitionDb_FallsBackToDefaultConnectionString_WhenNoneProvided()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddCognitionDb(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<DbContextOptions<CognitionDbContext>>();

        GetConfiguredConnectionString(options).Should().Be("Host=localhost;Port=5432;Database=cognition;Username=postgres;Password=postgres");
    }

    private static string? GetConfiguredConnectionString(DbContextOptions options)
    {
        return options.Extensions
            .OfType<NpgsqlOptionsExtension>()
            .FirstOrDefault()
            ?.ConnectionString;
    }
}
