namespace Cinemadle.ServiceExtensions;

public static class AddCinemadleCorsExtension
{
    public static IServiceCollection AddCinemadleCors(this IServiceCollection services, bool isDevelopment)
    {
        services.AddCors(opts =>
        {
            opts.AddPolicy("AllowFrontend",
                p =>
                {
                    if (isDevelopment)
                    {
                        p.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                    else
                    {
                        p.WithOrigins("https://cinemadle.com")
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    }
                    
                });
        });

        return services;
    }
}