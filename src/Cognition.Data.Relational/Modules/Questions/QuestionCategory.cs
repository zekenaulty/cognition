using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Questions;

public class QuestionCategory : BaseEntity
{
    public string Key { get; set; } = string.Empty; // e.g., art-questions
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
