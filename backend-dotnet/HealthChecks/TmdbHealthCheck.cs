using Cinemadle.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cinemadle.HealthChecks;

public class TmdbHealthCheck(ITmdbRepository tmdbRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            const int movieId = 1924;
            var movie = await tmdbRepository.GetMovieById(movieId);
            var healthy = movie?.Id == movieId;

            return healthy
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception _)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}