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
    private static Dictionary<string, string> TestConfiguration { get; } = new()
    {
        { "DisableQuartz", "true" },
        { "CinemadleTestMode", "true" },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var entry in TestConfiguration)
        {
            builder.UseSetting(entry.Key, entry.Value);
        }

        if (configuration is null)
        {
            return;
        }

        foreach (var entry in configuration)
        {
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