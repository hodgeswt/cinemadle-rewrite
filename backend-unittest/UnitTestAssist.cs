using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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

public class CinemadleWebApplicationFactory : WebApplicationFactory<Program>
{
    private static Dictionary<string, string?> TestConfiguration { get; } = new()
    {
        { "DisableQuartz", "true" },
        { "CinemadleTestMode", "true" },
    };

    private static void ApplyConfiguration(IWebHostBuilder builder, Dictionary<string, string?> config)
    {
        foreach (var entry in config)
        {
            builder.UseSetting(entry.Key, entry.Value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ApplyConfiguration(builder, TestConfiguration);
    }
    
    public HttpClient CreateClientWithConfig(Dictionary<string, string?> customConfig)
    {
        // never allow quartz to run
        customConfig.Add("DisableQuartz", "true");
        
        return WithWebHostBuilder(builder =>
        {
            ApplyConfiguration(builder, customConfig);
        }).CreateClient();
    }
}