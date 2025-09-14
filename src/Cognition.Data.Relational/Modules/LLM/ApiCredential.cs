using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public class ApiCredential : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    // Reference to where the credential is stored, e.g., env var name or Secret Manager key
    public string KeyRef { get; set; } = string.Empty;
    public DateTime? LastUsedAtUtc { get; set; }
    public bool IsValid { get; set; } = true;
    public string? Notes { get; set; }
}
