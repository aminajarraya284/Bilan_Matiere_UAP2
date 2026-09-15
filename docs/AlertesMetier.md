# Alertes métier persistantes

## Objet et limites

Le module Alertes persiste des anomalies déjà produites par les services de bilan et les agrégats SQL existants. Il ne remplace jamais la donnée source, ne définit aucun nouveau seuil et ne recalcule aucune formule de bilan.

Le déclenchement livré au Lot 4 est explicite :

```http
POST /api/v1/alertes/recalculer?periode=YYYY-MM
```

Aucune tâche planifiée et aucun branchement automatique aux validations métier n'ont été ajoutés. Cette limitation réduit le risque d'effet de bord ; l'automatisation des déclencheurs reste `TODO_INFRA`.

L'audit transactionnel et la solution recommandée après commit sont détaillés dans
[`AutomatisationAlertes.md`](AutomatisationAlertes.md). Le recalcul manuel reste
nécessaire après les mutations des sources ; aucun déclencheur automatique n'est
annoncé comme livré.

## Modèle persistant

La table SQL Server `AlertesMetier` conserve :

- le code, le type, la gravité, le statut, le titre et la description ;
- la source, la période, l'OF, l'article et le type matière ;
- la clé de déduplication ;
- la première et la dernière date de détection en UTC ;
- les utilisateurs et dates d'acquittement, de résolution et d'ignorance ;
- le commentaire de traitement ;
- un JSON limité à 4 000 caractères, composé uniquement de données techniques autorisées ;
- l'éligibilité à la résolution automatique et l'indicateur d'activité.

Le contenu d'une alerte automatique n'est pas modifiable librement et aucun endpoint de création manuelle n'existe.

## Cycle de vie

| Statut | Sens | Actions autorisées |
| --- | --- | --- |
| `Ouverte` | Condition détectée et non traitée | Acquitter, résoudre, ignorer |
| `Acquittee` | Un utilisateur a pris connaissance | Résoudre, ignorer |
| `Resolue` | Cause traitée ou condition sûre disparue | Lecture seule |
| `Ignoree` | Écart volontairement écarté avec justification | Lecture seule |

L'acquittement et la résolution sont idempotents. L'ignorance exige un commentaire non vide. Une occurrence résolue ou ignorée reste conservée. Si la même condition réapparaît après un état terminal, une nouvelle occurrence est créée ; aucune méthode de réouverture silencieuse n'est implémentée.

## Gravités

Les seules valeurs sont `Information`, `Avertissement` et `Erreur`. Aucun niveau `Critique` n'existe.

La classification actuelle est prudente et technique : `Avertissement` pour les anomalies métier ou données manquantes, `Erreur` uniquement pour un import dont le statut source est `Echec`. La gravité opérationnelle et l'effet éventuel sur une clôture restent `TODO_METIER` / **À CONFIRMER** par SOPAL.

## Déduplication

La clé stable est :

```text
Code | Periode | NumeroOF | CodeArticle | TypeMatiere | Source
```

Chaque composante est trimée, passée en majuscules, les valeurs absentes deviennent `<NULL>` et les caractères `%` et `|` sont échappés. La date de recalcul, la quantité, la description, l'utilisateur et tout GUID aléatoire sont exclus.

Un index SQL unique filtré sur `EstActive = 1` complète le contrôle applicatif. Deux recalculs identiques mettent à jour `DerniereDetectionLeUtc` sans modifier `DetecteeLeUtc` et sans créer de deuxième ligne active.

## Catalogue livré

`Avertissement*` signifie que la gravité est une classification technique prudente, `TODO_METIER` pour sa validation opérationnelle.

| Code | Source | Condition source réelle | Unité | Gravité | Résolution auto. | Validation |
| --- | --- | --- | --- | --- | --- | --- |
| `FON_INVENTAIRE_DEBUT_MANQUANT` | Bilan Fonderie | Anomalie source `STOCK_DEBUT_MANQUANT` | kg | Avertissement* | Oui | Condition existante |
| `FON_INVENTAIRE_FIN_MANQUANT` | Bilan Fonderie | Anomalie source `STOCK_FIN_MANQUANT` | kg | Avertissement* | Oui | Condition existante |
| `FON_SORTIE_X3_UNITE_NON_CONVERTIBLE` | Bilan Fonderie | Anomalie de sortie X3 dans une unité non convertible ; ligne exclue du total source | kg attendu | Avertissement* | Oui | Condition existante ; conversion `RM-FON-003` à confirmer |
| `FON_RECEPTION_U2_POIDS_MANQUANT` | Bilan Fonderie | Une réception ne contribue pas au bilan faute de poids exploitable | kg | Avertissement* | Oui | Condition existante |
| `FON_RECEPTION_U2_TYPE_MATIERE_NON_RESOLU` | Bilan Fonderie | Type matière d'une réception non résolu par le bilan | — | Avertissement* | Oui | Condition existante |
| `U2_QUANTITE_NON_RECEPTIONNEE` | Bilan Usinage 2 | `QuantiteNonReceptionneePieces > 0` calculée par le bilan | pièces | Avertissement* | Oui | Condition existante, aucun seuil ajouté |
| `U2_RECEPTION_SUPERIEURE_EMR` | Bilan Usinage 2 | `QuantiteNonReceptionneePieces < 0` calculée par le bilan | pièces | Avertissement* | Oui | Condition existante, signe conservé |
| `U2_ARTICLE_OF_INTROUVABLE` | Bilan Usinage 2 | Anomalie source `ARTICLE_OF_INTROUVABLE` | — | Avertissement* | Oui | Condition existante |
| `U2_ENTREE_OF_INTROUVABLE` | Bilan Usinage 2 | Anomalie source `ENTREE_OF_INTROUVABLE` | pièces attendues | Avertissement* | Oui | Condition existante |
| `U2_ENTREE_OF_UNITE_INCOMPATIBLE` | Bilan Usinage 2 | Anomalie source `ENTREE_OF_UNITE_INCOMPATIBLE` | unité source explicite | Avertissement* | Oui | Condition existante |
| `U2_TYPE_MATIERE_INTROUVABLE` | Bilan Usinage 2 | Anomalie source `TYPE_MATIERE_SORTIE_MP_INTROUVABLE` | — | Avertissement* | Oui | Condition existante |
| `U2_POIDS_PHYSIQUE_MANQUANT` | Bilan Usinage 2 | Ligne éligible atelier 16 sans poids physique unitaire | kg/pièce | Avertissement* | Oui | Condition existante |
| `U2_POIDS_THEORIQUE_MANQUANT` | Bilan Usinage 2 | Ligne éligible atelier 16 sans poids théorique unitaire | kg/pièce | Avertissement* | Oui | Condition existante ; `RM-U2-010` à confirmer |
| `UAP2_CTRL_MANQUANT` | Bilan UAP2 | Anomalie source CTRL absent | pièces | Avertissement* | Oui | CTRL reste une référence |
| `UAP2_POIDS_PHYSIQUE_MANQUANT` | Bilan UAP2 | Conversion de la sortie impossible faute de poids physique | kg pour la sortie, pièces pour l'écart | Avertissement* | Oui | Unités séparées |
| `UAP2_BILAN_A_VERIFIER` | Bilan UAP2 | Statut source « À vérifier » selon le paramètre déjà appliqué par le bilan | pièces | Avertissement* | Oui | Aucun seuil dupliqué |
| `UAP2_PARAMETRE_SEUIL_MANQUANT` | Bilan UAP2 | Anomalie source d'absence de `SEUIL_ECART_UAP2` | — | Avertissement* | Oui | Paramètre existant |
| `X3_IMPORT_ECHEC` | Import X3 | Nombre d'imports au statut source `Echec` supérieur à zéro sur le mois | nombre | Erreur | Non | Statut source existant |
| `X3_IMPORT_PARTIEL` | Import X3 | Nombre d'imports au statut source `Partiel` supérieur à zéro sur le mois | nombre | Avertissement* | Non | Statut source existant |

Un fichier identique nominal ne génère aucune alerte. Les codes Références, Inventaire détaillé et Contrôle poids ambigu ne sont pas créés faute de signal source mensuel suffisamment précis dans les services actuels.

## Résolution automatique

La résolution automatique est activée uniquement pour une condition courante recalculable. Si elle n'est plus détectée lors du recalcul de la même période, l'alerte est résolue par `SYSTEME`. Les alertes d'import restent soumises à un traitement humain : la disparition d'un compteur mensuel ne prouve pas que l'échec historique a été traité.

Une alerte ignorée n'est jamais résolue automatiquement. La disparition inexpliquée d'une source n'est pas interprétée comme une valeur zéro.

## API et autorisations

| Méthode | Route | Policy | Description |
| --- | --- | --- | --- |
| GET | `/api/v1/alertes` | `Alertes.Read` | Liste paginée et filtrée |
| GET | `/api/v1/alertes/{id}` | `Alertes.Read` | Détail et traçabilité |
| POST | `/api/v1/alertes/recalculer?periode=YYYY-MM` | `Alertes.Manage` | Recalcul explicite |
| POST | `/api/v1/alertes/{id}/acquitter` | `Alertes.Manage` | Acquittement |
| POST | `/api/v1/alertes/{id}/resoudre` | `Alertes.Manage` | Résolution sans modifier la source |
| POST | `/api/v1/alertes/{id}/ignorer` | `Alertes.Manage` | Ignorance avec justification |

Les trois rôles applicatifs peuvent lire et traiter dans leur périmètre. `Admin` accède à toutes les sources, `ResponsableFonderie` à `Bilan Fonderie`, et `ResponsableUsinage2` à `Bilan Usinage 2` et `Bilan UAP2`. Le contrôle est appliqué dans le backend ; le frontend ne constitue pas une barrière de sécurité.

## Migration

Migration EF Core : `20260806091249_AddPersistentBusinessAlerts` (`AddPersistentBusinessAlerts`).

Elle crée uniquement la table `AlertesMetier` et ses index. Elle n'a pas été appliquée à une base distante. Commande de génération utilisée :

```powershell
dotnet ef migrations add AddPersistentBusinessAlerts --project src/BilanMatiereUAP2.Infrastructure --startup-project src/BilanMatiereUAP2.Api --configuration Release
```

Commande d'application locale, à exécuter seulement sur une base locale explicitement choisie :

```powershell
dotnet ef database update --project src/BilanMatiereUAP2.Infrastructure --startup-project src/BilanMatiereUAP2.Api --configuration Release
```

## Frontend

La verticale `alertes` est `api` par défaut. `AlertesView.vue` utilise un service Axios et un store Pinia typés. L'écran gère chargement, erreur, vide, filtres, pagination, détail, actions et navigation vers une source existante. Une erreur API reste visible et ne charge jamais les anciens mocks.

Les anciens types de démonstration encore nécessaires à des tests historiques ont été isolés sous le nom `alertes.legacy-mock.types.ts` ; ils ne sont pas importés par le service, le store ou la vue de production Alertes.

## Points ouverts

- `TODO_METIER` : valider la gravité opérationnelle de chaque code et les éventuels cas bloquants.
- `TODO_METIER` : définir une politique de rétention ou d'archivage des alertes terminales.
- `TODO_INFRA` : décider quels événements sûrs déclencheront automatiquement un recalcul.
- **À CONFIRMER** : le signe du rebut U2 (`RM-UAP2-002`) reste inchangé.
- **À CONFIRMER** : aucune conversion X3 non-KG n'est ajoutée (`RM-FON-003`).
