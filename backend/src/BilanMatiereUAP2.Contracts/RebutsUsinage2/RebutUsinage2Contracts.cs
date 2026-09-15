using System.ComponentModel.DataAnnotations;

namespace BilanMatiereUAP2.Contracts.RebutsUsinage2;

/// <summary>
/// Rebut declare a l'atelier Usinage 2. Toutes les quantites sont en PIECES.
/// </summary>
public sealed record RebutUsinage2Response(
    Guid Id,
    DateOnly DateDeclaration,
    string Periode,
    string NumeroOF,
    string CodeArticle,
    decimal QuantiteRebutPieces,
    string? Motif,
    string? Observation,
    string Statut,
    string? CreePar,
    DateTime CreeLeUtc,
    string? ValidePar,
    DateTime? ValideLeUtc);

public record CreateRebutUsinage2Request
{
    [Required(ErrorMessage = "La date de déclaration est obligatoire.")]
    public DateOnly DateDeclaration { get; init; }

    [Required(ErrorMessage = "Le numéro d'OF est obligatoire.")]
    public string NumeroOF { get; init; } = string.Empty;

    [Required(ErrorMessage = "Le code article est obligatoire.")]
    public string CodeArticle { get; init; } = string.Empty;

    /// <summary>Quantite rebutee en PIECES (jamais en kg).</summary>
    [Range(0.0001, double.MaxValue, ErrorMessage = "La quantité rebutée doit être strictement positive.")]
    public decimal QuantiteRebutPieces { get; init; }

    public string? Motif { get; init; }
    public string? Observation { get; init; }
}

public sealed record UpdateRebutUsinage2Request : CreateRebutUsinage2Request;

public sealed record PagedRebutsUsinage2Response(IReadOnlyList<RebutUsinage2Response> Items, int Total);
