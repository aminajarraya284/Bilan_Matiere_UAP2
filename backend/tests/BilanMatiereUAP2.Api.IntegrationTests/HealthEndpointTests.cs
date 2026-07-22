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

    [Theory]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("http://127.0.0.1:3001")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:3001")]
    public async Task GetTypesMatiere_FromConfiguredFrontendOrigin_ReturnsCorsHeader(string origin)
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/types-matiere?actif=true");
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
