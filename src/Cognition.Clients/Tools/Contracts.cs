using Cognition.Clients.Tools.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients.Tools;

public record ToolContext(
    Guid? AgentId,
    Guid? ConversationId,
    Guid? PersonaId,
    IServiceProvider Services,
    CancellationToken Ct);

public interface ITool
{
    string Name { get; }
    // Fully-qualified type name used for DI lookup; must match DB Tool.ClassPath
    string ClassPath { get; }
    Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args);
}

public interface IToolDispatcher
{
    Task<(bool ok, object? result, string? error)> ExecuteAsync(Guid toolId, ToolContext ctx, IDictionary<string, object?> args, bool log = true);
    Task<(bool ok, PlannerResult? result, string? error)> ExecutePlannerAsync(Guid toolId, PlannerContext ctx, PlannerParameters parameters, bool log = true);
}
