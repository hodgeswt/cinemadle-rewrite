using Cinemadle.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinemadle.UnitTest;

public class ApplicationStartupTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void ApplicationStartupShouldRunSetupDbContext()
    {
        _ = factory.Server;

        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var dbContext = services.GetRequiredService<DatabaseContext>();
        
        Assert.NotNull(dbContext);
        Assert.NotNull(dbContext.Database);
        Assert.Empty(dbContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    public void ApplicationStartupShouldRunSetupIdentityContext()
    {
        _ = factory.Server;
        
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var identityContext = services.GetRequiredService<IdentityContext>();
        
        Assert.NotNull(identityContext);
        Assert.NotNull(identityContext.Database);
        Assert.Empty(identityContext.Database.GetPendingMigrations());
    }
}