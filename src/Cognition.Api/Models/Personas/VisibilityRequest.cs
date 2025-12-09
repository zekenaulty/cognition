namespace Cognition.Api.Models.Personas;

public sealed class VisibilityRequest
{
    public bool IsPublic { get; init; }

    public VisibilityRequest() { }

    public VisibilityRequest(bool isPublic) => IsPublic = isPublic;
}
