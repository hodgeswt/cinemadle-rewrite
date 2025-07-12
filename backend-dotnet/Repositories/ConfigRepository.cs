using Cinemadle.Datamodel;
using Cinemadle.Exceptions;
using Cinemadle.Interfaces;

using System.Text.Json;

namespace Cinemadle.Repositories;

public class ConfigRepository : IConfigRepository
{
    private ILogger<ConfigRepository> _logger;
    private bool _configLoaded;
    private CinemadleConfig? _config;
    private readonly string _configFileName = "CinemadleConfig.json";

    public ConfigRepository(ILogger<ConfigRepository> logger)
    {
        _logger = logger;
        string typeName = this.GetType().AssemblyQualifiedName ?? "ConfigRepository";
        logger.LogDebug("+ctor({type})", typeName);
        _config = null;

        if (!TryLoadConfig(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _configFileName)))
        {
            throw new ObjectInstantationException(typeName);
        }

        _configLoaded = true;
        logger.LogDebug("-ctor({type})", typeName);
    }

    public bool IsLoaded()
    {
        return _configLoaded;
    }

    public CinemadleConfig GetConfig()
    {
        if (!IsLoaded() || _config == null)
        {
            throw new InvalidOperationException("Unable to get undefined config");
        }

        return _config;
    }

    public bool TryLoadConfig(string path)
    {
        _logger.LogDebug("+TryLoadConfig");

        try
        {
            if (!File.Exists(path))
            {
                _logger.LogError("TryLoadConfig: Path {path} does not exist.", path);
                return false;
            }

            string configJson = File.ReadAllText(path);
            _config = JsonSerializer.Deserialize<CinemadleConfig>(configJson);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("TryLoadConfig: Exception thrown. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            return false;
        }
        finally
        {
            _logger.LogDebug("-TryLoadConfig");
        }
    }
}
