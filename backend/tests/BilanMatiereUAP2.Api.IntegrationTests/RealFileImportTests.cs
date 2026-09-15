using System.Security.Cryptography;
using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BilanMatiereUAP2.Api.IntegrationTests;

public sealed class RealFileImportTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RealFileImportTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ImportGammefab_RealFile_SuccessfullyPersistsToDatabase()
    {
        await _factory.ResetDatabaseAsync();

        var gammefabPath = "c:\\Users\\user\\Downloads\\GAMMEFAB (34).xls";
        if (!File.Exists(gammefabPath))
        {
            // Fichier industriel reel non embarque dans le depot (taille, confidentialite) : verification
            // best-effort sur les postes de developpement qui disposent de l'export local, pas bloquante en CI.
            return;
        }

        using var fileStream = File.OpenRead(gammefabPath);
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();

        var result = await service.ImportGammesAsync(fileStream, "GAMMEFAB_V1", "X3", "GAMMEFAB (34).xls", isCompleteExport: false);

        // Verify import succeeded without throwing
        Assert.False(result.Duplicate);
        Assert.NotNull(result);

        using var dbScope = _factory.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<BilanMatiereUAP2DbContext>();
        var operation10 = await db.RefGammesFabrication
            .AsNoTracking()
            .FirstOrDefaultAsync(gamme => gamme.NumeroOperation == "10");
        Assert.NotNull(operation10);
        Assert.False(string.IsNullOrWhiteSpace(operation10.CodeArticle));
        Assert.False(string.IsNullOrWhiteSpace(operation10.CodeAtelier));
    }

    [Fact]
    public async Task ImportGammefab_Idempotent_SecondImportReturnsZeroInserts()
    {
        await _factory.ResetDatabaseAsync();

        var gammefabPath = "c:\\Users\\user\\Downloads\\GAMMEFAB (34).xls";
        if (!File.Exists(gammefabPath))
        {
            return;
        }

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();

        // First import
        using (var fileStream1 = File.OpenRead(gammefabPath))
        {
            var result1 = await service.ImportGammesAsync(fileStream1, "GAMMEFAB_V1", "X3", "GAMMEFAB (34).xls", isCompleteExport: false);
            Assert.False(result1.Duplicate);
            Assert.True(result1.Inserted > 0);
        }

        // Second import of same file (idempotence)
        using (var fileStream2 = File.OpenRead(gammefabPath))
        {
            var result2 = await service.ImportGammesAsync(fileStream2, "GAMMEFAB_V1", "X3", "GAMMEFAB (34).xls", isCompleteExport: false);
            Assert.True(result2.Duplicate, "Second import should detect file as duplicate");
        }
    }

    [Fact]
    public async Task ImportNomenclatures_RealFile_SuccessfullyCompletes()
    {
        await _factory.ResetDatabaseAsync();

        var nomenclaturePath = "c:\\Users\\user\\Downloads\\BOMARTPARENT (31).xls";
        if (!File.Exists(nomenclaturePath))
        {
            return;
        }

        using var fileStream = File.OpenRead(nomenclaturePath);
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IManufacturingReferencesService>();

        var result = await service.ImportNomenclaturesAsync(fileStream, "BOMARTPARENT_V1", "X3", "BOMARTPARENT (31).xls", isCompleteExport: false);

        // Verify import completed and returned data
        Assert.NotNull(result);
        Assert.False(result.Duplicate);
    }

    [Fact]
    public async Task ImportYRJMCH_RealFile_SuccessfullyCompletes()
    {
        await _factory.ResetDatabaseAsync();

        var yrjmchPath = "c:\\Users\\user\\Downloads\\YRJMCH - 2026-07-06T154037.205.xls";
        if (!File.Exists(yrjmchPath))
        {
            return;
        }

        using var fileStream = File.OpenRead(yrjmchPath);
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IReportMachineService>();

        var result = await service.ImportAsync(fileStream, "YRJMCH_V1", "X3", "YRJMCH - 2026-07-06T154037.205.xls");

        // Verify import completed without throwing exception
        Assert.NotNull(result);
        Assert.False(result.Duplicate || result.Duplicate);
    }
}
