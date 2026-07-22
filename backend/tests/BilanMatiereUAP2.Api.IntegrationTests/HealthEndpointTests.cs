namespace BilanMatiereUAP2.Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public sealed class HealthEndpointTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task GetHealth_ReturnsApiAndDatabaseStatus()
    {
        await factory.ResetDatabaseAsync();
        var response = await factory.CreateClient().GetAsync("/health");
        var payload = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("sql-server", payload);
        Assert.Contains("Healthy", payload);
    }
}
