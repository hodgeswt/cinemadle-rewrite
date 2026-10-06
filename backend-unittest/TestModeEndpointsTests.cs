namespace Cinemadle.UnitTest;

public class TestModeEndpointsDisabledTests(CinemadleWebApplicationFactoryTestModeDisabled factory) : IClassFixture<CinemadleWebApplicationFactoryTestModeDisabled>
{
    [Fact]
    public async Task TestModeDestroyEndpointGivesExpectedStatusCode()
    {
        
        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/test/destroy");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeRigEndpointGivesExpectedResults()
    {
        
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/test/rig/1924");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeUnrigEndpointGivesExpectedResults()
    {
        
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/test/rig/undo");
        
        Assert.Equal(404, (int)response.StatusCode);
    }
}

public class TestModeEndpointsEnabledTests(CinemadleWebApplicationFactory factory) : IClassFixture<CinemadleWebApplicationFactory>
{
    [Fact]
    public async Task TestModeDestroyEndpointGivesExpectedStatusCode()
    {
        
        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/test/destroy");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeRigEndpointGivesExpectedResults()
    {
        
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/test/rig/1924");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
    
    [Fact]
    public async Task TestModeUnrigEndpointGivesExpectedResults()
    {
        
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/test/rig/undo");
        
        Assert.Equal(200, (int)response.StatusCode);
    }
}