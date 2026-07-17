using System.Net.Http.Json;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Cinemadle.UnitTest;

public class FeatureFlagWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CinemadleConfig:FeatureFlags:TestTrue"] = "true",
                ["CinemadleConfig:FeatureFlags:TestFalse"] = "false",
            });
        });
    }
}

public class FeatureFlagsUnitTest(FeatureFlagWebApplicationFactory factory) : IClassFixture<FeatureFlagWebApplicationFactory>
{
    [Fact]
    public async Task GetAllReturnsFlags()
    {
        var logger = UnitTestAssist.GetLogger<FeatureFlagRepository>();
        var config = Mocks.GetMockedConfigRepository();
        var flagRepository = new FeatureFlagRepository(logger, config);
        
        var result = await flagRepository.GetAll();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("TestTrue"));
        Assert.True(result.ContainsKey("TestFalse"));
    }
    
    [Theory]
    [InlineData("TestTrue", true)]
    [InlineData("TestFalse", false)]
    public async Task GetFlagReturnsProperValue(string flagName, bool expectedValue)
    {
        var logger = UnitTestAssist.GetLogger<FeatureFlagRepository>();
        var config = Mocks.GetMockedConfigRepository();
        var flagRepository = new FeatureFlagRepository(logger, config);
        
        var result = await flagRepository.Get(flagName);
        
        Assert.Equal(expectedValue, result);
    }
    
    [Fact]
    public void ControllerShouldReturnAllFlags()
    {
        var client = factory.CreateClient();
        var response = client.GetAsync("/api/flags/all").Result;
        
        response.EnsureSuccessStatusCode();
        
        var content = response.Content.ReadFromJsonAsync<FeatureFlagsDto>().Result;
        
        Assert.NotNull(content);
        Assert.True(content.FeatureFlags["TestTrue"]);
        Assert.False(content.FeatureFlags["TestFalse"]);
    }
    
    [Theory]
    [InlineData("TestTrue", true)]
    [InlineData("TestFalse", false)]
    public void ControllerShouldReturnProperFlagValue(string flagName, bool expectedValue)
    {
        var client = factory.CreateClient();
        var response = client.GetAsync($"/api/flags/{flagName}").Result;
        
        response.EnsureSuccessStatusCode();
        
        var content = response.Content.ReadFromJsonAsync<FeatureFlagDto>().Result;
        
        Assert.NotNull(content);
        Assert.Equal(flagName, content.Name);
        Assert.Equal(expectedValue, content.Value);
    }
}