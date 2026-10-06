using System.Net.Http.Json;
using System.Reflection;
using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cinemadle.UnitTest;

public class ApplicationStartupTests(CinemadleWebApplicationFactory factory)
    : IClassFixture<CinemadleWebApplicationFactory>, IDisposable
{
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public async Task ApplicationStartupShouldRunSetupDbContext()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage versionMessage = await client.GetAsync("/api/information/version");
        DbVersionDto versionInfo = await versionMessage.Content.ReadFromJsonAsync<DbVersionDto>();
        
        var migration = typeof(InitialCreate)
            .Assembly
            .GetTypes()
            .Where(x => x.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(DatabaseContext))
            .Select(x => x.GetCustomAttribute<MigrationAttribute>()?.Id.Split('_')[0])
            .OrderByDescending(x => x)
            .First();

        Assert.NotNull(migration);
        Assert.Equal(migration, versionInfo.MainDbVersion);
    }

    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public async Task ApplicationStartupShouldRunSetupIdentityDbContext()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage versionMessage = await client.GetAsync("/api/information/version");
        DbVersionDto versionInfo = await versionMessage.Content.ReadFromJsonAsync<DbVersionDto>();
        
        var migration = typeof(InitialCreate)
            .Assembly
            .GetTypes()
            .Where(x => x.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(IdentityContext))
            .Select(x =>  x.GetCustomAttribute<MigrationAttribute>()?.Id.Split('_')[0])
            .OrderByDescending(x => x)
            .First();

        Assert.NotNull(migration);
        Assert.Equal(migration, versionInfo.IdentityDbVersion);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}