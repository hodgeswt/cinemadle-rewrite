using System.Diagnostics.Metrics;

namespace Cinemadle.Interfaces;

public interface ICinemadleMetrics
{
    Dictionary<string, Counter<long>> EndpointMetrics { get; }
}