namespace Cognition.Api.Infrastructure;

public static class JwtOptions
{
    // Development fallback secret (must be >= 32 bytes). Used only if no
    // environment/config secret is provided. Never allowed in Production.
    public const string DevFallbackSecret = "dev-secret-change-me-please-change-32bytes!dev";

    // The JWT signing secret resolved during app startup.
    public static string Secret { get; set; } = string.Empty;
}
