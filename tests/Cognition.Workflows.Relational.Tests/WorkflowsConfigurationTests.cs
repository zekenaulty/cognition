using System;
using System.Collections.Generic;
using System.Linq;
using Cognition.Workflows.Definitions;
using Cognition.Workflows.Executions;
using Cognition.Workflows.Relational;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Cognition.Workflows.Relational.Tests;

public class WorkflowsConfigurationTests
{
    [Fact]
    public void WorkflowDefinitionConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(WorkflowDefinition));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("workflow_definitions");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(WorkflowDefinition.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowDefinition.Name), "name", isNullable: false, maxLength: 200);
        AssertColumn(entity!, nameof(WorkflowDefinition.Version), "version", isNullable: false, maxLength: 64);

        var uniqueIndex = FindIndex(entity!, nameof(WorkflowDefinition.Name), nameof(WorkflowDefinition.Version));
        uniqueIndex.IsUnique.Should().BeTrue();
        GetRelationalName(uniqueIndex).Should().Be("ux_workflow_definitions_name_version");
    }

    [Fact]
    public void WorkflowNodeConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(WorkflowNode));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("workflow_nodes");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(WorkflowNode.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowNode.WorkflowDefinitionId), "workflow_definition_id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowNode.Key), "key", isNullable: false, maxLength: 200);
        AssertColumn(entity!, nameof(WorkflowNode.NodeType), "node_type", isNullable: false, maxLength: 200);

        var uniqueIndex = FindIndex(entity!, nameof(WorkflowNode.WorkflowDefinitionId), nameof(WorkflowNode.Key));
        uniqueIndex.IsUnique.Should().BeTrue();
        GetRelationalName(uniqueIndex).Should().Be("ux_workflow_nodes_definition_key");

        var definitionIndex = FindIndex(entity!, nameof(WorkflowNode.WorkflowDefinitionId));
        definitionIndex.IsUnique.Should().BeFalse();
        GetRelationalName(definitionIndex).Should().Be("ix_workflow_nodes_definition_id");

        var definitionForeignKey = FindForeignKey(entity!, nameof(WorkflowNode.WorkflowDefinitionId));
        GetRelationalName(definitionForeignKey).Should().Be("fk_workflow_nodes_definition");
    }

    [Fact]
    public void WorkflowEdgeConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(WorkflowEdge));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("workflow_edges");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(WorkflowEdge.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowEdge.WorkflowDefinitionId), "workflow_definition_id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowEdge.FromNodeId), "from_node_id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowEdge.ToNodeId), "to_node_id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowEdge.Kind), "kind", isNullable: false, maxLength: 200);

        var definitionIndex = FindIndex(entity!, nameof(WorkflowEdge.WorkflowDefinitionId));
        definitionIndex.IsUnique.Should().BeFalse();
        GetRelationalName(definitionIndex).Should().Be("ix_workflow_edges_definition_id");

        var fromIndex = FindIndex(entity!, nameof(WorkflowEdge.WorkflowDefinitionId), nameof(WorkflowEdge.FromNodeId));
        GetRelationalName(fromIndex).Should().Be("ix_workflow_edges_from_node");

        var toIndex = FindIndex(entity!, nameof(WorkflowEdge.WorkflowDefinitionId), nameof(WorkflowEdge.ToNodeId));
        GetRelationalName(toIndex).Should().Be("ix_workflow_edges_to_node");

        var fromForeignKey = FindForeignKey(entity!, nameof(WorkflowEdge.FromNodeId));
        GetRelationalName(fromForeignKey).Should().Be("fk_workflow_edges_from_node");

        var toForeignKey = FindForeignKey(entity!, nameof(WorkflowEdge.ToNodeId));
        GetRelationalName(toForeignKey).Should().Be("fk_workflow_edges_to_node");
    }

    [Fact]
    public void WorkflowExecutionConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(WorkflowExecution));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("workflow_executions");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(WorkflowExecution.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowExecution.WorkflowDefinitionId), "workflow_definition_id", isNullable: false);
        AssertColumn(entity!, nameof(WorkflowExecution.Status), "status", isNullable: false);

        var definitionIndex = FindIndex(entity!, nameof(WorkflowExecution.WorkflowDefinitionId));
        definitionIndex.IsUnique.Should().BeFalse();
        GetRelationalName(definitionIndex).Should().Be("ix_workflow_executions_definition_id");

        var statusIndex = FindIndex(entity!, nameof(WorkflowExecution.WorkflowDefinitionId), nameof(WorkflowExecution.Status));
        statusIndex.IsUnique.Should().BeFalse();
        GetRelationalName(statusIndex).Should().Be("ix_workflow_executions_definition_status");
    }

    private static CognitionWorkflowsDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<CognitionWorkflowsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cognition_workflows_shadow;Username=postgres;Password=postgres")
            .UseInternalServiceProvider(provider)
            .Options;

        return new CognitionWorkflowsDbContext(options);
    }

    private static IModel TryGetModel(CognitionWorkflowsDbContext context)
    {
        try
        {
            return context.Model;
        }
        catch (Exception ex)
        {
            throw new XunitException($"Failed to build CognitionWorkflowsDbContext model for configuration tests: {ex}");
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

    private static IIndex FindIndex(IEntityType entity, params string[] propertyNames)
    {
        var matches = entity.GetIndexes()
            .Where(index => index.Properties.Select(p => p.Name).SequenceEqual(propertyNames))
            .ToList();

        matches.Should().ContainSingle($"Expected index on {entity.ClrType.Name}({string.Join(", ", propertyNames)})");
        return matches[0];
    }

    private static IForeignKey FindForeignKey(IEntityType entity, params string[] propertyNames)
    {
        var matches = entity.GetForeignKeys()
            .Where(foreignKey => foreignKey.Properties.Select(p => p.Name).SequenceEqual(propertyNames))
            .ToList();

        matches.Should().ContainSingle($"Expected foreign key on {entity.ClrType.Name}({string.Join(", ", propertyNames)})");
        return matches[0];
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
