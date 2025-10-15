using System.Diagnostics;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Planning;

public abstract class PlannerBase<TParameters> : IPlannerTool where TParameters : PlannerParameters
{
    private readonly ILogger _logger;
    private readonly IPlannerTelemetry _telemetry;
    private readonly IPlannerTranscriptStore _transcriptStore;
    private readonly IPlannerTemplateRepository _templateRepository;
    public string Name => Metadata.Name;
    public string ClassPath => $"{GetType().FullName}, {GetType().Assembly.GetName().Name}";

    protected PlannerBase(
        ILoggerFactory loggerFactory,
        IPlannerTelemetry telemetry,
        IPlannerTranscriptStore transcriptStore,
        IPlannerTemplateRepository templateRepository)
    {
        if (loggerFactory is null) throw new ArgumentNullException(nameof(loggerFactory));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = loggerFactory.CreateLogger(GetType());
        _transcriptStore = transcriptStore ?? throw new ArgumentNullException(nameof(transcriptStore));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
    }

    public abstract PlannerMetadata Metadata { get; }

    protected ILogger Logger => _logger;
    protected IPlannerTemplateRepository TemplateRepository => _templateRepository;

    public async Task<PlannerResult> PlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));

        var typedParameters = ConvertParameters(parameters);
        _logger.LogDebug("Planner {Planner} validating parameters type {ParameterType}.", Metadata.Name, typedParameters.GetType().Name);
        ValidateInputs(typedParameters);

        var telemetryContext = new PlannerTelemetryContext(
            ToolId: context.ToolId,
            PlannerName: Metadata.Name,
            Capabilities: Metadata.Capabilities,
            AgentId: context.ToolContext.AgentId,
            ConversationId: context.ToolContext.ConversationId,
            Environment: context.Environment);

        _logger.LogInformation("Planner {Planner} starting (toolId={ToolId}, agent={AgentId}, conversation={ConversationId}).",
            Metadata.Name, context.ToolId, context.ToolContext.AgentId, context.ToolContext.ConversationId);
        _telemetry.PlanStarted(telemetryContext);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await ExecutePlanAsync(context, typedParameters, ct).ConfigureAwait(false);
            stopwatch.Stop();
            result.AddMetric("durationMs", stopwatch.Elapsed.TotalMilliseconds);
            try
            {
                await _transcriptStore.StoreAsync(context, Metadata, result, ct).ConfigureAwait(false);
            }
            catch (Exception storeEx) when (storeEx is not OperationCanceledException)
            {
                _logger.LogWarning(storeEx, "Planner {Planner} transcript store failed.", Metadata.Name);
            }
            _logger.LogInformation("Planner {Planner} completed in {DurationMs}ms with outcome {Outcome}.",
                Metadata.Name, stopwatch.Elapsed.TotalMilliseconds, result.Outcome);
            _telemetry.PlanCompleted(telemetryContext, result);
            return result;
        }
        catch (OperationCanceledException oce)
        {
            stopwatch.Stop();
            _logger.LogWarning("Planner {Planner} cancelled after {DurationMs}ms.", Metadata.Name, stopwatch.Elapsed.TotalMilliseconds);
            _telemetry.PlanFailed(telemetryContext, oce);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Planner {Planner} failed after {DurationMs}ms.", Metadata.Name, stopwatch.Elapsed.TotalMilliseconds);
            _telemetry.PlanFailed(telemetryContext, ex);
            throw;
        }
    }

    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var plannerContext = BuildPlannerContext(ctx, args);
        var parameters = ConvertParameters(new PlannerParameters(args));
        var result = await PlanAsync(plannerContext, parameters, ctx.Ct).ConfigureAwait(false);
        return result;
    }

    protected virtual PlannerContext BuildPlannerContext(ToolContext ctx, IDictionary<string, object?> args)
    {
        ScopePath? scopePath = null;
        if (args.TryGetValue("ScopeToken", out var scopeObj) && scopeObj is ScopeToken scopeToken)
        {
            scopePath = scopeToken.ToScopePath();
        }
        else if (ctx.AgentId.HasValue || ctx.ConversationId.HasValue)
        {
            var inferred = new ScopeToken(null, null, null, ctx.AgentId, ctx.ConversationId, null, null);
            scopePath = inferred.ToScopePath();
        }

        return PlannerContext.FromToolContext(ctx, scopePath);
    }

    protected virtual void ValidateInputs(TParameters parameters)
    {
    }

    protected abstract Task<PlannerResult> ExecutePlanAsync(PlannerContext context, TParameters parameters, CancellationToken ct);

    protected virtual TParameters ConvertParameters(PlannerParameters parameters)
        => (parameters as TParameters) ?? (TParameters)Activator.CreateInstance(typeof(TParameters), parameters.AsReadOnlyDictionary())!;
}
