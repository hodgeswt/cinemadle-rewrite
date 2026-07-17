using NLog.Extensions.Logging;

namespace Cinemadle.ServiceExtensions;

public static class SetupCinemadleLoggingExtension
{
    public static IServiceCollection SetupCinemadleLogging(this IServiceCollection services, IConfigurationSection logConfiguration)
    {
        NLog.LogManager.Configuration = new NLogLoggingConfiguration(logConfiguration);

        services.AddLogging(l =>
        {
            l.ClearProviders();
            l.AddNLog();
        });
        
        return services;
    }
}