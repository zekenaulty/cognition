using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g., OpenAI, Ollama, Gemini
    public string DisplayName { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Model> Models { get; set; } = [];
}
