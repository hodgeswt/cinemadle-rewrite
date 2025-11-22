using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json.Serialization;
using NLog.Extensions.Logging;
using Cinemadle.Jobs;
using Quartz;
using Microsoft.OpenApi;

namespace Cinemadle;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opts =>
        {
            opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Cinemadle JWT",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            opts.AddSecurityRequirement((document) => new OpenApiSecurityRequirement()
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });

        builder.Services.AddCors(opts =>
        {
            opts.AddPolicy("AllowFrontend",
                p =>
                {
                    if (builder.Environment.IsDevelopment())
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

        builder.Services.Configure<ForwardedHeadersOptions>(opts =>
        {
            opts.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            opts.KnownIPNetworks.Clear();
            opts.KnownProxies.Clear();
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddMemoryCache();

        builder.Services.AddDbContext<DatabaseContext>();
        builder.Services.AddDbContext<IdentityContext>();

        builder.Services.AddSingleton<IConfigRepository, ConfigRepository>();
        builder.Services.AddSingleton<ICacheRepository, CacheRepository>();
        builder.Services.AddScoped<ITmdbRepository, TmdbRepository>();
        builder.Services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        builder.Services.AddScoped<IGuessRepository, GuessRepository>();
        builder.Services.AddScoped<IPaymentRepository, StripePaymentRepository>();
        builder.Services.AddScoped<CustomGameRemovalJob>();

        builder.Services.AddQuartz(qb =>
        {
            JobKey jobKey = new(nameof(CustomGameRemovalJob));
            qb.AddJob<CustomGameRemovalJob>(opts => opts.WithIdentity(jobKey));
            qb.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(CustomGameRemovalJob)}-trigger")
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromHours(24))
                    .RepeatForever()));
        });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy =>
                policy.RequireClaim(ClaimTypes.Role, nameof(CustomRoles.Admin)));

        builder.Services.AddIdentityApiEndpoints<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContext>();

        NLog.LogManager.Configuration = new NLogLoggingConfiguration(builder.Configuration.GetSection("NLog"));

        builder.Services.AddLogging(l =>
        {
            l.ClearProviders();
            l.AddNLog();
        });

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("cinemadle started at {Time}", DateTime.UtcNow);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        logger.LogInformation("Ensuring database is created");
        using var scope = app.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Database ensured created");
        logger.LogInformation("Ensuring identity database is created");
        IdentityContext identityDb = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        identityDb.Database.EnsureCreated();
        logger.LogInformation("Identity database ensured created");

        logger.LogInformation("Ensuring roles are created");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (CustomRoles customRole in Enum.GetValues(typeof(CustomRoles)))
        {
            string roleName = customRole.ToString();
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

        logger.LogInformation("Roles ensured created");
        logger.LogInformation("Checking for admin user assignment");
        string? adminEmail = Environment.GetEnvironmentVariable("CINEMADLE_ADMIN_EMAIL");

        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogInformation("Admin email found");
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            IdentityUser? admin = await userManager.FindByEmailAsync(adminEmail);
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

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseAuthorization();
        app.MapIdentityApi<IdentityUser>();
        app.MapControllers();
        app.UseCors("AllowFrontend");

        logger.LogInformation("Starting application");
        app.Run();
    }
}

