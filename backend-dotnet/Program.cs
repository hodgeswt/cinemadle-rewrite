using Cinemadle.Database;
using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

            opts.AddSecurityRequirement(new OpenApiSecurityRequirement
               {
                   {
                       new OpenApiSecurityScheme
                       {
                           Reference = new OpenApiReference
                           {
                               Type = ReferenceType.SecurityScheme,
                               Id = "Bearer"
                           }
                       },
                       Array.Empty<string>()
                   }
               });
        });

        builder.Services.AddCors(opts =>
        {
            opts.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
        });

        builder.Services.Configure<ForwardedHeadersOptions>(opts =>
        {
            opts.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            opts.KnownNetworks.Clear();
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
        builder.Services.AddScoped<IGuessRepository, GuessRepository>();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy =>
                policy.RequireClaim(ClaimTypes.Role, nameof(CustomRoles.Admin)));

        builder.Services.AddIdentityApiEndpoints<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContext>();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        using var scope = app.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.EnsureCreated();
        IdentityContext identityDb = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        identityDb.Database.EnsureCreated();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (CustomRoles customRole in Enum.GetValues(typeof(CustomRoles)))
        {
            string roleName = customRole.ToString();
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        string? adminEmail = Environment.GetEnvironmentVariable("CINEMADLE_ADMIN_EMAIL");

        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            IdentityUser? admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is not null && !await userManager.IsInRoleAsync(admin, nameof(CustomRoles.Admin)))
            {
                await userManager.AddToRoleAsync(admin, nameof(CustomRoles.Admin));
            }
        }

        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;

            if (response.StatusCode == 404)
            {
                response.Clear();
                response.StatusCode = 302;
                response.Redirect("/index.html");
            }

            await System.Threading.Tasks.Task.CompletedTask;
        });
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapIdentityApi<IdentityUser>();
        app.UseStaticFiles();
        app.MapControllers();
        app.UseCors("AllowAllOrigins"); ;
        app.Run();
    }
}

