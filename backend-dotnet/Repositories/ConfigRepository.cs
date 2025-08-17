using Cinemadle.Datamodel.DTO;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Exceptions;
using Cinemadle.Interfaces;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cinemadle.Repositories;

public class ConfigRepository : IConfigRepository
{
    private readonly ILogger<ConfigRepository> _logger;
    private bool _configLoaded;
    private CinemadleConfig? _config;
    private readonly string _configFileName = "CinemadleConfig.json";

    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigRepository(ILogger<ConfigRepository> logger)
    {
        _logger = logger;
        string typeName = this.GetType().AssemblyQualifiedName ?? "ConfigRepository";
        logger.LogDebug("+ctor({type})", typeName);
        _config = null;

        _jsonOptions = new();
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

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
        if (_configLoaded)
        {
            return true;
        }

        try
        {
            if (!File.Exists(path))
            {
                _logger.LogError("TryLoadConfig: Path {path} does not exist.", path);
                return false;
            }

            string configJson = File.ReadAllText(path);
            _config = JsonSerializer.Deserialize<CinemadleConfig>(configJson, _jsonOptions);
            _configLoaded = true;

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
