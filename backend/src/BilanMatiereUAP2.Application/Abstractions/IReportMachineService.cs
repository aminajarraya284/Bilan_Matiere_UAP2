using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.Imports;
using BilanMatiereUAP2.Contracts.RapportsMachine;

namespace BilanMatiereUAP2.Application.Abstractions;

/// <summary>
/// Résultat d'un recalcul de classification CTRL/SCI/EMR/TRN à partir du code poste.
/// Les compteurs par origine portent sur l'ETAT FINAL de toutes les lignes analysees.
/// </summary>
public sealed record RecalculerOrigineProcessusResult(
    int LignesAnalysees,
    int LignesModifiees,
    int LignesInchangees,
    int LignesSci,
    int LignesEmr,
    int LignesTrn,
    int LignesInconnues,
    int LignesCtrl = 0);

public interface IReportMachineService
{
    Task<ImportRapportMachineResponse> ImportAsync(Stream fileStream, string profilImport, string sourceSystem, string nomFichierOriginal, CancellationToken cancellationToken = default);
    Task<PagedResult<RapportMachineResponse>> QueryAsync(string? numeroOF, string? codeArticle, string? codePoste, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RapportMachineConsultationResponse> QueryConsultationAsync(
        RapportMachineConsultationQuery query,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    Task<ImportTrackingResponse> GetImportTrackingAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    /// <summary>
    /// Totaux CTRL / rebut par origine process pour un OF + article.
    /// <paramref name="annee"/> et <paramref name="mois"/> restreignent le calcul a une periode ;
    /// omis, le cumul porte sur tout l'historique de l'OF (usage diagnostic uniquement).
    /// </summary>
    Task<RapportMachineAggregateResponse> AggregateAsync(string numeroOF, string codeArticle, int? annee = null, int? mois = null, CancellationToken cancellationToken = default);

    // Reevalue OrigineProcessus de toutes les lignes actives depuis les trois premiers
    // caracteres du poste YRJMCH. Transactionnel et idempotent.
    Task<RecalculerOrigineProcessusResult> RecalculerOrigineProcessusAsync(CancellationToken cancellationToken = default);
}
