using System.Diagnostics.Metrics;
using System.Reflection;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Cinemadle.Metrics;

public class CinemadleMetrics : ICinemadleMetrics
{

    public static readonly string METER_NAME = "Cinemadle.Metrics";
    private static readonly string _routeCounterTemplate = "{0}:{1}/{2}"; // {method}:{baseRoute}/{route}
    private readonly Meter _meter;
    private readonly ILogger<CinemadleMetrics> _logger;
    public Counter<long> RequestsProcessedCounter { get; }
    public Dictionary<string, Counter<long>> EndpointMetrics { get; } = [];
    public static string? GetCounterName(string baseRoute, MethodBase? method)
    {
        if (method is null)
        {
            return null;
        }

        HttpMethodAttribute? httpMethod = method.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
        if (httpMethod is null || httpMethod.Template is null)
        {
            return null;
        }

        string? route = httpMethod.Template;

        if (route is null)
        {
            return null;
        }

        string counterName = string.Format(_routeCounterTemplate, httpMethod.HttpMethods.FirstOrDefault(), baseRoute, route);
        return counterName;
    }

    public CinemadleMetrics(IMeterFactory meterFactory, ILogger<CinemadleMetrics> logger)
    {
        _logger = logger;
        _logger.LogDebug("+CinemadleMetrics.ctor");
        _meter = meterFactory.Create(METER_NAME, "1.0");
        RequestsProcessedCounter = _meter.CreateCounter<long>("app.requests.processed", "Number of processed requests");

        IEnumerable<Type>? controllerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

        if (controllerTypes is null)
        {
            _logger.LogDebug("-CinemadleMetrics.ctor");
            return;
        }

        foreach (Type controller in controllerTypes)
        {
            string baseRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
            IEnumerable<MethodInfo>? endpoints = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsDefined(typeof(NonActionAttribute), false));

            if (endpoints is null || !endpoints.Any())
            {
                continue;
            }

            foreach (MethodInfo endpoint in endpoints)
            {
                string? counterName = GetCounterName(baseRoute, endpoint);
                if (string.IsNullOrWhiteSpace(counterName))
                {
                    continue;
                }

                _logger.LogDebug("CinemadleMetrics.ctor: making counter {counterName}", counterName);
                Counter<long> c = _meter.CreateCounter<long>(counterName, $"Requests processed {counterName}");
                EndpointMetrics.Add(counterName, c);
            }
        }
    }
}