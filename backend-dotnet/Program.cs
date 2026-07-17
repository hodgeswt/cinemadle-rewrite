using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Cinemadle.HealthChecks;
using NLog.Extensions.Logging;
using Cinemadle.Jobs;
using Cinemadle.ServiceExtensions;
using Quartz;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle;

public class Program
{
    private static void MigrateDatabases(IServiceScope scope, ILogger<Program> logger)
    {
        logger.LogInformation("Ensuring databases are created");
        
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.Migrate();
        logger.LogInformation("DbContext created");
        
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        identityDb.Database.Migrate();
        
        logger.LogInformation("IdentityContext created");
    }

    private static async Task CreateRoles(IServiceScope scope, ILogger<Program> logger)
    {
        logger.LogInformation("Ensuring roles are created");
        
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var customRole in Enum.GetValues<CustomRoles>())
        {
            var roleName = customRole.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogInformation("Creating role {RoleName}", roleName);
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
            else
            {
                logger.LogInformation("Role {RoleName} already exists", roleName);
            }
        }
    }

    private static async Task SetupAdmin(IServiceScope scope, ILogger<Program> logger)
    {
        var adminEmail = Environment.GetEnvironmentVariable("CINEMADLE_ADMIN_EMAIL");

        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogInformation("Admin email found");
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is not null && !await userManager.IsInRoleAsync(admin, nameof(CustomRoles.Admin)))
            {
                logger.LogInformation("Assigning admin role to user with email");
                await userManager.AddToRoleAsync(admin, nameof(CustomRoles.Admin));
            }
            else
            {
                logger.LogInformation("Admin user not found or already assigned");
            }
        }
        else
        {
            logger.LogInformation("No admin to configure");
        }
    }
    
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var dbConnectionString = builder.Configuration.GetSection("DatabaseConnectionString").Value ?? string.Empty;
        var logConfiguration = builder.Configuration.GetSection("NLog");

        builder.Services
            .AddCinemadleOpenApi()
            .AddCinemadleCors(builder.Environment.IsDevelopment())
            .ForwardHeaders()
            .AddMemoryCache()
            .RegisterCinemadleServices(dbConnectionString)
            .SetupCinemadleQuartz()
            .SetupCinemadleAuthIdent()
            .SetupCinemadleLogging(logConfiguration);

        builder.Services
            .AddHealthChecks()
            .AddCheck<TmdbHealthCheck>("tmdb")
                .AddDbContextCheck<DatabaseContext>()
                .AddDbContextCheck<IdentityContext>();
        
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        using var scope = app.Services.CreateScope();

        MigrateDatabases(scope, logger);
        await CreateRoles(scope, logger);
        await SetupAdmin(scope, logger);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        
        app.UseAuthorization();
        app.MapGroup("/api/cinemadle")
           .MapIdentityApi<IdentityUser>();
        app.MapHealthChecks("/healthz");
        app.MapControllers();
        app.UseCors("AllowFrontend");

        logger.LogInformation("cinemadle started at {Time}", DateTime.UtcNow);
        await app.RunAsync();
    }
}

