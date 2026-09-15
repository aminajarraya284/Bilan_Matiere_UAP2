using System.Globalization;
using BilanMatiereUAP2.Api.Authorization;
using BilanMatiereUAP2.Application.Features.Bilans;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BilanMatiereUAP2.Api.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
[Authorize(Policy = AuthorizationPolicies.ReadReferences)]
public sealed class DiagnosticsController(BilanMatiereUAP2DbContext dbContext) : ControllerBase
{
    // Diagnostic en lecture seule (aucune ecriture) de la disponibilite des sources necessaires
    // aux bilans Fonderie et UAP2, pour une periode YYYY-MM. N'active jamais une formule non
    // confirmee : le Bilan UAP2 reste bloque tant que TODO_METIER_UAP2_CTRL_REFERENCE n'est pas leve.
    [HttpGet("preparation-bilans/{periode}")]
    public async Task<IActionResult> GetPreparationBilans(string periode, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(periode, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return BadRequest(new { message = "La période doit respecter le format YYYY-MM." });
        }

        var debut = new DateOnly(parsed.Year, parsed.Month, 1);
        var fin = debut.AddMonths(1);

        var sortiesMagasinX3 = await dbContext.ConsommationsComposants.AsNoTracking()
            .Where(c => c.DateImputation >= debut && c.DateImputation < fin)
            .ToListAsync(cancellationToken);
        var dechetsFonderie = await dbContext.DechetsFonderie.AsNoTracking()
            .Where(d => !d.EstSupprime && d.DateDeclaration >= debut && d.DateDeclaration < fin)
            .ToListAsync(cancellationToken);
        var receptionsU2 = await dbContext.ReceptionsUsinage2.AsNoTracking()
            .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide && r.DateReception >= debut && r.DateReception < fin)
            .ToListAsync(cancellationToken);
        var projectionsInventaire = await dbContext.InventairesFonderie.AsNoTracking()
            .Where(i => i.Periode == debut && i.Statut != StatutInventaireFonderie.Annule)
            .ToListAsync(cancellationToken);
        var stocksDebutConnus = projectionsInventaire.Count(i => i.StockDebutKg != null);

        var sourcesFonderie = new[]
        {
            new
            {
                nom = "StockDebut",
                disponible = stocksDebutConnus > 0,
                nombreLignes = (int?)stocksDebutConnus,
                unites = new[] { "KG" },
                ambiguites = stocksDebutConnus == projectionsInventaire.Count
                    ? Array.Empty<string>()
                    : new[] { "Certains types matière n'ont pas de stock de début : la période précédente n'est pas clôturée (RM-INV-005)." }
            },
            new
            {
                nom = "SortiesMagasinX3",
                disponible = sortiesMagasinX3.Count > 0,
                nombreLignes = (int?)sortiesMagasinX3.Count,
                unites = sortiesMagasinX3.Select(c => c.Unite).Distinct().ToArray(),
                // Seules les lignes en KG entrent dans le bilan : aucune regle de conversion
                // n'est confirmee pour les autres unites.
                ambiguites = sortiesMagasinX3.Any(c => !string.Equals(c.Unite, "KG", StringComparison.OrdinalIgnoreCase))
                    ? new[] { "Des consommations sont exprimées dans une unité autre que KG : elles sont exclues du bilan et signalées en anomalie." }
                    : Array.Empty<string>()
            },
            new
            {
                nom = "StockFin",
                disponible = projectionsInventaire.Count > 0,
                nombreLignes = (int?)projectionsInventaire.Count,
                unites = new[] { "KG" },
                ambiguites = Array.Empty<string>()
            },
            new
            {
                nom = "DechetsFonderie",
                disponible = dechetsFonderie.Count > 0,
                nombreLignes = (int?)dechetsFonderie.Count,
                unites = new[] { "KG" },
                ambiguites = Array.Empty<string>()
            },
            new
            {
                nom = "ReceptionsU2",
                disponible = receptionsU2.Count > 0,
                nombreLignes = (int?)receptionsU2.Count,
                unites = new[] { "pieces" },
                ambiguites = Array.Empty<string>()
            }
        };
        var formuleFonderieActivable = sourcesFonderie.All(source => source.disponible);

        var bilanFonderie = new
        {
            nomBilan = "Fonderie",
            sources = sourcesFonderie,
            reglesConfirmees = new[]
            {
                "INPUT = StockDebut + SortiesMagasinX3",
                "OUTPUT = StockFin + DechetsFonderie + ReceptionsU2",
                "ECART = INPUT - OUTPUT",
                "Inventaire Four exclu du calcul."
            },
            reglesTodoMetier = Array.Empty<string>(),
            formuleActivable = formuleFonderieActivable
        };

        var bilanUap2 = new
        {
            nomBilan = "UAP2",
            sources = Array.Empty<object>(),
            reglesConfirmees = Array.Empty<string>(),
            reglesTodoMetier = new[]
            {
                "TODO_METIER_UAP2_CTRL_REFERENCE : la règle de référence CTRL (SCI/EMR/TRN ?) pour le Bilan UAP2 n'est pas encore confirmée par SOPAL."
            },
            formuleActivable = false
        };

        return Ok(new { periode, bilanFonderie, bilanUap2 });
    }
    [HttpGet("imports-reels")]
    public async Task<IActionResult> GetImportsReels(CancellationToken cancellationToken)
    {
        var gammesActives = await dbContext.RefGammesFabrication.CountAsync(entity => entity.IsActive, cancellationToken);
        var nomenclaturesActives = await dbContext.RefNomenclatures.CountAsync(entity => entity.IsActive, cancellationToken);
        var rapportsMachine = await dbContext.X3RapportsMachine.CountAsync(entity => entity.IsActive, cancellationToken);
        var derniersImportBatches = await dbContext.ImportBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.DateCreationUtc)
            .Take(10)
            .Select(batch => new
            {
                batch.Id,
                batch.HashFichierSha256,
                TypeImport = batch.TypeImport.ToString(),
                batch.ProfilImport,
                Statut = batch.Statut.ToString(),
                batch.NombreLignesLues,
                batch.NombreLignesValides,
                batch.NombreLignesImportees,
                batch.NombreAnomalies,
                batch.ReadCount,
                batch.ValidCount,
                batch.InsertedCount,
                batch.UpdatedCount,
                batch.UnchangedCount,
                batch.RejectedCount,
                batch.ArchiveErrorMessage,
                batch.CreeParUtilisateurId,
                batch.CreeParNom,
                batch.DateCreationUtc,
                batch.DateFinUtc
            })
            .ToListAsync(cancellationToken);
        var anomaliesRecentes = await dbContext.ImportReferenceAnomalies
            .AsNoTracking()
            .OrderByDescending(anomaly => anomaly.DateCreationUtc)
            .Take(20)
            .Select(anomaly => new
            {
                anomaly.Id,
                anomaly.ImportBatchId,
                TypeImport = anomaly.TypeImport.ToString(),
                anomaly.NumeroLigneSource,
                anomaly.NomFeuilleSource,
                anomaly.Message,
                anomaly.DateCreationUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            gammesActives,
            nomenclaturesActives,
            rapportsMachine,
            derniersImportBatches,
            anomaliesRecentes
        });
    }
}
