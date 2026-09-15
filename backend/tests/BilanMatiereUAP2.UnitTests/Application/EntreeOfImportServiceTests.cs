using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.EntreeOf;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class EntreeOfImportServiceTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public async Task ImportAsync_ReimporteSansDoublon_EtCompteInsertionsModificationsInchanges()
    {
        var repository = new FakeEntreeOfRepository([]);
        var reader = new FixedMouvementReader();
        var archive = new RecordingArchiveService();
        var refresh = new RecordingDependencyRefreshService();
        var service = new EntreeOfImportService(
            reader,
            repository,
            archive,
            refresh,
            new AnonymousCurrentUserService(),
            new TestImportHashCalculator(),
            new TestParametreApplicationProvider());

        reader.Rows = [CreerLigneImport("OF1", "ART1", 10m), CreerLigneImport("OF2", "ART2", 20m)];
        var first = await service.ImportAsync(new MemoryStream([1]), "X3", "MVT-v1.xls");

        reader.Rows =
        [
            CreerLigneImport("OF1", "ART1", 15m),
            CreerLigneImport("OF2", "ART2", 20m),
            CreerLigneImport("OF3", "ART3", 30m),
            CreerLigneImport("OF4", "ART4", 40m)
        ];
        var second = await service.ImportAsync(new MemoryStream([2]), "X3", "MVT-v2.xls");
        var duplicate = await service.ImportAsync(new MemoryStream([2]), "X3", "MVT-v2.xls");

        Assert.Equal(2, first.Inserted);
        Assert.Equal(2, second.Inserted);
        Assert.Equal(1, second.Updated);
        Assert.Equal(1, second.Unchanged);
        Assert.Equal(second.RowsValid, second.Inserted + second.Updated + second.Unchanged);
        Assert.Equal(second.RowsRead, second.RowsValid + second.Rejected);
        Assert.True(duplicate.Duplicate);
        Assert.Equal("Duplicate", duplicate.Statut);
        Assert.Equal(3, repository.AddedBatches.Count);
        Assert.Equal(4, repository.Rows.Count);
        Assert.Equal(3, archive.ArchivedBatchIds.Count);
        Assert.Equal(2, refresh.MvtRefreshCount);
    }

    [Fact]
    public async Task ResolveAsync_AucunMouvement_RetourneMouvementAbsent()
    {
        var service = CreateService(new FakeEntreeOfRepository([]));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("MouvementAbsent", result.Statut);
        Assert.Null(result.QuantiteNetteEntreeOfPieces);
    }

    [Fact]
    public async Task ResolveAsync_PlusieursArticlesDistincts_RetourneAmbigu()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", 10m),
            CreerMouvement("OF123", "ART2", "UN", 5m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, null, CancellationToken.None);

        Assert.Equal("Ambigu", result.Statut);
    }

    [Fact]
    public async Task ResolveAsync_ArticleDifferentDeLAttendu_RetourneMouvementAbsent()
    {
        var rows = new[] { CreerMouvement("OF123", "ART1", "UN", 10m) };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART-AUTRE", CancellationToken.None);

        Assert.Equal("MouvementAbsent", result.Statut);
    }

    [Fact]
    public async Task ResolveAsync_UniteDifferenteDeUN_RetourneUniteIncompatible()
    {
        var rows = new[] { CreerMouvement("OF123", "ART1", "KG", 10m) };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("UniteIncompatible", result.Statut);
    }

    [Fact]
    public async Task ResolveAsync_Moins980Plus980_RetourneZeroTrouve()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", -980m),
            CreerMouvement("OF123", "ART1", "UN", 980m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal(0m, result.QuantiteNetteEntreeOfPieces);
    }

    [Fact]
    public async Task ResolveAsync_FiltreParOfEtArticleAvantDeTesterLAmbiguite()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", 100m),
            CreerMouvement("OF123", "ART2", "UN", 200m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal("ART1", result.CodeArticle);
        Assert.Equal(100m, result.QuantiteNetteEntreeOfPieces);
    }

    // Les quantites X3 sont SIGNEES : un net negatif est une donnee legitime, pas une erreur.
    // Aucun Math.Abs, aucun clamp a zero, aucun rejet.
    [Fact]
    public async Task ResolveAsync_QuantiteNetteNegative_ConserveLeSigne()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", 10m),
            CreerMouvement("OF123", "ART1", "UN", -25m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal(-15m, result.QuantiteNetteEntreeOfPieces);
    }

    // Exigence explicite du cahier des charges : -980 + 980 = 0, sans valeur absolue.
    [Fact]
    public async Task ResolveAsync_MouvementsOpposes_SAnnulentExactement()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", -980m),
            CreerMouvement("OF123", "ART1", "UN", 980m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal(0m, result.QuantiteNetteEntreeOfPieces);
    }

    [Fact]
    public async Task ResolveAsync_PlusieursLignesPositivesMemeArticleMemeUnite_SommeCorrectement()
    {
        var rows = new[]
        {
            CreerMouvement("OF123", "ART1", "UN", 50m),
            CreerMouvement("OF123", "ART1", "UN", 30m),
            CreerMouvement("OF123", "ART1", "UN", -5m)
        };
        var service = CreateService(new FakeEntreeOfRepository(rows));

        var result = await service.ResolveAsync("OF123", 2026, 7, "ART1", CancellationToken.None);

        Assert.Equal("Trouve", result.Statut);
        Assert.Equal(75m, result.QuantiteNetteEntreeOfPieces);
        Assert.Equal("ART1", result.CodeArticle);
    }

    private static EntreeOfComposant CreerMouvement(string numeroOF, string codeArticle, string unite, decimal quantiteSignee)
        => EntreeOfComposant.Creer(Guid.NewGuid(), new DateOnly(2026, 7, 15), numeroOF, codeArticle, "Designation test", unite, quantiteSignee, Now);

    private static MouvementStockX3Row CreerLigneImport(string numeroOF, string codeArticle, decimal quantiteSignee)
        => new(
            11,
            new DateOnly(2026, 7, 15),
            codeArticle,
            $"Designation {codeArticle}",
            null,
            null,
            null,
            quantiteSignee,
            "UN",
            "Entree OF",
            null,
            null,
            numeroOF,
            numeroOF,
            null,
            null,
            null,
            "Feuil1");

    private static EntreeOfImportService CreateService(FakeEntreeOfRepository repository)
        => new(
            new ThrowingMouvementReader(),
            repository,
            new ThrowingArchiveService(),
            new NoopDependencyRefreshService(),
            new AnonymousCurrentUserService(),
            new TestImportHashCalculator(),
            new TestParametreApplicationProvider());

    private sealed class NoopDependencyRefreshService : IImportDependencyRefreshService
    {
        public Task RefreshAfterMvtEntreeOfAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RefreshAfterRapportMachineAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? UserName => null;
        public string? Role => null;
        public bool IsAuthenticated => false;
    }

    private sealed class FakeEntreeOfRepository(IReadOnlyList<EntreeOfComposant> rows) : IEntreeOfRepository
    {
        private readonly List<EntreeOfComposant> _rows = [.. rows];
        public IReadOnlyList<EntreeOfComposant> Rows => _rows;
        public List<ImportBatch> AddedBatches { get; } = [];

        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
            => Task.FromResult(AddedBatches.LastOrDefault(batch =>
                batch.HashFichierSha256 == hashSha256 &&
                batch.Statut is BilanMatiereUAP2.Domain.Enums.StatutImport.Termine or BilanMatiereUAP2.Domain.Enums.StatutImport.TermineAvecAvertissementArchive));
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        {
            AddedBatches.Add(batch);
            return Task.CompletedTask;
        }
        public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<string, EntreeOfComposant>)_rows
                .Where(row => cleMetiers.Contains(row.CleMetier))
                .ToDictionary(row => row.CleMetier, StringComparer.OrdinalIgnoreCase));
        public Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default)
        {
            _rows.AddRange(items);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(string numeroOF, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)_rows.Where(r => r.NumeroOF == numeroOF.ToUpperInvariant()).ToArray());
        public Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)_rows
                .Where(r => r.DateImputation.Year == annee && r.DateImputation.Month == mois)
                .ToArray());
        public Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)_rows
                .Where(r => r.DateImputation == date)
                .ToArray());
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult<IDisposable>(new NoopDisposable());
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private sealed class FixedMouvementReader : IMouvementStockX3Reader
    {
        public IReadOnlyList<MouvementStockX3Row> Rows { get; set; } = [];
        public Task<X3ReadResult<MouvementStockX3Row>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken = default)
            => Task.FromResult(new X3ReadResult<MouvementStockX3Row>(Rows, [], Rows.Count));
    }

    private sealed class RecordingDependencyRefreshService : IImportDependencyRefreshService
    {
        public int MvtRefreshCount { get; private set; }
        public Task RefreshAfterMvtEntreeOfAsync(CancellationToken cancellationToken = default)
        {
            MvtRefreshCount++;
            return Task.CompletedTask;
        }
        public Task RefreshAfterRapportMachineAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingArchiveService : IImportArchiveService
    {
        public List<Guid> ArchivedBatchIds { get; } = [];
        public Task ArchiveAsync(Guid importBatchId, BilanMatiereUAP2.Domain.Enums.TypeImport typeImport, string? profilImport, string nomFichierOriginal, byte[] content, Guid? uploadeParUtilisateurId, string? uploadeParNom, CancellationToken cancellationToken = default)
        {
            ArchivedBatchIds.Add(importBatchId);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ThrowingMouvementReader : IMouvementStockX3Reader
    {
        public Task<X3ReadResult<MouvementStockX3Row>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Le reader n'est pas sollicité par les tests de résolution.");
    }

    private sealed class ThrowingArchiveService : IImportArchiveService
    {
        public Task ArchiveAsync(Guid importBatchId, BilanMatiereUAP2.Domain.Enums.TypeImport typeImport, string? profilImport, string nomFichierOriginal, byte[] content, Guid? uploadeParUtilisateurId, string? uploadeParNom, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TestImportHashCalculator : IImportHashCalculator
    {
        public string CalculateSha256(byte[] content) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        public string CalculateCoupleHash(string firstHash, string secondHash) => CalculateSha256(System.Text.Encoding.UTF8.GetBytes($"{firstHash}:{secondHash}"));
    }

    private sealed class TestParametreApplicationProvider : IParametreApplicationProvider
    {
        public Task<string?> GetValueAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("Entree OF");

        public Task<decimal?> GetDecimalAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<decimal?>(null);

        public Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
