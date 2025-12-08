using System.ComponentModel.DataAnnotations;

namespace Cognition.Api.Models.Conversations;

public sealed class AddVersionRequest
{
    [Required, StringLength(4000, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Content must contain non-whitespace characters.")]
    public string Content { get; init; } = string.Empty;

    public AddVersionRequest() { }
    public AddVersionRequest(string content)
    {
        Content = content;
    }
}
