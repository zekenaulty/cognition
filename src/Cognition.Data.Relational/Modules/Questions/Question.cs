using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Questions;

public class Question : BaseEntity
{
    public Guid CategoryId { get; set; }
    public QuestionCategory Category { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public string[]? Tags { get; set; }
    public int? Difficulty { get; set; }
}
