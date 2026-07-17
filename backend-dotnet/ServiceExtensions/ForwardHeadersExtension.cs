namespace Cinemadle.ServiceExtensions;

public static class ForwardHeadersExtension
{
    public static IServiceCollection ForwardHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(opts =>
        {
            opts.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            opts.KnownIPNetworks.Clear();
            opts.KnownProxies.Clear();
        });

        return services;
    }
}