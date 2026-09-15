using System.Net.Http.Headers;
using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ManufacturingReferencesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ManufacturingReferencesApiTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostImportGammesEndpoint_Works()
    {
        await _factory.ResetDatabaseAsync();

        // Remplace le lecteur Excel par un stub pour éviter la dépendance
        // à un vrai fichier GAMMEFAB dans les tests d'intégration API.
        var clientFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGammeFabricationReader>();
                services.AddScoped<IGammeFabricationReader>(_ => new StubGammeReader());
            }));

        var client = clientFactory.CreateClient();
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

    private sealed class StubGammeReader : IGammeFabricationReader
    {
        public Task<IEnumerable<RefGammeFabrication>> ReadAsync(
            Stream excelStream,
            string sourceSystem,
            Guid importBatchId,
            CancellationToken cancellationToken = default)
        {
            var g = RefGammeFabrication.Creer("ART1", "ALT", "OP1", "P1", sourceSystem, importBatchId, DateTime.UtcNow);
            return Task.FromResult<IEnumerable<RefGammeFabrication>>(new[] { g });
        }
    }
}
