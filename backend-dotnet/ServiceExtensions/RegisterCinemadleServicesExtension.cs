using Cinemadle.Database;
using Cinemadle.Interfaces;
using Cinemadle.Jobs;
using Cinemadle.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.ServiceExtensions;

public static class RegisterCinemadleServicesExtension
{
    public static IServiceCollection RegisterCinemadleServices(this IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContext<DatabaseContext>(options => options.UseSqlite(DatabaseContext.CreateDbConnectionString(dbConnectionString)));
        services.AddDbContext<IdentityContext>(options => options.UseSqlite(DatabaseContext.CreateDbConnectionString(dbConnectionString)));

        services.AddSingleton<IConfigRepository, ConfigRepository>();
        services.AddSingleton<ICacheRepository, CacheRepository>();
        services.AddScoped<ITmdbRepository, TmdbRepository>();
        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<IGuessRepository, GuessRepository>();
        services.AddScoped<IHintRepository, HintRepository>();
        services.AddScoped<CustomGameRemovalJob>();
        return services;
    }
}