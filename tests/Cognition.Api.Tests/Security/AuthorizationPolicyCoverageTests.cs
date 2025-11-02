using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Cognition.Api.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Cognition.Api.Tests.Security;

public class AuthorizationPolicyCoverageTests
{
    private static readonly string[] KnownPolicies =
    [
        AuthorizationPolicies.AuthenticatedUser,
        AuthorizationPolicies.ViewerOrHigher,
        AuthorizationPolicies.UserOrHigher,
        AuthorizationPolicies.AdministratorOnly
    ];

    [Fact]
    public void ControllersAndHubsDeclareAuthorizeAttributes()
    {
        var assembly = typeof(AuthorizationPolicies).Assembly;
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "Cognition.Api.Controllers")
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) || typeof(Hub).IsAssignableFrom(t))
            .ToList();

        var missing = types
            .Where(t => !t.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any())
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        missing.Should().BeEmpty("all controllers and hubs should declare an authorization policy");
    }

    [Fact]
    public void AuthorizationPoliciesMustUseKnownNames()
    {
        var assembly = typeof(AuthorizationPolicies).Assembly;
        var authorizeAttributes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "Cognition.Api.Controllers")
            .SelectMany(t => GetAuthorizeAttributes(t)
                .Concat(t.GetMethods().SelectMany(GetAuthorizeAttributes)))
            .ToList();

        var unknown = authorizeAttributes
            .Select(attr => attr.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Where(policy => !KnownPolicies.Contains(policy!))
            .Distinct()
            .ToList();

        unknown.Should().BeEmpty("policy names should match AuthorizationPolicies constants");
    }

    private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(MemberInfo member) =>
        member.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>();
}
