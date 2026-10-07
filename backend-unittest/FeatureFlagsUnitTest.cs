using System.Net.Http.Json;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Repositories;

using Cinemadle.UnitTest.Infrastructure;

namespace Cinemadle.UnitTest;


public class FeatureFlagsUnitTest(FeatureFlagWebApplicationFactory factory) : IClassFixture<FeatureFlagWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAllReturnsFlags()
    {
        var logger = UnitTestAssist.GetLogger<FeatureFlagRepository>();
        var config = CinemadleMocks.GetMockedConfigRepository();
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
        var config = CinemadleMocks.GetMockedConfigRepository();
        var flagRepository = new FeatureFlagRepository(logger, config);
        
        var result = await flagRepository.Get(flagName);
        
        Assert.Equal(expectedValue, result);
    }
    
    [Fact]
    public void ControllerShouldReturnAllFlags()
    {
        var response = _client.GetAsync("/api/flags/all").Result;
        
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
        var response = _client.GetAsync($"/api/flags/{flagName}").Result;
        
        response.EnsureSuccessStatusCode();
        
        var content = response.Content.ReadFromJsonAsync<FeatureFlagDto>().Result;
        
        Assert.NotNull(content);
        Assert.Equal(flagName, content.Name);
        Assert.Equal(expectedValue, content.Value);
    }
}