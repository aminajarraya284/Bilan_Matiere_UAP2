using BilanMatiereUAP2.Application.Abstractions;
using BilanMatiereUAP2.Domain.Entities;
using BilanMatiereUAP2.Domain.Services;

namespace BilanMatiereUAP2.Application.Services;

public sealed class OfContextResolver(
    IOfContextDataRepository dataRepository,
    IOfContextReferencesRepository referencesRepository,
    IParametreApplicationProvider parametres,
    ITypeMatiereRepository typeMatiereRepository) : IOfContextResolver
{
    public async Task<OfContextResult> ResolveAsync(
        string numeroOf,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var normalizedOf = BilanMetierRules.NormaliserCle(numeroOf);
        var data = await dataRepository.GetAsync(normalizedOf, cancellationToken);
        var codeAtelierUap2 = BilanMetierRules.NormaliserCle(await parametres.GetValueAsync(
            ParametresApplicationCles.CodeAtelierUap2,
            cancellationToken));
        var typesMatiereAutorises = (await typeMatiereRepository.GetAllAsync(true, cancellationToken))
            .Select(type => BilanMetierRules.NormaliserCle(type.Code))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var anomalies = new List<string>();
        var existsInYrjmch = data.RapportsMachine.Count > 0;
        var existsInEntree = data.EntreesOf.Count > 0;
        var existsInSortie = data.SortiesMp.Count > 0;
        var existsInEncours = data.EncoursProduct is not null;

        if (!existsInYrjmch && !existsInEntree && !existsInSortie && !existsInEncours)
        {
            anomalies.Add("OF_X3_INTROUVABLE");
        }

        var articleCandidates = data.SortiesMp.Select(link => link.CodeArticle)
            .Concat(data.EncoursProduct is null
                ? Array.Empty<string?>()
                : new[] { data.EncoursProduct.ArticlePrincipal })
            .Concat(data.RapportsMachine.Select(row => row.CodeArticle))
            .Concat(data.EntreesOf.Select(row => row.CodeArticle))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(BilanMetierRules.NormaliserCle)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string? article = articleCandidates.Length == 1 ? articleCandidates[0] : null;
        if (articleCandidates.Length == 0)
        {
            anomalies.Add("ARTICLE_OF_INTROUVABLE");
        }
        else if (articleCandidates.Length > 1)
        {
            anomalies.Add("PLUSIEURS_ARTICLES_POUR_OF");
        }

        var designation = article is null
            ? null
            : data.EncoursProduct is { ArticlePrincipal: not null } order &&
              string.Equals(order.ArticlePrincipal, article, StringComparison.OrdinalIgnoreCase)
                ? order.DesignationPrincipale
                : data.RapportsMachine
                    .Where(row => string.Equals(row.CodeArticle, article, StringComparison.OrdinalIgnoreCase))
                    .Select(row => row.DesignationArticle)
                    .Concat(data.EntreesOf
                        .Where(row => string.Equals(row.CodeArticle, article, StringComparison.OrdinalIgnoreCase))
                        .Select(row => row.DesignationArticle))
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        var gammes = article is null
            ? []
            : (await referencesRepository.GetGammesActivesByArticleAsync(article, cancellationToken))
                .Where(gamme => date is null || gamme.EstValideLe(date.Value))
                .ToArray();
        if (string.IsNullOrWhiteSpace(codeAtelierUap2))
        {
            anomalies.Add($"PARAMETRE_APPLICATION_MANQUANT:{ParametresApplicationCles.CodeAtelierUap2}");
        }

        var atelier16 = gammes
            .Where(gamme => string.Equals(
                BilanMetierRules.NormaliserCle(gamme.CodeAtelier),
                codeAtelierUap2,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var distinctGroupes = atelier16
            .Select(gamme => BilanMetierRules.NormaliserCle(gamme.GroupePosteCharge))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string? codeAtelier = atelier16.Length > 0 ? codeAtelierUap2 : null;
        string? groupe = distinctGroupes.Length == 1 ? distinctGroupes[0] : null;
        // Eligibilite UAP2 = AU MOINS UNE ligne GAMMEFAB sur l'atelier UAP2.
        // Plusieurs groupes de postes de charge laissent le GROUPE non resolu (anomalie
        // ATELIER_UAP2_AMBIGU ci-dessous) mais ne retirent pas l'eligibilite : c'est la
        // meme regle que celle appliquee par BilanUsinage2Service.
        var eligible = !string.IsNullOrWhiteSpace(codeAtelierUap2) && atelier16.Length > 0;
        if (gammes.Length == 0)
        {
            anomalies.Add("ATELIER_UAP2_INTROUVABLE");
        }
        else if (atelier16.Length == 0)
        {
            anomalies.Add("HORS_ATELIER_UAP2");
        }
        else if (distinctGroupes.Length > 1)
        {
            anomalies.Add("ATELIER_UAP2_AMBIGU");
        }

        var sortieTypes = data.SortiesMp
            .Select(link => BilanMetierRules.NormaliserCle(link.CodeTypeMatiere))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string? typeMatiere = null;
        var sourceType = SourceTypeMatiere.Inconnue;
        if (sortieTypes.Length == 1)
        {
            typeMatiere = sortieTypes[0];
            sourceType = SourceTypeMatiere.SortieMp;
        }
        else if (sortieTypes.Length > 1)
        {
            anomalies.Add("TYPE_MATIERE_SORTIE_MP_AMBIGUE");
        }
        else if (article is not null)
        {
            if (typesMatiereAutorises.Length == 0)
            {
                anomalies.Add("REFERENTIEL_TYPES_MATIERE_ACTIFS_VIDE");
            }

            var nomenclatureTypes = new List<string>();
            foreach (var candidate in typesMatiereAutorises)
            {
                var rows = await referencesRepository.GetNomenclaturesByArticleAndComponentAsync(article, candidate, cancellationToken);
                if (rows.Any()) nomenclatureTypes.Add(candidate);
            }

            var distinctNomenclatureTypes = nomenclatureTypes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (distinctNomenclatureTypes.Length == 1)
            {
                typeMatiere = distinctNomenclatureTypes[0];
                sourceType = SourceTypeMatiere.Nomenclature;
            }
            else if (distinctNomenclatureTypes.Length > 1)
            {
                anomalies.Add("TYPE_MATIERE_AMBIGUE");
            }
            else
            {
                anomalies.Add("TYPE_MATIERE_INTROUVABLE");
            }
        }
        else
        {
            anomalies.Add("TYPE_MATIERE_INTROUVABLE");
        }

        return new OfContextResult(
            normalizedOf,
            article,
            designation,
            typeMatiere,
            sourceType,
            codeAtelier,
            groupe,
            eligible,
            existsInYrjmch,
            existsInEntree,
            existsInSortie,
            existsInEncours,
            anomalies.Count == 0 ? "Complet" : anomalies[0],
            anomalies.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}
