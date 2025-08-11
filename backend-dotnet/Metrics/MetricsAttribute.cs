using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Cinemadle.Metrics;

[AttributeUsage(AttributeTargets.Method)]
public class MetricsAttribute : ActionFilterAttribute 
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is not ControllerBase controller)
        {
            return;
        }

        if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return;
        }

        var controllerType = controller.GetType();
        var baseRoute = controllerType.GetCustomAttribute<RouteAttribute>()?.Template ?? "base";

        var counterName = CinemadleMetrics.GetCounterName(baseRoute, controllerActionDescriptor.MethodInfo);
        if (string.IsNullOrWhiteSpace(counterName))
        {
            return;
        }

        ICinemadleMetrics? metrics = context.HttpContext.RequestServices.GetService<ICinemadleMetrics>();
        metrics?.EndpointMetrics[counterName]?.Add(1);

        base.OnActionExecuting(context);
    }
}