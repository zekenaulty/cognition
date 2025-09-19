using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public class ClientProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public Guid? ModelId { get; set; }
    public Model? Model { get; set; }

    public Guid? ApiCredentialId { get; set; }
    public ApiCredential? ApiCredential { get; set; }

    public string? UserName { get; set; }
    public string? BaseUrlOverride { get; set; }

    public int MaxTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.8;
    public double TopP { get; set; } = 0.95;
    public double PresencePenalty { get; set; } = 0.5;
    public double FrequencyPenalty { get; set; } = 0.1;
    public bool Stream { get; set; } = true;
    public bool LoggingEnabled { get; set; }

    public bool IsActive { get; set; } = true;

}
