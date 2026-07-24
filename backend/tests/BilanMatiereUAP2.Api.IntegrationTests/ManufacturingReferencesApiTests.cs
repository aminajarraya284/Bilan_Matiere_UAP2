using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ManufacturingReferencesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ManufacturingReferencesApiTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostImportGammesEndpoint_Works()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Development-Role", "Admin");

        var bytes = Encoding.UTF8.GetBytes("filecontent");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "test.xlsx");

        var response = await client.PostAsync("/api/v1/references/gammes/import?profilImport=pf&sourceSystem=SYS", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("inserted", json, StringComparison.OrdinalIgnoreCase);
    }
}
