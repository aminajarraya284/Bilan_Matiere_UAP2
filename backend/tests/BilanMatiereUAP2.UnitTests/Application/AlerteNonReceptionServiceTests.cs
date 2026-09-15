using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Features.ReceptionsUsinage2;
using BilanMatiereUAP2.Contracts;
using BilanMatiereUAP2.Contracts.ReceptionsUsinage2;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;

namespace BilanMatiereUAP2.UnitTests.Application;

/// <summary>
/// Alerte JOURNALIERE des pieces non receptionnees : une ligne par Numero OF,
/// univers issu des Sorties MP de la date selectionnee, quantites en PIECES.
/// </summary>
public sealed class AlerteNonReceptionServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Jour = new(2026, 7, 15);
    private static readonly DateOnly Veille = new(2026, 7, 14);

    // L'univers des OF vient des Sorties MP : une Sortie MP d'un autre jour est exclue.
    [Fact]
    public async Task SortieMp_HorsDate_EstExclue()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour), Lien("OF2", "ART2", "M3010L0", Veille)],
            rapports: [LigneEmr("OF1", 100m, 5m, Jour)],
            receptions: []);

        var alerte = await service.GetAsync(Jour, 1, 50, CancellationToken.None);

        var ligne = Assert.Single(alerte.Lignes);
        Assert.Equal("OF1", ligne.NumeroOF);
    }

    // Le YRJMCH d'un autre jour n'alimente pas l'alerte du jour demande.
    [Fact]
    public async Task Yrjmch_AutreDate_EstExclu()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 100m, 5m, Veille)],
            receptions: []);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Null(ligne.CtrlEmrPieces);
        Assert.Equal(StatutAlerteNonReception.DonneeManquante, ligne.Statut);
        Assert.Contains(AnomalieAlerteNonReception.RapportMachineEmrIntrouvable, ligne.Anomalies);
    }

    // Une reception du lendemain ne reduit pas la non-reception du jour precedent.
    [Fact]
    public async Task Reception_AutreDate_NeReduitPasLAlerteDuJour()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 1000m, 20m, Jour)],
            receptions: [ReceptionValidee(900m, Jour.AddDays(1))]);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal(0m, ligne.QuantiteReceptionneeU2Pieces);
        Assert.Equal(980m, ligne.QuantiteNonReceptionneePieces);
    }

    // Cas de recette : 1000 - 20 = 980 pieces, 900 recues -> 80 non receptionnees.
    [Fact]
    public async Task CasDeRecette_CtrlMoinsRebutMoinsReception()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 1000m, 20m, Jour)],
            receptions: [ReceptionValidee(900m, Jour)]);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal(1000m, ligne.CtrlEmrPieces);
        Assert.Equal(20m, ligne.RebutEmrPieces);
        Assert.Equal(980m, ligne.NombreDePieces);
        Assert.Equal(900m, ligne.QuantiteReceptionneeU2Pieces);
        Assert.Equal(80m, ligne.QuantiteNonReceptionneePieces);
        Assert.Equal(StatutAlerteNonReception.AVerifier, ligne.Statut);
    }

    // Brouillons et annulees sont exclus de la quantite recue.
    [Fact]
    public async Task Receptions_BrouillonEtAnnulee_SontExclues()
    {
        var annulee = ReceptionValidee(50m, Jour);
        annulee.Annuler(Guid.NewGuid(), "A", Now);

        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 100m, 0m, Jour)],
            receptions: [ReceptionValidee(30m, Jour), annulee, Reception(25m, Jour)]);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal(30m, ligne.QuantiteReceptionneeU2Pieces);
        Assert.Equal(70m, ligne.QuantiteNonReceptionneePieces);
    }

    // Le signe est conserve : ni Math.Abs, ni Math.Max, ni clamp a zero.
    [Theory]
    [InlineData(80, 20, StatutAlerteNonReception.AVerifier)]
    [InlineData(100, 0, StatutAlerteNonReception.Ok)]
    [InlineData(120, -20, StatutAlerteNonReception.AnomalieSurReception)]
    public async Task NonReception_ConserveLeSigneEtPiloteLeStatut(
        decimal receptionnee,
        decimal attendue,
        string statutAttendu)
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 100m, 0m, Jour)],
            receptions: [ReceptionValidee(receptionnee, Jour)]);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal(attendue, ligne.QuantiteNonReceptionneePieces);
        Assert.Equal(statutAttendu, ligne.Statut);
    }

    // YRJMCH absent : null partout, jamais zero.
    [Fact]
    public async Task YrjmchAbsent_RetourneNullEtNonZero()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports: [],
            receptions: [ReceptionValidee(50m, Jour)]);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Null(ligne.CtrlEmrPieces);
        Assert.Null(ligne.RebutEmrPieces);
        Assert.Null(ligne.NombreDePieces);
        Assert.Null(ligne.QuantiteNonReceptionneePieces);
        Assert.Equal(StatutAlerteNonReception.DonneeManquante, ligne.Statut);
    }

    // Seules les lignes YRJMCH de prefixe EMR entrent dans le calcul.
    [Fact]
    public async Task SeulesLesLignesEmr_SontRetenues()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour)],
            rapports:
            [
                Ligne("OF1", "EMR39", 100m, 5m, Jour),
                Ligne("OF1", "SCI03", 500m, 50m, Jour),
                Ligne("OF1", "TRN07", 300m, 30m, Jour)
            ],
            receptions: []);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal(100m, ligne.CtrlEmrPieces);
        Assert.Equal(5m, ligne.RebutEmrPieces);
        Assert.Equal(95m, ligne.NombreDePieces);
    }

    // Plusieurs articles pour un meme OF : une seule ligne + anomalie.
    [Fact]
    public async Task PlusieursArticlesPourUnOf_NeCreePasPlusieursLignes()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour), Lien("OF1", "ART2", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 100m, 0m, Jour)],
            receptions: []);

        var ligne = Assert.Single((await service.GetAsync(Jour, 1, 50, CancellationToken.None)).Lignes);

        Assert.Equal("Plusieurs articles", ligne.CodeArticle);
        Assert.Contains(AnomalieAlerteNonReception.PlusieursArticlesPourOf, ligne.Anomalies);
        // Le statut reste pilote par la QUANTITE non receptionnee (100 - 0 = 100 > 0).
        // L'ambiguite d'article se lit dans les anomalies, elle n'ecrase pas le statut.
        Assert.Equal(StatutAlerteNonReception.AVerifier, ligne.Statut);
    }

    // Une ligne par OF, meme avec plusieurs lignes YRJMCH et plusieurs receptions.
    [Fact]
    public async Task UneLigneParOf_AvecAgregationDesSources()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour), Lien("OF2", "ART2", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 60m, 5m, Jour), LigneEmr("OF1", 40m, 5m, Jour), LigneEmr("OF2", 10m, 0m, Jour)],
            receptions: [ReceptionValidee(30m, Jour, "OF1"), ReceptionValidee(20m, Jour, "OF1")]);

        var alerte = await service.GetAsync(Jour, 1, 50, CancellationToken.None);

        Assert.Equal(2, alerte.Lignes.Count);
        var of1 = alerte.Lignes.Single(l => l.NumeroOF == "OF1");
        Assert.Equal(100m, of1.CtrlEmrPieces);
        Assert.Equal(10m, of1.RebutEmrPieces);
        Assert.Equal(50m, of1.QuantiteReceptionneeU2Pieces);
        Assert.Equal(40m, of1.QuantiteNonReceptionneePieces);
    }

    [Fact]
    public async Task Reponse_ExposeLesCompteursEtLaPagination()
    {
        var service = CreateService(
            sortiesMp: [Lien("OF1", "ART1", "M3010L0", Jour), Lien("OF2", "ART2", "M3010L0", Jour)],
            rapports: [LigneEmr("OF1", 100m, 0m, Jour)],
            receptions: []);

        var alerte = await service.GetAsync(Jour, 1, 1, CancellationToken.None);

        Assert.Equal(Jour, alerte.Date);
        Assert.Equal(2, alerte.Total);
        Assert.Single(alerte.Lignes);
        Assert.Equal(1, alerte.NombreAVerifier);        // OF1 : 100 non receptionnees
        Assert.Equal(1, alerte.NombreDonneesManquantes); // OF2 : pas de YRJMCH
    }

    // ------------------------------------------------------------------
    private static AlerteNonReceptionService CreateService(
        IReadOnlyList<(SortieMpMaterialLink Lien, DateOnly Date)> sortiesMp,
        IReadOnlyList<X3RapportMachine> rapports,
        IReadOnlyList<ReceptionUsinage2> receptions,
        IReadOnlyList<EntreeOfComposant>? entreesOf = null)
        => new(
            new FakeSortieMp(sortiesMp),
            new FakeRapports(rapports),
            new FakeReceptions(receptions),
            new FakeEntreesOf(entreesOf ?? []));

    private static (SortieMpMaterialLink, DateOnly) Lien(string of, string article, string matiere, DateOnly date)
        => (new SortieMpMaterialLink(of, article, matiere), date);

    private static X3RapportMachine LigneEmr(string of, decimal ctrl, decimal rebut, DateOnly date)
        => Ligne(of, "EMR39", ctrl, rebut, date);

    private static X3RapportMachine Ligne(string of, string poste, decimal ctrl, decimal rebut, DateOnly date)
        => X3RapportMachine.Creer(date, poste, OrigineProcessus.Inconnue, "10", "ART1", null, of, null, ctrl, rebut, null, "X3", Guid.NewGuid(), Now);

    private static ReceptionUsinage2 ReceptionValidee(decimal pieces, DateOnly date, string of = "OF1")
    {
        var reception = Reception(pieces, date, of);
        reception.Valider(false, null, Guid.NewGuid(), "V", Now);
        return reception;
    }

    private static ReceptionUsinage2 Reception(decimal pieces, DateOnly date, string of = "OF1")
        => ReceptionUsinage2.Creer(of, date, pieces, pieces * 2.6m, null, null, Snapshot(), Guid.NewGuid(), "T", Now);

    private static ReceptionUsinage2.ContexteSnapshot Snapshot()
        => new("ART1", "Article test", "En cours", "16", "M3010L0", 2.5m, "KG", 2.5m, 2.6m, Guid.NewGuid(),
            10m, 100m, 5m, 0m, 5m, 0m, "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", "Trouve", []);

    private sealed class FakeSortieMp(IReadOnlyList<(SortieMpMaterialLink Lien, DateOnly Date)> liens) : ISortieMpMaterialRepository
    {
        public Task<IReadOnlyList<SortieMpMaterialLink>> GetAllByPeriodeAsync(
            int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)[]);

        public Task<IReadOnlyList<SortieMpMaterialLink>> GetByPeriodeAsync(
            int annee, int mois, IReadOnlyCollection<SortieMpMaterialKey> cles, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)[]);

        public Task<IReadOnlyList<SortieMpMaterialLink>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<SortieMpMaterialLink>)liens
                .Where(l => l.Date == date)
                .Select(l => l.Lien)
                .ToArray());
    }

    private sealed class FakeRapports(IReadOnlyList<X3RapportMachine> rows) : IReportMachineRepository
    {
        public Task<IReadOnlyList<X3RapportMachine>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)rows.Where(r => r.DateProduction == date).ToArray());

        public Task<IReadOnlyList<X3RapportMachine>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<IReadOnlyList<X3RapportMachine>> GetByOfAndArticleAsync(string numeroOF, string codeArticle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<IReadOnlyList<X3RapportMachine>> GetByOfArticleAndPeriodeAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, string profilImport, CancellationToken cancellationToken = default)
            => Task.FromResult<ImportBatch?>(null);
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAnomaliesAsync(IEnumerable<ImportReferenceAnomaly> anomalies, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRowsAsync(IEnumerable<X3RapportMachine> rows, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<X3RapportMachine>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<PagedResult<X3RapportMachine>> QueryAsync(string? numeroOF = null, string? codeArticle = null, string? codePoste = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<X3RapportMachine>([], 0));
        public Task<IReadOnlyList<X3RapportMachine>> GetAllActiveTrackedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<X3RapportMachine>)[]);
        public Task<IReadOnlyList<PosteNonMappeSummary>> GetPostesNonMappesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<PosteNonMappeSummary>)[]);
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkBatchAsFailedAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeReceptions(IReadOnlyList<ReceptionUsinage2> receptions) : IReceptionUsinage2Repository
    {
        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)receptions
                .Where(r => r.StatutReception == StatutReceptionUsinage2.Valide && r.DateReception == date)
                .ToArray());

        public Task<ReceptionUsinage2?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<ReceptionUsinage2?>(null);
        public Task<PagedResult<ReceptionUsinage2>> GetPagedAsync(ReceptionsUsinage2Filter filter, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<ReceptionUsinage2>([], 0));
        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByOfAndArticleAsync(string numeroOF, string codeArticle, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)[]);
        public Task<IReadOnlyList<ReceptionUsinage2>> GetValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)[]);
        public Task<IReadOnlyList<ReceptionUsinage2>> GetToutesValidesByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)[]);
        public Task<IReadOnlyList<ReceptionUsinage2>> GetConcernedByControlePoidsAsync(
            string codeArticle, string codeTypeMatiere, string? numeroOF, DateOnly dateControle, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ReceptionUsinage2>)[]);
        public Task AddAsync(ReceptionUsinage2 reception, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Quatrieme source de l'univers journalier : les mouvements Entree OF du jour.
    /// Un OF present uniquement ici doit tout de meme produire une ligne d'alerte.
    /// </summary>
    private sealed class FakeEntreesOf(IReadOnlyList<EntreeOfComposant> entrees) : IEntreeOfRepository
    {
        public Task<IReadOnlyList<EntreeOfComposant>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)entrees
                .Where(entree => entree.DateImputation == date)
                .ToArray());

        public Task<IReadOnlyList<EntreeOfComposant>> GetByPeriodeAsync(int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)entrees
                .Where(entree => entree.DateImputation.Year == annee && entree.DateImputation.Month == mois)
                .ToArray());

        public Task<IReadOnlyList<EntreeOfComposant>> GetByOfAndPeriodeAsync(string numeroOF, int annee, int mois, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<EntreeOfComposant>)[]);
        public Task<ImportBatch?> GetCompletedBatchByHashAsync(string hashSha256, CancellationToken cancellationToken = default)
            => Task.FromResult<ImportBatch?>(null);
        public Task AddBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateBatchAsync(ImportBatch batch, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkBatchAsEchecAsync(Guid batchId, DateTime dateFinUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, EntreeOfComposant>> GetByCleMetiersAsync(IReadOnlyCollection<string> cleMetiers, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<string, EntreeOfComposant>)new Dictionary<string, EntreeOfComposant>());
        public Task AddRangeAsync(IEnumerable<EntreeOfComposant> items, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RollbackTransactionAsync(IDisposable transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
