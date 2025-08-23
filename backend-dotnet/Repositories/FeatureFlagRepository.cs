using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;

namespace Cinemadle.Repositories;

public class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly ILogger<FeatureFlagRepository> _logger;
    private readonly CinemadleConfig _config;

    public FeatureFlagRepository(ILogger<FeatureFlagRepository> logger, IConfigRepository config)
    {
        _logger = logger;
        _logger.LogDebug("+FeatureFlagRepository.ctor");

        _config = config.GetConfig();
        _logger.LogDebug("FeatureFlagRepository.ctor: feature flags {flags}", _config.FeatureFlags);

        _logger.LogDebug("-FeatureFlagRepository.ctor");
    }

    public Task<bool> Get(string name)
    {
        if (!_config.FeatureFlags.TryGetValue(name, out bool val))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(val);
    }

    public Task<Dictionary<string, bool>> GetAll()
    {
        return Task.FromResult(_config.FeatureFlags);
    }
}