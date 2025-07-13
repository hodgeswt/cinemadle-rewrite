using Cinemadle.Database;
using Cinemadle.Interfaces;
using Cinemadle.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

using System.Text.Json.Serialization;

namespace Cinemadle;

public class Program
{
    public static void Main(string[] args)
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
        builder.Services.AddSingleton<ITmdbRepository, TmdbRepository>();
        builder.Services.AddSingleton<IGuessRepository, GuessRepository>();

        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<IdentityUser>()
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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapIdentityApi<IdentityUser>();
        app.UseStaticFiles();
        app.MapControllers();
        app.UseCors("AllowAllOrigins"); ;
        app.Run();
    }
}

