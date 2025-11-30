using System.Net.Http.Json;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class LoggerSandboxAlertPublisher : ISandboxAlertPublisher
{
    private readonly ILogger<LoggerSandboxAlertPublisher> _logger;
    private readonly ToolSandboxAlertOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public LoggerSandboxAlertPublisher(
        ILogger<LoggerSandboxAlertPublisher> logger,
        IOptions<ToolSandboxAlertOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task PublishAsync(SandboxDecision decision, Tool tool, ToolContext context, CancellationToken ct)
    {
        if (!_options.Enabled || decision.IsAllowed)
        {
            return;
        }

        _logger.LogWarning("Sandbox alert: tool denied {ToolId} {ClassPath} mode={Mode} reason={Reason} agent={AgentId} persona={PersonaId} conversation={ConversationId}",
            tool.Id,
            tool.ClassPath,
            decision.Mode,
            decision.Reason,
            context.AgentId,
            context.PersonaId,
            context.ConversationId);

        if (!string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    toolId = tool.Id,
                    tool.ClassPath,
                    decision.Mode,
                    decision.Reason,
                    context.AgentId,
                    context.PersonaId,
                    context.ConversationId
                };
                await client.PostAsJsonAsync(_options.WebhookUrl, payload, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send sandbox alert webhook");
            }
        }
    }
}
