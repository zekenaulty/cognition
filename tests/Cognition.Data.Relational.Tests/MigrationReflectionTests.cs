using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Cognition.Data.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using Xunit.Sdk;

namespace Cognition.Data.Relational.Tests;

public class MigrationReflectionTests
{
    private static readonly HashSet<string> MigrationsAllowedToBeEmpty = new(StringComparer.Ordinal)
    {
        "20250914043550_NamespaceRefactor",
    };

    private static readonly HashSet<string> IrreversibleMigrations = new(StringComparer.Ordinal)
    {
        "20250919034730_SeedFictionTools",
        "20251005102500_RemoveLegacyFictionTools",
    };

    [Fact]
    public void AllMigrations_ShouldApplyAndRollbackSequentially()
    {
        var migrations = DiscoverMigrations();
        var applied = new List<string>();

        foreach (var migrationInfo in migrations)
        {
            ApplyMigrationUp(migrationInfo);
            applied.Add(migrationInfo.Id);
        }

        for (var i = migrations.Count - 1; i >= 0; i--)
        {
            ApplyMigrationDown(migrations[i]);
            applied.RemoveAt(applied.Count - 1);
        }

        applied.Should().BeEmpty("all migrations should roll back cleanly");
    }

    [Fact]
    public void CanJumpToSpecificMigrationById()
    {
        var migrations = DiscoverMigrations();
        migrations.Should().NotBeEmpty();

        var orderedIds = migrations.Select(m => m.Id).ToList();
        var jumpTarget = orderedIds[orderedIds.Count / 2];

        var applied = new List<string>();
        foreach (var migrationInfo in migrations)
        {
            ApplyMigrationUp(migrationInfo);
            applied.Add(migrationInfo.Id);
            if (migrationInfo.Id == jumpTarget)
            {
                break;
            }
        }

        applied.Should().Contain(jumpTarget);

        while (applied.Count > 0)
        {
            var id = applied[^1];
            var migration = migrations.Single(m => m.Id == id);
            ApplyMigrationDown(migration);
            applied.RemoveAt(applied.Count - 1);
        }
    }

    private static void ApplyMigrationUp(MigrationInfo migrationInfo)
    {
        var builder = new MigrationBuilder("Npgsql");
        InvokeMigrationMethod(migrationInfo.Instance, "Up", builder);

        if (!builder.Operations.Any())
        {
            if (!MigrationsAllowedToBeEmpty.Contains(migrationInfo.Id))
            {
                throw new XunitException($"Migration {migrationInfo.Id} ({migrationInfo.Instance.GetType().Name}) produced no operations in Up()");
            }

            return;
        }
    }

    private static void ApplyMigrationDown(MigrationInfo migrationInfo)
    {
        var builder = new MigrationBuilder("Npgsql");

        if (IrreversibleMigrations.Contains(migrationInfo.Id))
        {
            Action act = () => InvokeMigrationMethod(migrationInfo.Instance, "Down", builder);
            act.Should().ThrowExactly<NotSupportedException>();
            return;
        }

        InvokeMigrationMethod(migrationInfo.Instance, "Down", builder);
        // Allow empty Down() for reversible migrations, but enforce the method runs without throwing
    }

    private static List<MigrationInfo> DiscoverMigrations()
    {
        var assembly = typeof(CognitionDbContext).Assembly;
        var migrationTypes = assembly.GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.GetCustomAttribute<MigrationAttribute>()?.Id ?? string.Empty)
            .ToList();

        var result = new List<MigrationInfo>();
        foreach (var type in migrationTypes)
        {
            var attribute = type.GetCustomAttribute<MigrationAttribute>()
                ?? throw new XunitException($"Migration {type.FullName} is missing MigrationAttribute");

            var instance = Activator.CreateInstance(type) as Migration
                ?? throw new XunitException($"Failed to instantiate migration {type.FullName}");

            result.Add(new MigrationInfo(attribute.Id, type, instance));
        }

        return result;
    }

    private record MigrationInfo(string Id, Type Type, Migration Instance);

    private static void InvokeMigrationMethod(Migration migration, string methodName, MigrationBuilder builder)
    {
        var method = typeof(Migration).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null)
        {
            throw new XunitException($"Migration {migration.GetType().FullName} is missing method {methodName}");
        }

        try
        {
            method.Invoke(migration, new object[] { builder });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }
}
