using System.Collections.Generic;
using System.Diagnostics;
using Cognition.Clients.Scope;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Planning;

public abstract class PlannerBase<TParameters> : IPlannerTool where TParameters : PlannerParameters
{
    private readonly ILogger _logger;
    private readonly IPlannerTelemetry _telemetry;
    private readonly IPlannerTranscriptStore _transcriptStore;
    private readonly IPlannerTemplateRepository _templateRepository;
    private readonly PlannerCritiqueOptions _critiqueOptions;
    private readonly IScopePathBuilder _scopePathBuilder;
    private PlannerCritiqueManager? _critiqueManager;
    public string Name => Metadata.Name;
    public string ClassPath => $"{GetType().FullName}, {GetType().Assembly.GetName().Name}";

    protected PlannerBase(
        ILoggerFactory loggerFactory,
        IPlannerTelemetry telemetry,
        IPlannerTranscriptStore transcriptStore,
        IPlannerTemplateRepository templateRepository,
        IOptions<PlannerCritiqueOptions> critiqueOptions,
        IScopePathBuilder scopePathBuilder)
    {
        if (loggerFactory is null) throw new ArgumentNullException(nameof(loggerFactory));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = loggerFactory.CreateLogger(GetType());
        _transcriptStore = transcriptStore ?? throw new ArgumentNullException(nameof(transcriptStore));
        _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        _critiqueOptions = critiqueOptions?.Value ?? new PlannerCritiqueOptions();
        _scopePathBuilder = scopePathBuilder ?? throw new ArgumentNullException(nameof(scopePathBuilder));
    }

    public abstract PlannerMetadata Metadata { get; }

    protected ILogger Logger => _logger;
    protected IPlannerTemplateRepository TemplateRepository => _templateRepository;
    protected PlannerCritiqueManager Critique => _critiqueManager ?? PlannerCritiqueManager.Disabled;

    public async Task<PlannerResult> PlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));

        var typedParameters = ConvertParameters(parameters);
        _logger.LogDebug("Planner {Planner} validating parameters type {ParameterType}.", Metadata.Name, typedParameters.GetType().Name);
        ValidateInputs(typedParameters);
        await EnsureRequiredTemplatesAsync(ct).ConfigureAwait(false);

        var effectiveContext = context;
        var personaOrAgentId = effectiveContext.ToolContext.PersonaId ?? effectiveContext.PrimaryAgentId;
        var critiqueProfile = Metadata.CritiqueProfile ?? PlannerCritiqueProfile.Disabled;
        var metadataAllowsCritique = critiqueProfile.AllowsPersona(personaOrAgentId);
        var finalCritiqueEnabled = _critiqueOptions.IsPlannerEnabled(Metadata.Name, personaOrAgentId, metadataAllowsCritique);
        if (effectiveContext.SupportsSelfCritique != finalCritiqueEnabled)
        {
            effectiveContext = effectiveContext with { SupportsSelfCritique = finalCritiqueEnabled };
        }

        var telemetryContext = new PlannerTelemetryContext(
            ToolId: effectiveContext.ToolId,
            PlannerName: Metadata.Name,
            Capabilities: Metadata.Capabilities,
            AgentId: effectiveContext.ToolContext.AgentId,
            ConversationId: effectiveContext.ToolContext.ConversationId,
            Environment: effectiveContext.Environment,
            ScopePath: effectiveContext.ScopePath?.Canonical,
            PrimaryAgentId: effectiveContext.PrimaryAgentId,
            SupportsSelfCritique: effectiveContext.SupportsSelfCritique,
            TelemetryTags: Metadata.TelemetryTags,
            CorrelationId: effectiveContext.CorrelationId);

        _logger.LogInformation("Planner {Planner} starting (toolId={ToolId}, agent={AgentId}, conversation={ConversationId}).",
            Metadata.Name, effectiveContext.ToolId, effectiveContext.ToolContext.AgentId, effectiveContext.ToolContext.ConversationId);
        await _telemetry.PlanStartedAsync(telemetryContext, ct).ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        var critiqueEnabled = effectiveContext.SupportsSelfCritique;
        var critiqueBudget = _critiqueOptions.ResolveBudget(Metadata.Name);
        var critiqueManager = PlannerCritiqueManager.Create(critiqueEnabled, critiqueBudget, _logger);
        _critiqueManager = critiqueManager;
        try
        {
            var result = await ExecutePlanAsync(effectiveContext, typedParameters, ct).ConfigureAwait(false);
            critiqueManager.ApplyMetrics(result);
            stopwatch.Stop();
            result.AddMetric("durationMs", stopwatch.Elapsed.TotalMilliseconds);
            try
            {
                await _transcriptStore.StoreAsync(effectiveContext, Metadata, result, ct).ConfigureAwait(false);
            }
            catch (Exception storeEx) when (storeEx is not OperationCanceledException)
            {
                _logger.LogWarning(storeEx, "Planner {Planner} transcript store failed.", Metadata.Name);
            }
            _logger.LogInformation("Planner {Planner} completed in {DurationMs}ms with outcome {Outcome}.",
                Metadata.Name, stopwatch.Elapsed.TotalMilliseconds, result.Outcome);
            await _telemetry.PlanCompletedAsync(telemetryContext, result, ct).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException oce)
        {
            stopwatch.Stop();
            _logger.LogWarning("Planner {Planner} cancelled after {DurationMs}ms.", Metadata.Name, stopwatch.Elapsed.TotalMilliseconds);
            await _telemetry.PlanFailedAsync(telemetryContext, oce, ct).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Planner {Planner} failed after {DurationMs}ms.", Metadata.Name, stopwatch.Elapsed.TotalMilliseconds);
            await _telemetry.PlanFailedAsync(telemetryContext, ex, ct).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _critiqueManager = null;
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
            if (_scopePathBuilder.TryBuild(scopeToken, out var built))
            {
                scopePath = built;
            }
        }
        else if (_scopePathBuilder.TryBuild(
                     tenantId: null,
                     appId: null,
                     personaId: ctx.PersonaId,
                     agentId: ctx.AgentId,
                     conversationId: ctx.ConversationId,
                     planId: null,
                     projectId: null,
                     worldId: null,
                     out var inferred))
        {
            scopePath = inferred;
        }

        return PlannerContext.FromToolContext(ctx, scopePath);
    }

    protected virtual void ValidateInputs(TParameters parameters)
    {
    }

    protected abstract Task<PlannerResult> ExecutePlanAsync(PlannerContext context, TParameters parameters, CancellationToken ct);

    protected virtual TParameters ConvertParameters(PlannerParameters parameters)
        => (parameters as TParameters) ?? (TParameters)Activator.CreateInstance(typeof(TParameters), parameters.AsReadOnlyDictionary())!;

    private async Task EnsureRequiredTemplatesAsync(CancellationToken ct)
    {
        if (Metadata.Steps.Count == 0)
        {
            return;
        }

        var checkedTemplates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in Metadata.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.TemplateId))
            {
                continue;
            }

            if (!checkedTemplates.Add(step.TemplateId))
            {
                continue;
            }

            var template = await _templateRepository.GetTemplateAsync(step.TemplateId, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new InvalidOperationException($"Planner '{Metadata.Name}' requires template '{step.TemplateId}' for step '{step.Id}', but it was not found.");
            }
        }
    }
}
