using Cinemadle.Controllers;
using Cinemadle.Database;
using Cinemadle.ServiceExtensions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cinemadle.UnitTest;

public class ApplicationStartupTests(CinemadleWebApplicationFactory factory)
    : IClassFixture<CinemadleWebApplicationFactory>
{
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void ApplicationStartupShouldRunSetupDbContext()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var dbContext = services.GetRequiredService<DatabaseContext>();
        
        Assert.NotNull(dbContext);
        Assert.NotNull(dbContext.Database);
        Assert.Empty(dbContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void ApplicationStartupShouldRunSetupIdentityContext()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var identityContext = services.GetRequiredService<IdentityContext>();
        
        Assert.NotNull(identityContext);
        Assert.NotNull(identityContext.Database);
        Assert.Empty(identityContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void CinemadleWebApplicationFactoryDisablesQuartz()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var config = services.GetService<IConfiguration>();
        
        Assert.NotNull(config);
        Assert.True(config.GetValue<bool>("DisableQuartz"));
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void QuartzDisabledInTestMode()
    {
        // this is a hack to make sure the extension is called
        using var scope = factory.Services.CreateScope();
        
        Assert.False(SetupCinemadleQuartzExtension.WasQuartzEnabled);
        Assert.True(SetupCinemadleQuartzExtension.WasExtensionCalled);
    }
}