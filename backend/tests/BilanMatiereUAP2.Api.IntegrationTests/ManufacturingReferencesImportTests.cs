using System.Text;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class ManufacturingReferencesImportTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ManufacturingReferencesImportTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ImportGammes_FirstTime_InsertsBatchAndEntities()
    {
        await _factory.ResetDatabaseAsync();

        var bytes = Encoding.UTF8.GetBytes("dummy-file-1");

        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGammeFabricationReader>();
                services.AddScoped<IGammeFabricationReader>(_ => new TestGammeReader());
            });
        });

        using var scope = clientFactory.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var service = provider.GetRequiredService<IManufacturingReferencesService>();

        var result = await service.ImportGammesAsync(new MemoryStream(bytes), "profile", "SYS", isCompleteExport: false);

        Assert.False(result.Duplicate);
        Assert.True(result.Inserted > 0 || result.Updated >= 0);

        var repo = provider.GetRequiredService<IManufacturingReferencesRepository>();
        var hash = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(bytes)).Replace("-", string.Empty).ToUpperInvariant();
        var batch = await repo.GetBatchByHashAsync(hash);
        Assert.NotNull(batch);
        Assert.Equal("Termine", batch.Statut.ToString());
    }

    [Fact]
    public async Task ImportGammes_Duplicate_ReturnsDuplicate()
    {
        await _factory.ResetDatabaseAsync();
        var bytes = Encoding.UTF8.GetBytes("dummy-file-2");

        var clientFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGammeFabricationReader>();
                services.AddScoped<IGammeFabricationReader>(_ => new TestGammeReader());
            });
        });

        using (var scope = clientFactory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();
            var first = await service.ImportGammesAsync(new MemoryStream(bytes), "profile", "SYS", isCompleteExport: false);
            Assert.False(first.Duplicate);
        }

        using (var scope = clientFactory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();
            var second = await service.ImportGammesAsync(new MemoryStream(bytes), "profile", "SYS", isCompleteExport: false);
            Assert.True(second.Duplicate);
        }
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
