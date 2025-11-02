using Cognition.Data.Relational.Modules.Users;
using Microsoft.AspNetCore.Authorization;

namespace Cognition.Api.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string AuthenticatedUser = "authenticated-user";
    public const string ViewerOrHigher = "viewer-or-higher";
    public const string UserOrHigher = "user-or-higher";
    public const string AdministratorOnly = "administrator-only";

    private static readonly string[] ViewerRoles =
    [
        nameof(UserRole.Viewer),
        nameof(UserRole.User),
        nameof(UserRole.Administrator)
    ];

    private static readonly string[] UserRoles =
    [
        nameof(UserRole.User),
        nameof(UserRole.Administrator)
    ];

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
        options.AddPolicy(ViewerOrHigher, policy => policy.RequireRole(ViewerRoles));
        options.AddPolicy(UserOrHigher, policy => policy.RequireRole(UserRoles));
        options.AddPolicy(AdministratorOnly, policy => policy.RequireRole(nameof(UserRole.Administrator)));
    }
}
