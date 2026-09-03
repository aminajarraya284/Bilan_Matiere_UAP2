using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.Audit;
using BilanMatiereUAP2.Application.Features.Imports;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class ReportMachineServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 24, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ImportAsync_ReimporteSansDoublon_EtCompteInsertionsModificationsInchanges()
    {
        var repository = new FakeReportMachineRepository([]);
        var reader = new FixedReader();
        var archive = new RecordingArchiveService();
        var refresh = new RecordingDependencyRefreshService();
        var audit = new RecordingAuditService();
        var service = new AuditedReportMachineService(
            new ReportMachineService(
                repository,
                reader,
                archive,
                refresh,
                new AnonymousCurrentUserService(),
                new TestImportHashCalculator()),
            audit);

        reader.Rows =
        [
            CreerLigne("OF1", "ART1", 10m),
            CreerLigne("OF2", "ART2", 20m)
        ];
        var first = await service.ImportAsync(new MemoryStream([1]), "YRJMCH_V1", "X3", "YRJMCH-v1.xls");

        reader.Rows =
        [
            CreerLigne("OF1", "ART1", 15m),
            CreerLigne("OF2", "ART2", 20m),
            CreerLigne("OF3", "ART3", 30m),
            CreerLigne("OF4", "ART4", 40m)
        ];
        var second = await service.ImportAsync(new MemoryStream([2]), "YRJMCH_V1", "X3", "YRJMCH-v2.xls");
        var duplicate = await service.ImportAsync(new MemoryStream([2]), "YRJMCH_V1", "X3", "YRJMCH-v2.xls");

        Assert.Equal(2, first.Inserted);
        Assert.Equal(2, second.Inserted);
        Assert.Equal(1, second.Updated);
        Assert.Equal(1, second.Unchanged);
        Assert.Equal(second.RowsValid, second.Inserted + second.Updated + second.Unchanged);
        Assert.Equal(second.RowsRead, second.RowsValid + second.Rejected);
        Assert.True(duplicate.Duplicate);
        Assert.Equal("Duplicate", duplicate.Statut);
        Assert.Equal(0, duplicate.Inserted);
        Assert.Equal(0, duplicate.Updated);
        Assert.Equal(0, duplicate.Unchanged);
        Assert.Equal(3, repository.AddedBatches.Count);
        Assert.Equal(4, repository.Rows.Count);
        Assert.Equal(3, archive.ArchivedBatchIds.Count);
        Assert.Equal(2, refresh.RapportMachineRefreshCount);
        Assert.Equal(
            [ActionAudit.ImportReussi, ActionAudit.ImportReussi, ActionAudit.ImportDuplicate],
            audit.Entries.Select(entry => entry.Action).ToArray());
        Assert.All(audit.Entries, entry => Assert.Equal(DomaineAudit.ImportX3, entry.Domaine));
    }

    [Fact]
    public async Task ImportAsync_ArchiveEnErreur_ConserveImportEtRetourneAvertissement()
    {
        var repository = new FakeReportMachineRepository([]);
        var reader = new FixedReader { Rows = [CreerLigne("OF1", "ART1", 10m)] };
        var service = new ReportMachineService(
            repository,
            reader,
            new FailingArchiveService(),
            new NoopDependencyRefreshService(),
            new AnonymousCurrentUserService(),
            new TestImportHashCalculator());

        var result = await service.ImportAsync(new MemoryStream([3]), "YRJMCH_V1", "X3", "YRJMCH.xls");

        Assert.Equal("TermineAvecAvertissementArchive", result.Statut);
        Assert.Contains("archive", result.ArchiveErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(repository.Rows);
    }

    [Fact]
    public async Task AggregateAsync_SepareLesTotauxParOrigine_EtNAdditionnePasSciEmrTrn()
    {
        var batchId = Guid.NewGuid();
        var rows = new[]
        {
            // EMR : deux jours / deux equipes -> agreges
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "EMR32", OrigineProcessus.Emr, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", batchId, Now),
            X3RapportMachine.Creer(new DateOnly(2026, 1, 6), "EMR33", OrigineProcessus.Emr, "5", "ART1", null, "OF1", "E2", 50m, 3m, "1", "X3", batchId, Now),
            // SCI
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "SCI03", OrigineProcessus.Sci, "30", "ART1", null, "OF1", "E1", 400m, 5m, "1", "X3", batchId, Now),
            // TRN
            X3RapportMachine.Creer(new DateOnly(2026, 1, 7), "TRN20", OrigineProcessus.Trn, "10", "ART1", null, "OF1", "E1", 200m, 1m, "1", "X3", batchId, Now),
            // Poste non mappe -> exclu des totaux, compte a part
            X3RapportMachine.Creer(new DateOnly(2026, 1, 8), "NOY08", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 999m, 9m, "1", "X3", batchId, Now)
        };

        var service = CreateService(new FakeReportMachineRepository(rows));
        var result = await service.AggregateAsync("OF1", "ART1", cancellationToken: CancellationToken.None);

        Assert.Equal(150m, result.TotalCtrlEmr);
        Assert.Equal(400m, result.TotalCtrlSci);
        Assert.Equal(200m, result.TotalCtrlTrn);
        Assert.Equal(5m, result.TotalRebutEmr);
        Assert.Equal(5m, result.TotalRebutSci);
        Assert.Equal(1m, result.TotalRebutTrn);
        Assert.Equal(5, result.NombreLignes);
        Assert.Equal(1, result.NombreLignesNonMappees);
        Assert.Equal(new DateOnly(2026, 1, 5), result.PremiereDate);
        Assert.Equal(new DateOnly(2026, 1, 8), result.DerniereDate);
        Assert.True(result.Trouve);
    }

    [Fact]
    public async Task AggregateAsync_RetourneTrouveFalse_QuandAucuneLigne()
    {
        var service = CreateService(new FakeReportMachineRepository([]));
        var result = await service.AggregateAsync("OF-VIDE", "ART1", cancellationToken: CancellationToken.None);

        Assert.False(result.Trouve);
        Assert.Equal(0m, result.TotalCtrlEmr);
        Assert.Equal(0, result.NombreLignes);
        Assert.Null(result.PremiereDate);
        Assert.Null(result.DerniereDate);
    }

    private sealed class FakeReportMachineRepository(IReadOnlyList<X3RapportMachine> rows) : IReportMachineRepository
    {
        private readonly List<X3RapportMachine> _rows = [.. rows];
        public IReadOnlyList<X3RapportMachine> Rows => _rows;
        public List<ImportBatch> AddedBatches { get; } = [];

        public Task<IReadOnlyList<X3RapportMachine>> GetByOfAndArticleAsync(string numeroOF, string codeArticle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows
                .Where(row => row.NumeroOF == numeroOF && row.CodeArticle == codeArticle)
                .ToArray());

        public Task<IReadOnlyList<X3RapportMachine>> GetByOfArticleAndPeriodeAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows
                .Where(row => row.NumeroOF == numeroOF
                    && row.CodeArticle == codeArticle
                    && row.DateProduction.Year == annee
                    && row.DateProduction.Month == mois)
                .ToArray());

        public Task<IReadOnlyList<X3RapportMachine>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows.Where(r => r.DateProduction == date).ToArray());

        public Task<IReadOnlyList<X3RapportMachine>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows
                .Where(row => row.DateProduction.Year == annee && row.DateProduction.Month == mois)
                .ToArray());

        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, string profilImport, CancellationToken cancellationToken = default)
            => Task.FromResult(AddedBatches.LastOrDefault(batch =>
                batch.HashFichierSha256 == hashSha256 &&
                batch.ProfilImport == profilImport &&
                batch.Statut is StatutImport.Termine or StatutImport.TermineAvecAvertissementArchive));
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        {
            AddedBatches.Add(batch);
            return Task.CompletedTask;
        }
        public Task AddAnomaliesAsync(IEnumerable<ImportReferenceAnomaly> anomalies, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<X3RapportMachine>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows.Where(row => cleMetiers.Contains(row.CleMetier)).ToArray());
        public Task AddRowsAsync(IEnumerable<X3RapportMachine> rows, CancellationToken cancellationToken = default)
        {
            _rows.AddRange(rows);
            return Task.CompletedTask;
        }
        public Task<PagedResult<X3RapportMachine>> QueryAsync(string? numeroOF = null, string? codeArticle = null, string? codePoste = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<X3RapportMachine>(Array.Empty<X3RapportMachine>(), 0));
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult<IDisposable>(new NoopDisposable());
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkBatchAsFailedAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<X3RapportMachine>> GetAllActiveTrackedAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<X3RapportMachine>)_rows.Where(row => row.IsActive).ToArray());
        public Task<IReadOnlyList<PosteNonMappeSummary>> GetPostesNonMappesAsync(CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<PosteNonMappeSummary>)Array.Empty<PosteNonMappeSummary>());

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    private static RapportMachineRow CreerLigne(string numeroOF, string article, decimal quantiteCtrl)
        => new(2, new DateOnly(2026, 7, 1), "SCI03", "5", article, $"Designation {article}", numeroOF, "A", quantiteCtrl, 1m, "1", "Feuil1");

    private sealed class FixedReader : IReportMachineReader
    {
        public IReadOnlyList<RapportMachineRow> Rows { get; set; } = [];

        public Task<X3ReadResult<RapportMachineRow>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken = default)
            => Task.FromResult(new X3ReadResult<RapportMachineRow>(Rows, [], Rows.Count));
    }

    private sealed class RecordingDependencyRefreshService : IImportDependencyRefreshService
    {
        public int RapportMachineRefreshCount { get; private set; }
        public Task RefreshAfterMvtEntreeOfAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RefreshAfterRapportMachineAsync(CancellationToken cancellationToken = default)
        {
            RapportMachineRefreshCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingArchiveService : IImportArchiveService
    {
        public List<Guid> ArchivedBatchIds { get; } = [];
        public Task ArchiveAsync(Guid importBatchId, TypeImport typeImport, string? profilImport, string nomFichierOriginal, byte[] content, Guid? uploadeParUtilisateurId, string? uploadeParNom, CancellationToken cancellationToken = default)
        {
            ArchivedBatchIds.Add(importBatchId);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FailingArchiveService : IImportArchiveService
    {
        public Task ArchiveAsync(Guid importBatchId, TypeImport typeImport, string? profilImport, string nomFichierOriginal, byte[] content, Guid? uploadeParUtilisateurId, string? uploadeParNom, CancellationToken cancellationToken = default)
            => throw new IOException("Archive indisponible.");
        public Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Fact]
    public async Task RecalculerOrigineProcessusAsync_ClasseUnPrefixeInconnuEnInconnue()
    {
        var batchId = Guid.NewGuid();
        var rows = new[]
        {
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "NOY08", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", batchId, Now)
        };
        var repository = new FakeReportMachineRepository(rows);
        var service = CreateService(repository);

        var result = await service.RecalculerOrigineProcessusAsync(CancellationToken.None);

        Assert.Equal(1, result.LignesAnalysees);
        Assert.Equal(0, result.LignesModifiees);
        Assert.Equal(1, result.LignesInchangees);
        Assert.Equal(1, result.LignesInconnues);
        Assert.Equal(OrigineProcessus.Inconnue, rows[0].OrigineProcessus);
    }

    [Fact]
    public async Task RecalculerOrigineProcessusAsync_EstIdempotent_DeuxiemeAppelNeModifieRien()
    {
        var batchId = Guid.NewGuid();
        var rows = new[]
        {
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "NOY08", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", batchId, Now)
        };
        var repository = new FakeReportMachineRepository(rows);
        var service = CreateService(repository);

        await service.RecalculerOrigineProcessusAsync(CancellationToken.None);
        var second = await service.RecalculerOrigineProcessusAsync(CancellationToken.None);

        Assert.Equal(0, second.LignesModifiees);
    }

    [Fact]
    public async Task RecalculerOrigineProcessusAsync_ConserveLePrefixeEmrClasseEmr()
    {
        var rows = new[]
        {
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "EMR32", OrigineProcessus.Emr, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", Guid.NewGuid(), Now)
        };
        var service = CreateService(new FakeReportMachineRepository(rows));

        var result = await service.RecalculerOrigineProcessusAsync(CancellationToken.None);

        Assert.Equal(0, result.LignesModifiees);
        Assert.Equal(1, result.LignesInchangees);
        Assert.Equal(1, result.LignesEmr);
        Assert.Equal(OrigineProcessus.Emr, rows[0].OrigineProcessus);
    }

    [Fact]
    public async Task RecalculerOrigineProcessusAsync_CompteCtrlCommeOrigineDistincte()
    {
        var rows = new[]
        {
            X3RapportMachine.Creer(new DateOnly(2026, 1, 5), "CTRL", OrigineProcessus.Inconnue, "5", "ART1", null, "OF1", "E1", 100m, 2m, "1", "X3", Guid.NewGuid(), Now)
        };
        var service = CreateService(new FakeReportMachineRepository(rows));

        var result = await service.RecalculerOrigineProcessusAsync(CancellationToken.None);

        Assert.Equal(1, result.LignesCtrl);
        Assert.Equal(OrigineProcessus.Ctrl, rows[0].OrigineProcessus);
    }

    private sealed class ThrowingReader : IReportMachineReader
    {
        public Task<X3ReadResult<RapportMachineRow>> ReadAsync(X3ImportFileContent file, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Le reader n'est pas sollicité par les tests d'agrégation.");
    }

    private static ReportMachineService CreateService(IReportMachineRepository repository)
        => new(
            repository,
            new ThrowingReader(),
            new ThrowingArchiveService(),
            new NoopDependencyRefreshService(),
            new AnonymousCurrentUserService(),
            new TestImportHashCalculator());

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

    private sealed class ThrowingArchiveService : IImportArchiveService
    {
        public Task ArchiveAsync(Guid importBatchId, TypeImport typeImport, string? profilImport, string nomFichierOriginal, byte[] content, Guid? uploadeParUtilisateurId, string? uploadeParNom, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("L'archivage n'est pas sollicité par ces tests.");
        public Task<IReadOnlyList<ImportSourceFileResponse>> GetFilesAsync(Guid importBatchId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("L'archivage n'est pas sollicité par ces tests.");
        public Task<StoredFileContent> DownloadAsync(Guid importBatchId, Guid fileId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("L'archivage n'est pas sollicité par ces tests.");
    }

    private sealed class TestImportHashCalculator : IImportHashCalculator
    {
        public string CalculateSha256(byte[] content) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        public string CalculateCoupleHash(string firstHash, string secondHash) => CalculateSha256(System.Text.Encoding.UTF8.GetBytes($"{firstHash}:{secondHash}"));
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task EnregistrerAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

}
