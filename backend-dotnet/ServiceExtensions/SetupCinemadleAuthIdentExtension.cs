using System.Security.Claims;
using Cinemadle.Datamodel.Domain;
using Microsoft.AspNetCore.Identity;

namespace Cinemadle.ServiceExtensions;

public static class SetupCinemadleAuthIdentExtension
{
    public static IServiceCollection SetupCinemadleAuthIdent(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy =>
                policy.RequireClaim(ClaimTypes.Role, nameof(CustomRoles.Admin)));

        services.AddIdentityApiEndpoints<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContext>();
        return services;
    }
}