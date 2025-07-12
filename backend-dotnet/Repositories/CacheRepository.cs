using Cinemadle.Exceptions;
using Cinemadle.Interfaces;

using Microsoft.Extensions.Caching.Memory;

namespace Cinemadle.Repositories;

public class CacheRepository : ICacheRepository
{
    private ILogger<CacheRepository> _logger;
    private readonly int _ttl;
    private object _lock = new();
    private IMemoryCache _cache;

    public CacheRepository(ILogger<CacheRepository> logger, IConfigRepository configRepository, IMemoryCache memoryCache)
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "CacheRepository";

        _logger.LogDebug("+ctor({type})", type);

        _ttl = configRepository.GetConfig().CacheTTL;
        _cache = memoryCache;

        _logger.LogDebug("-ctor({type})", type);
    }

    public bool Set(string key, object value)
    {
        _logger.LogDebug("+Set({key})", key);
        try
        {
            lock (_lock)
            {
                _cache.Set(key, value, DateTime.Now.AddSeconds(_ttl));
            }

            _logger.LogDebug("-Set({key})", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to cache item for key {key}. Exception: {message}, StackTrace: {stackTrace}", key, ex.Message, ex.StackTrace);
            throw new CacheException($"Unable to cache item for key {key}");
        }

    }

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        _logger.LogDebug("+TryGet({key})", key);
        value = null;

        try
        {
            value = _cache.Get<T>(key);
            _logger.LogDebug("-TryGet({key})", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to get cache item for key {key}. Exception: {message}, StackTrace: {stackTrace}", key, ex.Message, ex.StackTrace);
            throw new CacheException($"Unable to get cache item for key {key}");
        }
    }
}
