using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Cinemadle.UnitTest;

public abstract class UnitTestAssist
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(x => x.AddConsole());

    public static ILogger<T> GetLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }
}

public class CinemadleWebApplicationFactoryBase(Dictionary<string, string>? configuration = null) : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"cinemadle-integrationtest-{Guid.NewGuid()}.db");

    private static Dictionary<string, string> TestConfiguration { get; } = new()
    {
        { "DisableQuartz", "true" },
        { "CinemadleTestMode", "true" },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        const string dbConnStringKey = "DatabaseConnectionString";
        builder.UseSetting("DatabaseConnectionString", $"DataSource={_dbPath}");

        foreach (var entry in TestConfiguration)
        {
            if (entry.Key == dbConnStringKey)
            {
                throw new ArgumentException($"Configuration value {dbConnStringKey} is not allowed to be overridden");
            }
            builder.UseSetting(entry.Key, entry.Value);
        }

        if (configuration is null)
        {
            return;
        }

        foreach (var entry in configuration)
        {
            if (entry.Key == dbConnStringKey)
            {
                throw new ArgumentException($"Configuration value {dbConnStringKey} is not allowed to be overridden");
            }
            builder.UseSetting(entry.Key, entry.Value);
        }
    }
}

public class CinemadleWebApplicationFactory() : CinemadleWebApplicationFactoryBase();

public class CinemadleWebApplicationFactoryTestModeDisabled(): CinemadleWebApplicationFactoryBase(new() { { "CinemadleTestMode", "false" } });

public class FeatureFlagWebApplicationFactory() : CinemadleWebApplicationFactoryBase(
    new()
    {
        ["CinemadleConfig:FeatureFlags:TestTrue"] = "true",
        ["CinemadleConfig:FeatureFlags:TestFalse"] = "false",
    }
);