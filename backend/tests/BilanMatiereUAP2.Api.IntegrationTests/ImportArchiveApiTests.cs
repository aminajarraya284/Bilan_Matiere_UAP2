using System.Net;
using System.Net.Http.Json;
using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ImportArchiveApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ImportArchiveApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ImportGammes_ArchiveLeFichierSource_TelechargeableEnsuite()
    {
        await _factory.ResetDatabaseAsync();
        var bytes = Encoding.UTF8.GetBytes("contenu-gammefab-test");

        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGammeFabricationReader>();
                services.AddScoped<IGammeFabricationReader>(_ => new TestGammeReader());
            });
        });

        Guid batchId;
        using (var scope = clientFactory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();
            await service.ImportGammesAsync(new MemoryStream(bytes), "GAMMEFAB_V1", "X3", "GAMMEFAB (34).xls", isCompleteExport: false);

            var repo = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesRepository>();
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            var batch = await repo.GetBatchByHashAsync(hash);
            batchId = batch!.Id;
        }

        var client = clientFactory.CreateClient();

        var files = await client.GetFromJsonAsync<List<ImportSourceFileResponse>>($"/api/v1/imports/{batchId}/files");
        Assert.Single(files!);
        Assert.Equal("GAMMEFAB (34).xls", files![0].NomFichierOriginal);
        Assert.Equal(bytes.Length, files[0].TailleOctets);

        var downloadResponse = await client.GetAsync($"/api/v1/imports/{batchId}/files/{files[0].Id}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, downloadedBytes);
        Assert.Equal("GAMMEFAB (34).xls", downloadResponse.Content.Headers.ContentDisposition?.FileNameStar ?? downloadResponse.Content.Headers.ContentDisposition?.FileName);
    }

    [Fact]
    public async Task Download_FichierInexistant_RetourneNotFound()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/imports/{Guid.NewGuid()}/files/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFiles_BatchSansFichier_RetourneListeVide()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var files = await client.GetFromJsonAsync<List<ImportSourceFileResponse>>($"/api/v1/imports/{Guid.NewGuid()}/files");

        Assert.Empty(files!);
    }

    private sealed class TestGammeReader : IGammeFabricationReader
    {
        public Task<IEnumerable<RefGammeFabrication>> ReadAsync(Stream excelStream, string sourceSystem, Guid importBatchId, CancellationToken cancellationToken = default)
        {
            var g = RefGammeFabrication.Creer("ART1", "ALT", "OP1", "P1", sourceSystem, importBatchId, DateTime.UtcNow);
            return Task.FromResult<IEnumerable<RefGammeFabrication>>(new[] { g });
        }
    }
}
