using System;
using System.Collections.Generic;
using System.Linq;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Questions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Cognition.Data.Relational.Tests;

public class QuestionsConfigurationTests
{
    [Fact]
    public void QuestionCategoryConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(QuestionCategory));
        entity.Should().NotBeNull();

        var tableName = entity!.GetTableName();
        tableName.Should().Be("question_categories");
        entity.GetSchema().Should().BeNull();

        AssertColumn(entity, nameof(QuestionCategory.Id), "id", isNullable: false);
        AssertColumn(entity, nameof(QuestionCategory.Key), "key", isNullable: false, maxLength: 128);
        AssertColumn(entity, nameof(QuestionCategory.Name), "name", isNullable: false, maxLength: 128);
        AssertColumn(entity, nameof(QuestionCategory.Description), "description", isNullable: true);
        AssertColumn(entity, nameof(QuestionCategory.CreatedAtUtc), "created_at_utc", isNullable: false);
        AssertColumn(entity, nameof(QuestionCategory.UpdatedAtUtc), "updated_at_utc", isNullable: true);

        var index = entity.GetIndexes().Should().ContainSingle().Subject;
        index.Properties.Select(p => p.Name).Should().Equal(new[] { nameof(QuestionCategory.Key) });
        index.IsUnique.Should().BeTrue();
        GetRelationalName(index).Should().Be("ux_question_categories_key");
    }

    [Fact]
    public void QuestionConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(Question));
        entity.Should().NotBeNull();

        var tableName = entity!.GetTableName();
        tableName.Should().Be("questions");
        entity.GetSchema().Should().BeNull();

        AssertColumn(entity, nameof(Question.Id), "id", isNullable: false);
        AssertColumn(entity, nameof(Question.CategoryId), "category_id", isNullable: false);
        AssertColumn(entity, nameof(Question.Text), "text", isNullable: false);
        AssertColumn(entity, nameof(Question.Tags), "tags", isNullable: true);
        AssertColumn(entity, nameof(Question.Difficulty), "difficulty", isNullable: true);
        AssertColumn(entity, nameof(Question.CreatedAtUtc), "created_at_utc", isNullable: false);
        AssertColumn(entity, nameof(Question.UpdatedAtUtc), "updated_at_utc", isNullable: true);

        var index = entity.GetIndexes().Should().ContainSingle().Subject;
        index.Properties.Select(p => p.Name).Should().Equal(new[] { nameof(Question.CategoryId) });
        index.IsUnique.Should().BeFalse();
        GetRelationalName(index).Should().Be("ix_questions_category");

        var foreignKey = entity.GetForeignKeys().Should().ContainSingle().Subject;
        foreignKey.PrincipalEntityType.ClrType.Should().Be(typeof(QuestionCategory));
        foreignKey.Properties.Select(p => p.Name).Should().Equal(new[] { nameof(Question.CategoryId) });
        GetRelationalName(foreignKey).Should().Be("fk_questions_categories");
    }

    private static CognitionDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cognition_test;Username=postgres;Password=postgres")
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

    private static void AssertColumn(IEntityType entity, string propertyName, string expectedColumnName, bool isNullable, int? maxLength = null)
    {
        var property = entity.FindProperty(propertyName);
        property.Should().NotBeNull($"Property {propertyName} should be configured on {entity.Name}");
        property!.IsNullable.Should().Be(isNullable);
        GetRelationalColumnName(property).Should().Be(expectedColumnName);
        if (maxLength.HasValue)
        {
            property.GetMaxLength().Should().Be(maxLength.Value);
        }
    }

    private static string? GetRelationalColumnName(IProperty property)
    {
        return property.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value as string;
    }

    private static string? GetRelationalName(IIndex index)
    {
        return index.FindAnnotation(RelationalAnnotationNames.Name)?.Value as string;
    }

    private static string? GetRelationalName(IForeignKey foreignKey)
    {
        return foreignKey.FindAnnotation(RelationalAnnotationNames.Name)?.Value as string;
    }
}
