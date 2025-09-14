using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cognition.Api.Infrastructure;

// Adds a global route prefix to all attribute-routed controllers, e.g., "api"
public class RoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public RoutePrefixConvention(IRouteTemplateProvider routeTemplateProvider)
    {
        _routePrefix = new AttributeRouteModel(routeTemplateProvider);
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors.Where(s => s.AttributeRouteModel != null))
            {
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
            }

            // If no route attribute present, add one with the prefix only
            if (controller.Selectors.All(s => s.AttributeRouteModel == null))
            {
                controller.Selectors.Add(new SelectorModel
                {
                    AttributeRouteModel = _routePrefix
                });
            }
        }
    }
}

