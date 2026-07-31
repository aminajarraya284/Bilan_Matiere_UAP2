using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Application.Services;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Enums;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.UnitTests.Application;

public sealed class OfContextResolverTests
{
    private static readonly DateOnly Date = new(2026, 7, 29);
    private static readonly Guid Batch = Guid.NewGuid();

    [Fact]
    public async Task OfPresentUniquementDansYrjmch_EstReconnuEtArticleEstResolu()
    {
        var data = new OfContextData(
            null,
            [Rapport("OF-YR", "ART-1")],
            [],
            []);
        var references = new FakeReferences();

        var result = await Resolve(data, references);

        Assert.True(result.ExisteDansYrjmch);
        Assert.False(result.ExisteDansEntreeOf);
        Assert.False(result.ExisteDansSortieMp);
        Assert.Equal("ART-1", result.CodeArticle);
        Assert.DoesNotContain("OF_X3_INTROUVABLE", result.Anomalies);
        Assert.Contains("ATELIER_UAP2_INTROUVABLE", result.Anomalies);
        Assert.Contains("TYPE_MATIERE_INTROUVABLE", result.Anomalies);
    }

    [Fact]
    public async Task OfAbsentDeToutesLesSources_ResteResolubleAvecAnomalieVisible()
    {
        var result = await Resolve(new OfContextData(null, [], [], []), new FakeReferences());

        // Code d'anomalie contractuel pour un OF absent des quatre sources X3.
        Assert.Contains("OF_X3_INTROUVABLE", result.Anomalies);
        Assert.Contains("ARTICLE_OF_INTROUVABLE", result.Anomalies);
        Assert.False(result.EligibleCalculUap2);
        Assert.Null(result.CodeArticle);
    }

    [Fact]
    public async Task SansSortieMp_UnSeulComposantNomenclatureDonneLeTypeMatiere()
    {
        var data = new OfContextData(null, [Rapport("OF-NOM", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddGamme("ART-1", "16", "10", "UAP2");
        references.AddNomenclature("ART-1", "M3000L0");

        var result = await Resolve(data, references);

        Assert.Equal("M3000L0", result.TypeMatiere);
        Assert.Equal(SourceTypeMatiere.Nomenclature, result.SourceTypeMatiere);
        Assert.True(result.EligibleCalculUap2);
        Assert.DoesNotContain("TYPE_MATIERE_INTROUVABLE", result.Anomalies);
    }

    [Fact]
    public async Task NomenclaturesAutoriseesMultiples_SontSignaleesAmbigues()
    {
        var data = new OfContextData(null, [Rapport("OF-NOM-AMB", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddNomenclature("ART-1", "M3000L0");
        references.AddNomenclature("ART-1", "M3010L0");

        var result = await Resolve(data, references);

        Assert.Null(result.TypeMatiere);
        Assert.Equal(SourceTypeMatiere.Inconnue, result.SourceTypeMatiere);
        Assert.Contains("TYPE_MATIERE_AMBIGUE", result.Anomalies);
    }

    [Fact]
    public async Task SortiesMpMultiples_NeReplientPasSurLaNomenclature()
    {
        var data = new OfContextData(
            null,
            [],
            [],
            [new SortieMpMaterialLink("OF-SMP-AMB", "ART-1", "M3000L0"), new SortieMpMaterialLink("OF-SMP-AMB", "ART-1", "M3010L0")]);
        var references = new FakeReferences();
        references.AddNomenclature("ART-1", "M3060L0");

        var result = await Resolve(data, references);

        Assert.Null(result.TypeMatiere);
        Assert.Equal(SourceTypeMatiere.Inconnue, result.SourceTypeMatiere);
        Assert.Contains("TYPE_MATIERE_SORTIE_MP_AMBIGUE", result.Anomalies);
        Assert.Empty(references.NomenclatureLookups);
    }

    [Fact]
    public async Task GammeAtelier16SurOperation10_EstEligibleSansFiltreOperation5()
    {
        var data = new OfContextData(null, [Rapport("OF-GAMME", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddGamme("ART-1", "16", "10", "UAP2");

        var result = await Resolve(data, references);

        Assert.True(result.EligibleCalculUap2);
        Assert.Equal("16", result.CodeAtelier);
        Assert.Equal("UAP2", result.Groupe);
        Assert.DoesNotContain("HORS_ATELIER_UAP2", result.Anomalies);
    }

    // Regle d'eligibilite : AU MOINS UNE ligne GAMMEFAB avec CodeAtelier = 16.
    // Plusieurs groupes de postes de charge sur cet atelier ne retirent pas l'eligibilite ;
    // ils rendent seulement le GROUPE non resolu, ce qui reste une anomalie visible.
    [Fact]
    public async Task PlusieursGroupesSurAtelier16_RestentEligiblesAvecAnomalieVisible()
    {
        var data = new OfContextData(null, [Rapport("OF-GAMME-MULTI", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddGamme("ART-1", "16", "10", "M3010");
        references.AddGamme("ART-1", "16", "20", "M3060");

        var result = await Resolve(data, references);

        Assert.True(result.EligibleCalculUap2);
        Assert.Equal("16", result.CodeAtelier);
        Assert.Null(result.Groupe);
        Assert.Contains("ATELIER_UAP2_AMBIGU", result.Anomalies);
        Assert.DoesNotContain("HORS_ATELIER_UAP2", result.Anomalies);
    }

    [Fact]
    public async Task GammeSansAtelier16_EstVisibleCommeHorsUap2()
    {
        var data = new OfContextData(null, [Rapport("OF-GAMME-NOK", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddGamme("ART-1", "15", "10", "AUTRE");

        var result = await Resolve(data, references);

        Assert.False(result.EligibleCalculUap2);
        Assert.Null(result.CodeAtelier);
        Assert.Contains("HORS_ATELIER_UAP2", result.Anomalies);
    }

    [Fact]
    public async Task ArticlesDistincts_SontSignalesSansChoixArbitraire()
    {
        var data = new OfContextData(
            null,
            [Rapport("OF-ART-AMB", "ART-1"), Rapport("OF-ART-AMB", "ART-2")],
            [],
            []);

        var result = await Resolve(data, new FakeReferences());

        Assert.Null(result.CodeArticle);
        Assert.Contains("PLUSIEURS_ARTICLES_POUR_OF", result.Anomalies);
    }

    [Fact]
    public async Task ParametreAtelierEtReferentielActifPilotentLeContexteSansListeCsv()
    {
        var data = new OfContextData(null, [Rapport("OF-PARAM", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddGamme("ART-1", "99", "10", "PARAM");
        references.AddNomenclature("ART-1", "M9999");
        var parameters = new FakeParametres(new Dictionary<string, string>
        {
            [ParametresApplicationCles.CodeAtelierUap2] = "99"
        });
        var typesMatiere = new FakeTypeMatiereRepository("M9999");

        var result = await Resolve(data, references, parameters, typesMatiere);

        Assert.True(result.EligibleCalculUap2);
        Assert.Equal("99", result.CodeAtelier);
        Assert.Equal("M9999", result.TypeMatiere);
        Assert.Equal(SourceTypeMatiere.Nomenclature, result.SourceTypeMatiere);
    }

    [Fact]
    public async Task NomenclatureIgnoreUnTypeMatiereDesactive()
    {
        var data = new OfContextData(null, [Rapport("OF-INACTIF", "ART-1")], [], []);
        var references = new FakeReferences();
        references.AddNomenclature("ART-1", "M9999");
        var typesMatiere = new FakeTypeMatiereRepository();
        typesMatiere.Add("M9999", actif: false);

        var result = await Resolve(data, references, typesMatiere: typesMatiere);

        Assert.Null(result.TypeMatiere);
        Assert.Equal(SourceTypeMatiere.Inconnue, result.SourceTypeMatiere);
        Assert.Empty(references.NomenclatureLookups);
        Assert.Contains("REFERENTIEL_TYPES_MATIERE_ACTIFS_VIDE", result.Anomalies);
    }

    private static Task<OfContextResult> Resolve(
        OfContextData data,
        FakeReferences references,
        IParametreApplicationProvider? parametres = null,
        ITypeMatiereRepository? typesMatiere = null)
        => new OfContextResolver(
            new FakeData(data),
            references,
            parametres ?? FakeParametres.Default,
            typesMatiere ?? FakeTypeMatiereRepository.Default).ResolveAsync(data.RapportsMachine.FirstOrDefault()?.NumeroOF
            ?? data.SortiesMp.FirstOrDefault()?.NumeroOF
            ?? "OF-TEST", Date);

    private static X3RapportMachine Rapport(string of, string article)
        => X3RapportMachine.Creer(
            Date,
            "POSTE-1",
            OrigineProcessus.Emr,
            "10",
            article,
            "Article",
            of,
            null,
            10m,
            0m,
            null,
            "TEST",
            Batch,
            Date.ToDateTime(TimeOnly.MinValue));

    private sealed class FakeData : IOfContextDataRepository
    {
        private readonly OfContextData value;

        public FakeData(OfContextData value) => this.value = value;

        public Task<OfContextData> GetAsync(string numeroOf, CancellationToken cancellationToken = default) => Task.FromResult(value);
    }

    private sealed class FakeTypeMatiereRepository : ITypeMatiereRepository
    {
        private readonly List<TypeMatiere> values = [];

        public static FakeTypeMatiereRepository Default { get; } =
            new("M3000L0", "M3010L0", "M3060L0");

        public FakeTypeMatiereRepository(params string[] codes)
        {
            foreach (var code in codes)
            {
                Add(code);
            }
        }

        public void Add(string code, bool actif = true)
        {
            var entity = TypeMatiere.Creer(code, $"Type {code}", DateTime.UtcNow);
            if (!actif)
            {
                entity.Desactiver(DateTime.UtcNow);
            }
            values.Add(entity);
        }

        public Task<IReadOnlyList<TypeMatiere>> GetAllAsync(bool? actif, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TypeMatiere>>(values
                .Where(type => actif is null || type.EstActif == actif)
                .ToArray());

        public Task<TypeMatiere?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(values.SingleOrDefault(type => type.Id == id));

        public Task<TypeMatiere?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken)
            => Task.FromResult(values.SingleOrDefault(type => type.Code == normalizedCode));

        public Task AddAsync(TypeMatiere typeMatiere, CancellationToken cancellationToken)
        {
            values.Add(typeMatiere);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReferences : IOfContextReferencesRepository
    {
        private readonly Dictionary<string, List<RefGammeFabrication>> gammes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string Article, string Composant), List<RefNomenclature>> nomenclatures = [];
        public List<(string Article, string Composant)> NomenclatureLookups { get; } = [];

        public void AddGamme(string article, string atelier, string operation, string groupe)
        {
            if (!gammes.TryGetValue(article, out var rows)) gammes[article] = rows = [];
            rows.Add(RefGammeFabrication.Creer(
                article, null, null, null, null, operation, atelier, "POSTE", null, groupe,
                null, null, null, null, null, null, null, null, null, null, null, null,
                "TEST", Batch, Date.ToDateTime(TimeOnly.MinValue)));
        }

        public void AddNomenclature(string article, string composant)
        {
            var key = (article, composant);
            if (!nomenclatures.TryGetValue(key, out var rows)) nomenclatures[key] = rows = [];
            rows.Add(RefNomenclature.Creer(article, null, "1", composant, "1", "1", "TEST", Batch, Date.ToDateTime(TimeOnly.MinValue)));
        }

        public Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticleAsync(string codeArticle, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RefGammeFabrication>>(gammes.TryGetValue(codeArticle, out var rows) ? rows : []);

        public Task<IReadOnlyList<RefGammeFabrication>> GetGammesActivesByArticlesAsync(
            IReadOnlyCollection<string> codesArticle,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RefGammeFabrication>>(codesArticle
                .Where(gammes.ContainsKey)
                .SelectMany(code => gammes[code])
                .ToArray());

        public Task<IReadOnlyList<RefNomenclature>> GetNomenclaturesByArticleAndComponentAsync(string codeArticleParent, string codeComposant, CancellationToken cancellationToken = default)
        {
            NomenclatureLookups.Add((codeArticleParent, codeComposant));
            return Task.FromResult<IReadOnlyList<RefNomenclature>>(nomenclatures.TryGetValue((codeArticleParent, codeComposant), out var rows) ? rows : []);
        }
    }

    private sealed class FakeParametres(
        IReadOnlyDictionary<string, string> values) : IParametreApplicationProvider
    {
        public static FakeParametres Default { get; } = new(new Dictionary<string, string>
        {
            [ParametresApplicationCles.CodeAtelierUap2] = "16",
            [ParametresApplicationCles.TypesMatiere] = "M3000L0,M3010L0,M3060L0",
            [ParametresApplicationCles.TransactionEntreeOf] = "Entree OF",
            [ParametresApplicationCles.SeuilEcartFonderie] = "5.0",
            [ParametresApplicationCles.SeuilEcartUap2] = "1.5"
        });

        public Task<string?> GetValueAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult(values.TryGetValue(cle, out var value) ? value : null);

        public Task<decimal?> GetDecimalAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult(values.TryGetValue(cle, out var value) && decimal.TryParse(value, out var parsed)
                ? parsed
                : (decimal?)null);

        public Task<IReadOnlyList<string>> GetCsvValuesAsync(string cle, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(values.TryGetValue(cle, out var value)
                ? value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(BilanMetierRules.NormaliserCle)
                    .ToArray()
                : []);
    }
}
