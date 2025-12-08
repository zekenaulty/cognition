using System.ComponentModel.DataAnnotations;

namespace Cognition.Api.Models.Conversations;

public sealed class SetActiveVersionRequest
{
    [Range(0, int.MaxValue)]
    public int Index { get; init; }
}
