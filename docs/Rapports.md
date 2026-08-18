# Rapports réels

## Principe

Le module Rapports présente des projections des données déjà calculées ou persistées. Il ne crée aucune donnée source, ne recopie aucune formule métier et n'ajoute aucune migration SQL.

L'aperçu JSON et l'export CSV passent par `RapportsService`. Les trois bilans sont obtenus auprès de leurs services applicatifs existants. L'inventaire, les alertes et les non-réceptions suivent la même règle. `RapportsReadRepository` est limité aux lectures nécessaires à l'historique des imports et aux sections factuelles du détail OF.

## Catalogue

| Code | Rapport | Source réelle | Unité principale | Filtres | Export |
|---|---|---|---|---|---|
| `RPT-001` | Bilan Fonderie mensuel | `BilanFonderieService` | kg | `periode` | CSV |
| `RPT-002` | Bilan Usinage 2 mensuel | `BilanUsinage2Service` | pièces | `periode` | CSV |
| `RPT-003` | Bilan UAP2 mensuel | `BilanUap2Service` | pièces ; sortie MP kg séparée | `periode` | CSV |
| `RPT-004` | Détail par OF | contexte OF, SQL, bilans, alertes | selon section | `numeroOF`, `periode` | CSV |
| `RPT-005` | Alertes par période | `AlerteApplicationService` | selon alerte | `periode`, `statut`, `severite` | CSV |
| `RPT-006` | Inventaire Fonderie | `InventaireFonderieService.GetSyntheseAsync` | kg | `periode`, `codeTypeMatiere` | CSV |
| `RPT-007` | Historique des imports | `ImportBatches`, `ImportX3Batches` | sans objet | dates, pagination | CSV |
| `RPT-008` | Réceptions non réceptionnées | `AlerteNonReceptionService` | pièces | `date`, pagination | CSV |

Tous ces rapports sont disponibles. PDF n'est pas annoncé, car aucun moteur PDF n'est présent et validé dans le dépôt. L'ancien bouton PDF créait un fichier texte depuis les mocks : ce comportement a été supprimé. Excel n'est pas exposé dans ce lot ; CSV constitue le format tabulaire interopérable validé.

## Métadonnées et données partielles

Chaque aperçu fournit :

- le code et le titre du rapport ;
- la date de génération UTC ;
- l'utilisateur ayant demandé la génération lorsqu'il est identifié ;
- la période lorsqu'elle existe ;
- l'unité principale ;
- la liste des unités et des sources réellement mobilisées ;
- les filtres effectivement appliqués ;
- l'indicateur `estPartiel` ;
- les avertissements de qualité.

Une valeur absente reste `null`. Le frontend l'affiche comme « Non renseigné ». Le CSV laisse la cellule vide. Aucun zéro n'est injecté pour compléter un stock, un poids, une quantité ou un total inconnu.

## Détail par OF

RPT-004 regroupe des sections distinctes :

1. contexte OF résolu ;
2. réceptions U2 ;
3. rapports machine par processus ;
4. mouvements X3 ;
5. contrôles poids ;
6. gammes et nomenclatures ;
7. ligne de bilan U2 autorisée ;
8. ligne de bilan UAP2 autorisée ;
9. alertes visibles pour le rôle.

Une section absente est signalée dans `donneesManquantes`. Elle n'est pas fabriquée à partir d'une autre source.

## Export CSV

Caractéristiques :

- encodage UTF-8 avec BOM ;
- séparateur `;` ;
- guillemets autour de chaque cellule ;
- guillemets internes doublés ;
- décimales en représentation invariante ;
- unités dans les en-têtes ;
- cellules absentes vides ;
- neutralisation des cellules commençant par `=`, `+`, `-` ou `@` ;
- nom de fichier limité aux caractères sûrs.

Les exports tabulaires sont limités à 200 lignes par requête dans ce lot. Pour une extraction plus volumineuse, une stratégie d'export asynchrone devra être définie ; elle ne doit pas charger toute une table en mémoire.

## Endpoints

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/v1/rapports/catalogue` | Catalogue filtré par rôle |
| GET | `/api/v1/rapports/fonderie/{periode}` | RPT-001 |
| GET | `/api/v1/rapports/usinage2/{periode}` | RPT-002 |
| GET | `/api/v1/rapports/uap2/{periode}` | RPT-003 |
| GET | `/api/v1/rapports/of/{numeroOF}?periode=YYYY-MM` | RPT-004 |
| GET | `/api/v1/rapports/alertes` | RPT-005 paginé |
| GET | `/api/v1/rapports/inventaire/{periode}` | RPT-006 |
| GET | `/api/v1/rapports/imports` | RPT-007 paginé |
| GET | `/api/v1/rapports/receptions-non-receptionnees` | RPT-008 paginé |
| GET | `/api/v1/rapports/{code}/export` | Export CSV avec les filtres du rapport |

La période respecte `YYYY-MM`. La pagination commence à 1 et la taille de page est comprise entre 1 et 200.

## Autorisations

| Rôle | Périmètre |
|---|---|
| `Admin` | RPT-001 à RPT-008 |
| `ResponsableFonderie` | Fonderie, détail OF, alertes filtrées à ses sources, inventaire et imports |
| `ResponsableUsinage2` | U2, UAP2, détail OF, alertes filtrées à ses sources, imports et non-réceptions |

La route frontend `/rapports` est visible pour les trois rôles. Le backend reste l'autorité : un appel direct hors périmètre retourne `403`.

## Règles préservées

- aucun mélange de kilogrammes et de pièces ;
- CTRL est une référence et non un quatrième processus ;
- signe du rebut U2 inchangé (`À CONFIRMER` dans la règle source) ;
- aucune conversion arbitraire d'une sortie X3 non-KG ;
- aucun seuil ou statut inventé ;
- aucune donnée manquante remplacée par zéro ;
- aucun hash, secret ou contenu de fichier source exposé dans le catalogue.

## Configuration

```text
VITE_RAPPORTS_SOURCE=api
```

En mode API, une erreur reste visible et efface l'ancien aperçu. Aucun fallback mock n'est exécuté. Les anciennes définitions `testRapports` et les exports basés sur `testDataRepository` ont été supprimés.

## Limites ouvertes

- export Excel : non retenu dans ce lot ; à confirmer si un format `.xlsx` natif est requis ;
- export PDF : non disponible, besoin métier et moteur de rendu à confirmer ;
- export de plus de 200 lignes : stratégie asynchrone ou streaming à définir ;
- validation visuelle finale avec une base de recette contenant des données industrielles anonymisées.
