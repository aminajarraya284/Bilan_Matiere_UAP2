using BilanMatiereUAP2.Domain.Common;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.Domain.Entities;

// NumeroOF et DateReception sont immuables apres creation : ils determinent le contexte X3 pris
// en snapshot. Toute correction necessitant un OF/date different implique une nouvelle reception,
// pas une modification silencieuse qui rendrait le snapshot incoherent avec sa cle de resolution.
public sealed class ReceptionUsinage2
{
    private ReceptionUsinage2() { }

    private ReceptionUsinage2(
        Guid id,
        string numeroOF,
        DateOnly dateReception,
        decimal nombrePiecesReceptionnees,
        decimal? poidsTotalReceptionneKg,
        string? origine,
        string? observation,
        ContexteSnapshot snapshot,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc)
    {
        Valider(nombrePiecesReceptionnees, poidsTotalReceptionneKg, observation);

        Id = id;
        NumeroOF = NormaliserCode(numeroOF, 50);
        DateReception = dateReception;
        NombrePiecesReceptionnees = nombrePiecesReceptionnees;
        PoidsTotalReceptionneKg = poidsTotalReceptionneKg;
        Origine = NormaliserTexte(origine, 100);
        Observation = NormaliserTexte(observation, 500);
        AppliquerSnapshot(snapshot);
        StatutReception = StatutReceptionUsinage2.Brouillon;
        CreeParUtilisateurId = creeParUtilisateurId;
        CreeParNom = NormaliserTexte(creeParNom, 150);
        DateCreationUtc = dateCreationUtc;
    }

    public Guid Id { get; private set; }
    public string NumeroOF { get; private set; } = string.Empty;
    public DateOnly DateReception { get; private set; }
    public decimal NombrePiecesReceptionnees { get; private set; }
    public decimal? PoidsTotalReceptionneKg { get; private set; }
    public string? Origine { get; private set; }
    public string? Observation { get; private set; }

    public string? CodeArticle { get; private set; }
    public string? DesignationArticle { get; private set; }
    public string? SituationX3 { get; private set; }
    public string? AtelierOrigine { get; private set; }
    public string? CodeTypeMatiere { get; private set; }
    public decimal? QuantiteLienUtilisee { get; private set; }
    public string? UniteQuantiteLien { get; private set; }
    public decimal? PoidsTheoriqueUnitaireKg { get; private set; }
    public decimal? PoidsPhysiqueUnitaireKg { get; private set; }
    public Guid? ControlePoidsFonderieId { get; private set; }
    public decimal? TotalCtrlSci { get; private set; }
    public decimal? TotalCtrlEmr { get; private set; }
    public decimal? TotalCtrlTrn { get; private set; }
    public decimal? TotalRebutSci { get; private set; }
    public decimal? TotalRebutEmr { get; private set; }
    public decimal? TotalRebutTrn { get; private set; }
    public string StatutArticle { get; private set; } = string.Empty;
    public string StatutAtelier { get; private set; } = string.Empty;
    public string StatutTypeMatiere { get; private set; } = string.Empty;
    public string StatutNomenclature { get; private set; } = string.Empty;
    public string StatutPoidsPhysique { get; private set; } = string.Empty;
    public string StatutRapportMachine { get; private set; } = string.Empty;
    public string SourceTypeMatiere { get; private set; } = "NonResolue";
    public bool EligibleAtelier16 { get; private set; }
    public string? AvertissementsJson { get; private set; }

    public StatutReceptionUsinage2 StatutReception { get; private set; }
    public string? DerogationJustification { get; private set; }
    public Guid? CreeParUtilisateurId { get; private set; }
    public string? CreeParNom { get; private set; }
    public DateTime DateCreationUtc { get; private set; }
    public Guid? ModifieParUtilisateurId { get; private set; }
    public string? ModifieParNom { get; private set; }
    public DateTime? DateModificationUtc { get; private set; }

    public sealed record ContexteSnapshot(
        string? CodeArticle,
        string? DesignationArticle,
        string? SituationX3,
        string? AtelierOrigine,
        string? CodeTypeMatiere,
        decimal? QuantiteLienUtilisee,
        string? UniteQuantiteLien,
        decimal? PoidsTheoriqueUnitaireKg,
        decimal? PoidsPhysiqueUnitaireKg,
        Guid? ControlePoidsFonderieId,
        decimal? TotalCtrlSci,
        decimal? TotalCtrlEmr,
        decimal? TotalCtrlTrn,
        decimal? TotalRebutSci,
        decimal? TotalRebutEmr,
        decimal? TotalRebutTrn,
        string StatutArticle,
        string StatutAtelier,
        string StatutTypeMatiere,
        string StatutNomenclature,
        string StatutPoidsPhysique,
        string StatutRapportMachine,
        IReadOnlyList<string> Avertissements,
        string SourceTypeMatiere = "NonResolue",
        bool EligibleAtelier16 = false);

    public static ReceptionUsinage2 Creer(
        string numeroOF,
        DateOnly dateReception,
        decimal nombrePiecesReceptionnees,
        decimal? poidsTotalReceptionneKg,
        string? origine,
        string? observation,
        ContexteSnapshot snapshot,
        Guid? creeParUtilisateurId,
        string? creeParNom,
        DateTime dateCreationUtc,
        Guid? id = null)
    {
        return new ReceptionUsinage2(
            id ?? Guid.NewGuid(),
            numeroOF,
            dateReception,
            nombrePiecesReceptionnees,
            poidsTotalReceptionneKg,
            origine,
            observation,
            snapshot,
            creeParUtilisateurId,
            creeParNom,
            dateCreationUtc);
    }

    public void Modifier(
        decimal nombrePiecesReceptionnees,
        decimal? poidsTotalReceptionneKg,
        string? origine,
        string? observation,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime dateModificationUtc)
    {
        GarantirBrouillon("modifiée");
        Valider(nombrePiecesReceptionnees, poidsTotalReceptionneKg, observation);
        NombrePiecesReceptionnees = nombrePiecesReceptionnees;
        PoidsTotalReceptionneKg = poidsTotalReceptionneKg;
        Origine = NormaliserTexte(origine, 100);
        Observation = NormaliserTexte(observation, 500);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom, 150);
        DateModificationUtc = dateModificationUtc;
    }

    // Action explicite uniquement (jamais automatique) : re-synchronise le snapshot avec l'etat
    // actuel des references X3, sans toucher aux champs saisis par l'utilisateur.
    public void RecalculerContexte(ContexteSnapshot snapshot, Guid? modifieParUtilisateurId, string? modifieParNom, DateTime dateModificationUtc)
    {
        GarantirBrouillon("recalculée");
        AppliquerSnapshot(snapshot);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom, 150);
        DateModificationUtc = dateModificationUtc;
    }

    // Rafraichissement automatique declenche par une nouvelle reference validee.
    // Seul le snapshot calcule est remplace : les champs saisis restent inchanges.
    public void RafraichirContexteAutomatique(
        ContexteSnapshot snapshot,
        Guid? modifieParUtilisateurId,
        string? modifieParNom,
        DateTime dateModificationUtc)
    {
        if (StatutReception == StatutReceptionUsinage2.Annule)
        {
            return;
        }

        AppliquerSnapshot(snapshot);
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom, 150);
        DateModificationUtc = dateModificationUtc;
    }

    public void Valider(bool derogationAdmin, string? derogationJustification, Guid? modifieParUtilisateurId, string? modifieParNom, DateTime dateModificationUtc)
    {
        GarantirBrouillon("validée");

        StatutReception = StatutReceptionUsinage2.Valide;
        DerogationJustification = derogationAdmin ? NormaliserTexte(derogationJustification, 500) : null;
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom, 150);
        DateModificationUtc = dateModificationUtc;
    }

    public void Annuler(Guid? modifieParUtilisateurId, string? modifieParNom, DateTime dateModificationUtc)
    {
        if (StatutReception == StatutReceptionUsinage2.Annule)
        {
            return;
        }

        StatutReception = StatutReceptionUsinage2.Annule;
        ModifieParUtilisateurId = modifieParUtilisateurId;
        ModifieParNom = NormaliserTexte(modifieParNom, 150);
        DateModificationUtc = dateModificationUtc;
    }

    private void GarantirBrouillon(string action)
    {
        if (StatutReception != StatutReceptionUsinage2.Brouillon)
        {
            throw new DomainValidationException($"Seule une réception Brouillon peut être {action}.");
        }
    }

    private void AppliquerSnapshot(ContexteSnapshot snapshot)
    {
        CodeArticle = NormaliserTexte(snapshot.CodeArticle, 100);
        DesignationArticle = NormaliserTexte(snapshot.DesignationArticle, 250);
        SituationX3 = NormaliserTexte(snapshot.SituationX3, 100);
        AtelierOrigine = NormaliserTexte(snapshot.AtelierOrigine, 50);
        CodeTypeMatiere = NormaliserTexte(snapshot.CodeTypeMatiere, 50);
        QuantiteLienUtilisee = snapshot.QuantiteLienUtilisee;
        UniteQuantiteLien = NormaliserTexte(snapshot.UniteQuantiteLien, 20);
        PoidsTheoriqueUnitaireKg = snapshot.PoidsTheoriqueUnitaireKg;
        PoidsPhysiqueUnitaireKg = snapshot.PoidsPhysiqueUnitaireKg;
        ControlePoidsFonderieId = snapshot.ControlePoidsFonderieId;
        TotalCtrlSci = snapshot.TotalCtrlSci;
        TotalCtrlEmr = snapshot.TotalCtrlEmr;
        TotalCtrlTrn = snapshot.TotalCtrlTrn;
        TotalRebutSci = snapshot.TotalRebutSci;
        TotalRebutEmr = snapshot.TotalRebutEmr;
        TotalRebutTrn = snapshot.TotalRebutTrn;
        StatutArticle = snapshot.StatutArticle;
        StatutAtelier = snapshot.StatutAtelier;
        StatutTypeMatiere = snapshot.StatutTypeMatiere;
        StatutNomenclature = snapshot.StatutNomenclature;
        StatutPoidsPhysique = snapshot.StatutPoidsPhysique;
        StatutRapportMachine = snapshot.StatutRapportMachine;
        SourceTypeMatiere = snapshot.SourceTypeMatiere;
        EligibleAtelier16 = snapshot.EligibleAtelier16;
        AvertissementsJson = System.Text.Json.JsonSerializer.Serialize(snapshot.Avertissements);
    }

    private static void Valider(decimal nombrePiecesReceptionnees, decimal? poidsTotalReceptionneKg, string? observation)
    {
        if (nombrePiecesReceptionnees <= 0)
        {
            throw new DomainValidationException("Le nombre de pièces réceptionnées doit être strictement positif.");
        }

        if (poidsTotalReceptionneKg is null or <= 0)
        {
            throw new DomainValidationException("Le poids total réceptionné doit être strictement positif.");
        }

        if (observation?.Trim().Length > 500)
        {
            throw new DomainValidationException("L'observation ne peut pas dépasser 500 caractères.");
        }
    }

    private static string NormaliserCode(string value, int maxLength)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized)) throw new DomainValidationException("Le numéro d'OF est obligatoire.");
        if (normalized.Length > maxLength) throw new DomainValidationException($"La valeur ne peut pas dépasser {maxLength} caractères.");
        return normalized;
    }

    private static string? NormaliserTexte(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
