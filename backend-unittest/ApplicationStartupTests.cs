using Cinemadle.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cinemadle.UnitTest;

public class ApplicationStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ApplicationStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _ = factory.Server;
    }
    
    [Fact]
    public void ApplicationStartupShouldRunSetupDbContext()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var dbContext = services.GetRequiredService<DatabaseContext>();
        
        Assert.NotNull(dbContext);
        Assert.NotNull(dbContext.Database);
        Assert.Empty(dbContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    public void ApplicationStartupShouldRunSetupIdentityContext()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var identityContext = services.GetRequiredService<IdentityContext>();
        
        Assert.NotNull(identityContext);
        Assert.NotNull(identityContext.Database);
        Assert.Empty(identityContext.Database.GetPendingMigrations());
    }
}