using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record X3ImportFileContent(string FileName, long Length, byte[] Content);

public sealed record X3ReadError(
    int NumeroLigneSource,
    StatutRapprochementX3 Statut,
    string Message,
    string? NumeroOF = null,
    string? CodeTypeMatiere = null,
    string? NomFeuilleSource = null);

public sealed record X3ReadResult<T>(
    IReadOnlyList<T> Rows,
    IReadOnlyList<X3ReadError> Errors,
    int TotalRows);

public sealed record EncoursProductRow(
    int NumeroLigneSource,
    string NumeroOF,
    string? Article,
    string? DesignationArticle,
    string? SituationOF,
    string? Atelier,
    string? NumeroOperation,
    string? CodePoste,
    string? TypePoste,
    string? NumeroPoste,
    string? EffectifPoste,
    string? Cad,
    string? EffectifOperation,
    decimal? QuantitePrevue,
    decimal? QuantiteRestante,
    DateOnly? DateOperation,
    DateOnly? DateObjectifOF,
    string? Statut,
    string? NomFeuilleSource = null);

public sealed record MouvementStockX3Row(
    int NumeroLigneSource,
    DateOnly DateImputation,
    string? Code,
    string? Designation,
    DateOnly? DateCreation,
    string? HeureCreation,
    string? Utilisateur,
    decimal QuantiteX3Signee,
    string? Unite,
    string? Transaction,
    string? TypePiece,
    string? Lot,
    string? PieceOrigine,
    string NumeroOFNormalise,
    string? DesignationMouvement,
    string? Emplacement,
    string? Statut,
    string? NomFeuilleSource = null);

public interface IEncoursProductReader
{
    Task<X3ReadResult<EncoursProductRow>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken);
}

public interface IMouvementStockX3Reader
{
    Task<X3ReadResult<MouvementStockX3Row>> ReadAsync(
        X3ImportFileContent file,
        CancellationToken cancellationToken);
}

public sealed record ConsommationsComposantsFilter(
    DateOnly? DateDebutIncluse,
    DateOnly? DateFinExclusive,
    string? NumeroOF,
    string? Article,
    string? CodeComposant,
    StatutRapprochementX3? Statut,
    int Page,
    int PageSize,
    string SortBy,
    bool Descending);

public interface IX3ImportRepository
{
    Task<ImportX3Batch?> GetBatchByPairHashAsync(string pairHash, CancellationToken cancellationToken);
    Task<ImportX3Batch?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ImportX3Anomaly>> GetAnomaliesAsync(Guid batchId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, X3OrdreFabrication>> GetOrdresByNumeroAsync(
        IReadOnlyCollection<string> numerosOF,
        CancellationToken cancellationToken);
    Task<X3OrdreFabrication?> GetOrdreContexteAsync(string numeroOF, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, Composant>> GetComposantsByCodeAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken);
    Task<PagedResult<ConsommationComposant>> GetConsommationsAsync(
        ConsommationsComposantsFilter filter,
        CancellationToken cancellationToken);
    Task AddBatchAsync(ImportX3Batch batch, CancellationToken cancellationToken);
    Task AddOrdresAsync(IEnumerable<X3OrdreFabrication> ordres, CancellationToken cancellationToken);
    Task AddComposantsAsync(IEnumerable<Composant> composants, CancellationToken cancellationToken);
}
