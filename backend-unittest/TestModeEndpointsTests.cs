namespace Cinemadle.UnitTest;

public class TestModeEndpointsDisabledTests(CinemadleWebApplicationFactoryTestModeDisabled factory) : IClassFixture<CinemadleWebApplicationFactoryTestModeDisabled>
{

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TestModeDestroyEndpointGivesExpectedStatusCode()
    {
        var response = await _client.DeleteAsync("/api/test/destroy");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeRigEndpointGivesExpectedResults()
    {
        var response = await _client.GetAsync($"/api/test/rig/1924");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeUnrigEndpointGivesExpectedResults()
    {
        var response = await _client.GetAsync($"/api/test/rig/undo");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
}

public class TestModeEndpointsEnabledTests(CinemadleWebApplicationFactory factory) : IClassFixture<CinemadleWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TestModeDestroyEndpointGivesExpectedStatusCode()
    {
        var response = await _client.DeleteAsync("/api/test/destroy");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeRigEndpointGivesExpectedResults()
    {
        var response = await _client.GetAsync($"/api/test/rig/1924");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeUnrigEndpointGivesExpectedResults()
    {
        var response = await _client.GetAsync($"/api/test/rig/undo");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
}