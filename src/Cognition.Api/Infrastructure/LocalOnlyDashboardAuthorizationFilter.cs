using Hangfire.Dashboard;

namespace Cognition.Api.Infrastructure;

// Very simple dashboard authorization: only allow local requests in non-development environments.
// For production, replace with a proper auth filter integrated with your identity system.
public class LocalOnlyDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        if (http is null) return false;
        // Allow only local requests
        return http.Connection.RemoteIpAddress is null || (
            http.Connection.RemoteIpAddress.Equals(System.Net.IPAddress.Loopback) ||
            http.Connection.RemoteIpAddress.Equals(System.Net.IPAddress.IPv6Loopback));
    }
}
