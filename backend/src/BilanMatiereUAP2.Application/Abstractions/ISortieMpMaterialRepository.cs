namespace BilanMatiereUAP2.Application.Abstractions;

public sealed record SortieMpMaterialKey(string NumeroOF, string CodeArticle);

public sealed record SortieMpMaterialLink(string NumeroOF, string CodeArticle, string CodeTypeMatiere);

public interface ISortieMpMaterialRepository
{
    Task<IReadOnlyList<SortieMpMaterialLink>> GetByPeriodeAsync(
        int annee,
        int mois,
        IReadOnlyCollection<SortieMpMaterialKey> cles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Liens « Sorties MP par ordre de fabrication » imputes a une date precise.
    ///
    /// Contrairement a <see cref="GetByPeriodeAsync"/>, qui complete des cles deja connues,
    /// cette methode ENUMERE les OF de la journee : c'est elle qui definit l'univers des
    /// lignes de l'alerte journaliere de non-reception.
    /// </summary>
    Task<IReadOnlyList<SortieMpMaterialLink>> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}
