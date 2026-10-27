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
    public async Task ControllerShouldReturnAllFlags()
    {
        var response = await _client.GetAsync("/api/flags/all");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<FeatureFlagsDto>();
        
        Assert.NotNull(content);
        Assert.True(content.FeatureFlags["TestTrue"]);
        Assert.False(content.FeatureFlags["TestFalse"]);
    }
    
    [Theory]
    [InlineData("TestTrue", true)]
    [InlineData("TestFalse", false)]
    public async Task ControllerShouldReturnProperFlagValue(string flagName, bool expectedValue)
    {
        var response = await _client.GetAsync($"/api/flags/{flagName}");
        
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<FeatureFlagDto>();
        
        Assert.NotNull(content);
        Assert.Equal(flagName, content.Name);
        Assert.Equal(expectedValue, content.Value);
    }
}