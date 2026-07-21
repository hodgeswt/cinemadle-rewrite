namespace Cinemadle.UnitTest;

public class TestModeEndpointsTests(CinemadleWebApplicationFactory factory) : IClassFixture<CinemadleWebApplicationFactory>
{
    [Theory]
    [InlineData("true", 200)]
    [InlineData("false", 404)]
    public async Task TestModeDestroyEndpointGivesExpectedStatusCode(string testMode, int expectedStatusCode)
    {
        
        var client = factory.CreateClientWithConfig(new Dictionary<string, string?>
        {
            ["CinemadleTestMode"] = testMode,
        });
        var response = await client.DeleteAsync("/api/test/destroy");
        
        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
    }
    
    [Theory]
    [InlineData("true", 200)]
    [InlineData("false", 404)]
    public async Task TestModeRigEndpointGivesExpectedResults(string testMode, int expectedStatusCode)
    {
        
        var client = factory.CreateClientWithConfig(new Dictionary<string, string?>
        {
            ["CinemadleTestMode"] = testMode,
        });
        var response = await client.GetAsync($"/api/test/rig/1924");
        
        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
    }
    
    [Theory]
    [InlineData("true", 200)]
    [InlineData("false", 404)]
    public async Task TestModeUnrigEndpointGivesExpectedResults(string testMode, int expectedStatusCode)
    {
        
        var client = factory.CreateClientWithConfig(new Dictionary<string, string?>
        {
            ["CinemadleTestMode"] = testMode,
        });
        var response = await client.GetAsync($"/api/test/rig/undo");
        
        Assert.Equal(expectedStatusCode, (int)response.StatusCode);
    }
}