# Bilan Fonderie : consultation par intervalle

## Contrat live

L'écran conserve **Date début** et **Date fin**, au format ISO `YYYY-MM-DD`.
« Actualiser le bilan » effectue une seule lecture ; il ne persiste aucun état métier.
Le changement d'une date recharge également les résultats. Une erreur efface les
anciennes lignes et affiche « Réessayer ». Une réponse ancienne ne remplace pas une
sélection plus récente.

```http
GET /api/v1/bilans/fonderie?dateDebut=2026-07-10&dateFin=2026-07-20
```

Les deux paramètres sont obligatoires, dates valides strictement au format ISO,
avec `dateDebut <= dateFin`. Une borne absente, vide, invalide ou inversée renvoie
`400`. Une journée unique est autorisée. Les bornes sont inclusives.

La route racine existante est utilisée ; elle ne revient plus implicitement au mois
courant lorsqu'aucune date n'est fournie. Sa réponse contient `dateDebut`, `dateFin`
et `bilan`. Dans cette réponse live, `bilan.periode` et la période de chaque ligne
identifient l'intervalle sous la forme `YYYY-MM-DD/YYYY-MM-DD`.

La route `GET /api/v1/bilans/fonderie/{periode}` garde son contrat mensuel et son
`periode` en `YYYY-MM`. Les rapports, le Dashboard, les alertes et l'archivage
continuent d'utiliser le service mensuel.

## Sources et filtrage

| Composante | Date SQL | Filtres conservés |
| --- | --- | --- |
| Sorties X3 | `ConsommationsComposants.DateImputation` | Rapprochées ; seules les KG sont sommées, autres unités signalées |
| Déchets | `DechetsFonderie.DateDeclaration` | Validés, non supprimés |
| Réceptions U2 et anomalies de type matière | `ReceptionsUsinage2.DateReception` | Validées ; règles de poids et type matière inchangées |

Chaque flux applique `date >= dateDebut && date <= dateFin` avant le chargement SQL.
Les agrégations restent décimales, en mémoire comme auparavant : une consultation
sur plusieurs années doit faire l'objet d'une recette volumétrique. Aucun plafond
de durée arbitraire n'est ajouté.

## Limite des stocks

Un inventaire est un **état mensuel**, jamais un flux à sommer entre deux dates.

| Borne de consultation | État retenu |
| --- | --- |
| Début au premier jour d'un mois | `StockDebutKg` de la projection mensuelle correspondante, s'il existe |
| Fin au dernier jour d'un mois | `StockFinKg` de la projection mensuelle correspondante, s'il existe |
| Début ou fin en cours de mois | `null` pour le stock de cette borne |

Du 10 au 20 juillet, les trois flux sont donc disponibles mais les deux stocks
restent inconnus. Les INPUT, OUTPUT et écart dépendants restent `null`, affichés
`—` avec les anomalies de stock manquant. Aucun stock journalier n'est extrapolé.
Sur plusieurs mois entiers, seuls les deux états aux bornes sont utilisés ; les
états intermédiaires ne sont pas additionnés.

Les formules, l'exclusion des fours, les règles de réception et les conversions
sont inchangées. La complétude `Complet` / `Incomplet` n'est pas une validation métier.
La fausse action « Valider tout » est supprimée ; l'archivage officiel reste
« Enregistrer la période » et concerne exclusivement le mois entier choisi.

## Vérification

Les tests HTTP utilisent le repository EF réel sur SQLite isolé. Pour chacun des
trois flux, des valeurs égales au jour sont placées les 9, 10, 15, 20 et 21 juillet :
du 10 au 20 inclus, le total attendu est `45` ; du 10 au 15, `25` ; du 15 au 20,
`35` ; le 20 seul, `20`. Les anomalies de type matière suivent les mêmes bornes.
Le contrat mensuel et le résultat sur les bornes naturelles du même mois sont
comparés, en neutralisant uniquement le libellé de période.

Ces tests ne remplacent pas une recette du nouveau filtre sur SQL Server SOPAL.
La consultation par intervalle nécessite `VITE_BILAN_FONDERIE_SOURCE=api` : le mode
de démonstration mensuel ne prétend pas filtrer une plage qu'il ne sait pas calculer.
