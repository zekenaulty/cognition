using Cognition.Workflows.Definitions;
using Cognition.Workflows.Executions;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Workflows.Relational;

public class CognitionWorkflowsDbContext : DbContext
{
    public CognitionWorkflowsDbContext(DbContextOptions<CognitionWorkflowsDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowNode> WorkflowNodes => Set<WorkflowNode>();
    public DbSet<WorkflowEdge> WorkflowEdges => Set<WorkflowEdge>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CognitionWorkflowsDbContext).Assembly);
    }
}
