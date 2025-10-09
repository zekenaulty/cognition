using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cognition.Data.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace Cognition.Data.Relational.Tests;

public class EntityConfigurationReflectionTests
{
    [Fact]
    public void AllEntityTypeConfigurations_ShouldApplyAndProduceTables()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var configurations = DiscoverConfigurations();

        foreach (var (configType, entityType) in configurations)
        {
            var entity = model.FindEntityType(entityType);
            entity.Should().NotBeNull($"Configuration {configType.FullName} should register entity {entityType.FullName}");

            var tableName = entity!.GetTableName();
            tableName.Should().NotBeNullOrEmpty($"Entity {entityType.FullName} configured by {configType.FullName} should map to a table");

            var storeObject = StoreObjectIdentifier.Table(tableName!, entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                property.GetColumnName(storeObject)
                    .Should().NotBeNullOrEmpty($"Property {entityType.Name}.{property.Name} should have a column mapping");
            }
        }
    }

    private static CognitionDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cognition_shadow;Username=postgres;Password=postgres")
            .UseInternalServiceProvider(provider)
            .Options;

        return new CognitionDbContext(options);
    }

    private static IModel TryGetModel(CognitionDbContext context)
    {
        try
        {
            return context.Model;
        }
        catch (Exception ex)
        {
            throw new XunitException($"Failed to build CognitionDbContext model for configuration tests: {ex}");
        }
    }

    private static List<(Type ConfigType, Type EntityType)> DiscoverConfigurations()
    {
        var assembly = typeof(CognitionDbContext).Assembly;
        return assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .Select(t => (Type: t, Interface: GetConfigurationInterface(t)))
            .Where(t => t.Interface != null)
            .Select(t => (t.Type, t.Interface!.GetGenericArguments()[0]))
            .ToList();
    }

    private static Type? GetConfigurationInterface(Type type)
    {
        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));
    }
}