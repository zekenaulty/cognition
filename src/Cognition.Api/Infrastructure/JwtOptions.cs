namespace Cognition.Api.Infrastructure;

public static class JwtOptions
{
    // The JWT signing secret resolved during app startup.
    public static string Secret { get; set; } = string.Empty;
}

