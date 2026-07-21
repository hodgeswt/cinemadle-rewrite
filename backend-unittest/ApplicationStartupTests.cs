using Cinemadle.Database;
using Cinemadle.ServiceExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cinemadle.UnitTest;

public class ApplicationStartupTests(CinemadleWebApplicationFactory factory)
    : IClassFixture<CinemadleWebApplicationFactory>, IDisposable
{
    private readonly IServiceScope _scope = factory.Services.CreateScope();
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void ApplicationStartupShouldRunSetupDbContext()
    {
        var services = _scope.ServiceProvider;
        
        var dbContext = services.GetRequiredService<DatabaseContext>();
        
        Assert.NotNull(dbContext);
        Assert.NotNull(dbContext.Database);
        Assert.Empty(dbContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void ApplicationStartupShouldRunSetupIdentityContext()
    {
        var services = _scope.ServiceProvider;
        
        var identityContext = services.GetRequiredService<IdentityContext>();
        
        Assert.NotNull(identityContext);
        Assert.NotNull(identityContext.Database);
        Assert.Empty(identityContext.Database.GetPendingMigrations());
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void CinemadleWebApplicationFactoryDisablesQuartz()
    {
        var services = _scope.ServiceProvider;
        var config = services.GetService<IConfiguration>();
        
        Assert.NotNull(config);
        Assert.True(config.GetValue<bool>("DisableQuartz"));
    }
    
    [Fact]
    [Trait("Category", "ApplicationStartup")]
    public void QuartzDisabledInTestMode()
    {
        Assert.False(SetupCinemadleQuartzExtension.WasQuartzEnabled);
        Assert.True(SetupCinemadleQuartzExtension.WasExtensionCalled);
    }

    public void Dispose()
    {
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}