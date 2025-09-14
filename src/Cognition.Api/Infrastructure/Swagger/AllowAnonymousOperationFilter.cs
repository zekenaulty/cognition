using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cognition.Api.Infrastructure.Swagger;

public class AllowAnonymousOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var type = method.DeclaringType;

        var hasAllowAnonymous =
            method.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
            (type != null && type.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any());

        if (hasAllowAnonymous)
        {
            // Remove global security requirement for anonymous endpoints
            operation.Security.Clear();
        }
    }
}

