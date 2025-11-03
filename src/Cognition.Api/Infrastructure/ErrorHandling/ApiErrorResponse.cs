using System.Text.Json.Serialization;

namespace Cognition.Api.Infrastructure.ErrorHandling;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? Details = null)
{
    public static ApiErrorResponse Create(string code, string message, object? details = null) =>
        new(code, message, details);
}
