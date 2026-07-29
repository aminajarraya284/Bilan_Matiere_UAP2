using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record RapportMachineRow(
    int NumeroLigneSource,
    DateOnly DateProduction,
    string CodePoste,
    string NumeroOperation,
    string CodeArticle,
    string? DesignationArticle,
    string NumeroOF,
    string? Equipe,
    decimal QuantiteCTRL,
    decimal QuantiteRebut,
    string? AlternativeGamme,
    string NomFeuilleSource);

public interface IReportMachineReader
{
    Task<X3ReadResult<RapportMachineRow>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken = default);
}

public sealed record PosteNonMappeSummary(
    string CodePoste,
    int NombreLignes,
    decimal QuantiteCtrlTotale,
    decimal QuantiteRebutTotale,
    DateOnly PremiereDate,
    DateOnly DerniereDate,
    IReadOnlyList<string> ExemplesArticles,
    IReadOnlyList<string> ExemplesOF);

public interface IReportMachineRepository
{
    Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, string profilImport, CancellationToken cancellationToken = default);
    Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default);
    Task AddAnomaliesAsync(IEnumerable<ImportReferenceAnomaly> anomalies, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<X3RapportMachine>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default);
    Task AddRowsAsync(IEnumerable<X3RapportMachine> rows, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<X3RapportMachine>> GetByOfAndArticleAsync(string numeroOF, string codeArticle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lignes de rapport machine d'un OF + article restreintes a une periode.
    /// Le Bilan Usinage 2 raisonne par periode : cumuler tous les mois d'un OF face a des
    /// receptions filtrees sur un seul mois comparerait deux grandeurs heterogenes.
    /// </summary>
    Task<IReadOnlyList<X3RapportMachine>> GetByOfArticleAndPeriodeAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default);

    /// <summary>Toutes les lignes actives d'une periode, base de calcul du Bilan Usinage 2.</summary>
    Task<IReadOnlyList<X3RapportMachine>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default);

    /// <summary>Lignes de rapport machine d'une date precise (alerte journaliere de non-reception).</summary>
    Task<IReadOnlyList<X3RapportMachine>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<PagedResult<X3RapportMachine>> QueryAsync(string? numeroOF = null, string? codeArticle = null, string? codePoste = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default);
    Task MarkBatchAsFailedAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default);

    // Toutes les lignes actives, suivies par EF (tracked), pour le recalcul d'OrigineProcessus.
    Task<IReadOnlyList<X3RapportMachine>> GetAllActiveTrackedAsync(CancellationToken cancellationToken = default);

    // Diagnostic : postes distincts dont l'origine est Inconnue, avec agregats pour aide a l'administration.
    Task<IReadOnlyList<PosteNonMappeSummary>> GetPostesNonMappesAsync(CancellationToken cancellationToken = default);
}
