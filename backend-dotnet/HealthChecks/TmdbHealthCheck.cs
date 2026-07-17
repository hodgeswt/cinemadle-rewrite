using Cinemadle.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cinemadle.HealthChecks;

public class TmdbHealthCheck(ITmdbRepository tmdbRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            return await tmdbRepository.ValidateApiKey()
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}