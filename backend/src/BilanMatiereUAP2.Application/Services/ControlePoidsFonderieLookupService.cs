using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Application.Services;

public sealed class ControlePoidsFonderieLookupService(
    IControlePoidsFonderieRepository repository,
    ITypeMatiereRepository typeMatiereRepository) : IControlePoidsFonderieLookupService
{
    public const string TODO_METIER = "TODO_METIER: choisir la règle SOPAL entre même OF, même lot ou série, dernier contrôle validé avant DateReception, contrôle validé article + matière, ou moyenne de contrôles.";

    public async Task<ControlePoidsFonderieLookupResult> ResolvePoidsPhysiqueAsync(
        string numeroOF,
        string codeArticle,
        string codeTypeMatiere,
        DateOnly dateReception,
        CancellationToken cancellationToken = default)
    {
        var typeMatiere = await typeMatiereRepository.GetByCodeAsync(
            codeTypeMatiere.Trim().ToUpperInvariant(),
            cancellationToken);
        if (typeMatiere is null)
        {
            return Empty("Introuvable", "Type matière introuvable pour la résolution du poids physique.");
        }

        var candidates = await repository.FindCandidatesAsync(
            codeArticle,
            typeMatiere.Id,
            numeroOF,
            dateReception,
            cancellationToken);
        var responseCandidates = candidates.Select(candidate => new ControlePoidsFonderieLookupCandidate(
            candidate.Id,
            candidate.DateControle,
            candidate.NumeroOF,
            candidate.CodeArticle,
            candidate.TypeMatiere?.Code ?? typeMatiere.Code,
            candidate.PoidsPhysiqueUnitaireKg,
            candidate.Statut.ToString())).ToArray();

        if (responseCandidates.Length == 0)
        {
            return Empty("PoidsPhysiqueManquant", "Aucun contrôle poids candidat.", responseCandidates);
        }

        var validCandidates = candidates
            .Where(candidate => candidate.Statut == StatutControlePoidsFonderie.Valide)
            .ToArray();
        if (validCandidates.Length == 0)
        {
            return Empty("ControleNonValide", "Aucun contrôle candidat n'est validé.", responseCandidates);
        }

        if (validCandidates.Any(candidate => candidate.PoidsPhysiqueUnitaireKg <= 0))
        {
            return Empty("ValeurInvalide", "Un contrôle validé contient un poids invalide.", responseCandidates);
        }

        if (validCandidates.Length > 1)
        {
            return Empty("Ambigu", TODO_METIER, responseCandidates);
        }

        var selected = validCandidates[0];
        return new ControlePoidsFonderieLookupResult(
            "Trouve",
            selected.PoidsPhysiqueUnitaireKg,
            selected.Id,
            "ControlePoidsFonderie",
            responseCandidates,
            null);
    }

    private static ControlePoidsFonderieLookupResult Empty(
        string statut,
        string message,
        IReadOnlyList<ControlePoidsFonderieLookupCandidate>? candidates = null)
    {
        return new ControlePoidsFonderieLookupResult(statut, null, null, null, candidates ?? [], message);
    }
}
