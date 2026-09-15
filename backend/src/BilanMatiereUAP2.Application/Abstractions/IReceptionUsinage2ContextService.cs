using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Domain.Entities;

namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record ControlePoidsFonderieLookupResult(
    string Statut,
    decimal? PoidsPhysiqueUnitaireKg,
    Guid? ControlePoidsFonderieId,
    string? Source,
    IReadOnlyList<ControlePoidsFonderieLookupCandidate> Candidats,
    string? Message);

public sealed record ControlePoidsFonderieLookupCandidate(
    Guid Id,
    DateOnly DateControle,
    string? NumeroOF,
    string CodeArticle,
    string CodeTypeMatiere,
    decimal PoidsPhysiqueUnitaireKg,
    string Statut);

public interface IControlePoidsFonderieLookupService
{
    Task<ControlePoidsFonderieLookupResult> ResolvePoidsPhysiqueAsync(
        string numeroOF,
        string codeArticle,
        string codeTypeMatiere,
        DateOnly dateReception,
        CancellationToken cancellationToken = default);
}

public interface IReceptionUsinage2ContextService
{
    Task<ReceptionU2ContextResponse?> GetAsync(string numeroOF, DateOnly dateReception, CancellationToken cancellationToken = default);
}
