using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class ToolSandboxOptionsSetup : IConfigureOptions<ToolSandboxOptions>
{
    private readonly IConfiguration _configuration;

    public ToolSandboxOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Configure(ToolSandboxOptions options)
    {
        _configuration.GetSection(ToolSandboxOptions.SectionName).Bind(options);
    }
}
