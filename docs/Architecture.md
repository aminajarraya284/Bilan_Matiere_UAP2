# Architecture

## Vue d'ensemble

Le depot est un monorepo simple :

- `frontend/` contient l'application Vue.js existante ;
- `backend/` contient l'API ASP.NET Core et ses couches ;
- `database/` est reserve aux scripts SQL manuels et a la documentation de base de donnees ;
- `docs/` centralise la documentation d'architecture, les futures specifications OpenAPI et les diagrammes UML.

Le frontend et le backend peuvent etre installes, executes et testes independamment.

## Projets backend

- **BilanMatiereUAP2.Domain** : coeur metier et invariants. Il ne depend d'aucun autre projet interne.
- **BilanMatiereUAP2.Contracts** : contrats d'echange de l'API. Il ne depend d'aucun projet metier interne.
- **BilanMatiereUAP2.Application** : cas d'usage et validations. Il depend de Domain et Contracts.
- **BilanMatiereUAP2.Infrastructure** : integration technique, Entity Framework Core et SQL Server. Il depend d'Application et Domain.
- **BilanMatiereUAP2.Api** : point d'entree HTTP et composition des dependances. Il depend d'Application, Infrastructure et Contracts.
- **BilanMatiereUAP2.Api.IntegrationTests** : tests de l'API en memoire. Il depend d'Api.

Ces dependances sont unidirectionnelles et ne forment aucun cycle.

## Flux applicatif

```text
Frontend Vue.js
      |
      v
API ASP.NET Core
      |
      v
Application
      |
      v
Infrastructure (Entity Framework Core)
      |
      v
SQL Server
```

La couche API recoit les requetes du frontend sous `/api/v1`. La couche Application execute les cas d'usage, Infrastructure realise les acces techniques via Entity Framework Core et Domain porte les concepts et invariants metier.

En environnement Development, l'application utilise la base `BilanMatiereUAP2` sur l'instance `.\SQLEXPRESS` et initialise les matieres `M3000L0`, `M3010L0` et `M3060L0`.

### Etat des verticales

La source de donnees de chaque verticale est pilotee par `frontend/src/config/dataSources.ts`, seul endroit autorise a lire les variables `VITE_*_SOURCE`. Une verticale en `mock` n'emet aucun appel reseau ; une verticale `disabled` affiche « Fonctionnalite non disponible ».

| Verticale | Source | Endpoint backend |
| --- | --- | --- |
| Authentification | `api` | `/api/auth` |
| Types matiere | `api` | `/api/v1/types-matiere` |
| Dechets Fonderie | `api` | `/api/v1/dechets-fonderie` |
| Controle poids Fonderie | `api` | `/api/v1/controles-poids-fonderie` |
| Receptions Usinage 2 | `api` | `/api/v1/receptions-usinage2` |
| Rebuts Usinage 2 | `api` | `/api/v1/rebuts-usinage2` |
| Etats X3 / Sorties magasin | `api` | `/api/v1/mouvements-stock`, `/api/v1/consommations-composants` |
| References fabrication | `api` | `/api/v1/references` |
| Historique des imports | `api` | `/api/v1/imports` |
| **Inventaire Fonderie** | `api` | `/api/v1/inventaires-fonderie/comptage` |
| **Bilan Fonderie** | `api` | `/api/v1/bilans/fonderie/{periode}` |
| **Bilan Usinage 2** | `api` | `/api/v1/bilans/usinage2/{periode}` |
| **Bilan UAP2** | `api` | `/api/v1/bilans/uap2/{periode}` |
| **Tableau de bord** | `api` | `/api/v1/dashboard?periode=YYYY-MM` |
| **Alertes** | `api` | `/api/v1/alertes` |
| Rapports | `mock` | aucun |
| Journal d'audit | `mock` | aucun |
| Administration | `mock` | aucun |
| Synchronisation Sage X3 | `disabled` | aucun |

Les verticales qui restent en `mock` sont isolées du réseau. Une erreur d'une verticale en mode `api` est affichée telle quelle : aucun repli silencieux vers une donnée de démonstration n'est autorisé.

### Agrégation du Dashboard

Le Dashboard utilise un unique appel HTTP côté frontend :

```text
DashboardView.vue
      |
      v
GET /api/v1/dashboard?periode=YYYY-MM
      |
      v
DashboardService (Application)
      |-- BilanFonderieService
      |-- BilanUsinage2Service
      |-- BilanUap2Service
      `-- DashboardReadRepository (compteurs SQL mensuels)
```

L'agrégateur appelle directement les services applicatifs existants ; il ne réalise aucun appel HTTP interne et ne recalcule pas leurs écarts. Le contrat conserve trois résumés indépendants : Fonderie en kilogrammes, Usinage 2 et UAP2 en pièces. Une valeur inconnue ou non calculable reste `null`, accompagnée d'un statut et d'un avertissement ; elle n'est jamais remplacée par zéro.

Les statuts possibles sont `OK`, `AVerifier`, `Anomalie`, `Partiel` et `NonDisponible`. La priorité globale est : `Anomalie`, puis `Partiel` (y compris lorsqu'une seule source est indisponible), puis `AVerifier`, puis `OK`. Si toutes les sources sont indisponibles, le statut global est `NonDisponible`.

Les compteurs d'inventaires, de réceptions validées et d'imports sont calculés sur le mois demandé. Le nombre de contrôles poids ambigus n'étant pas disponible sous forme d'agrégat mensuel fiable dans le modèle actuel, le contrat expose explicitement `null` et un avertissement au lieu de fabriquer une valeur.

L'endpoint est autorisé aux rôles `Admin`, `ResponsableFonderie` et `ResponsableUsinage2`. La vue limite en plus les liens de détail au périmètre métier du rôle, sans remplacer le contrôle d'accès du backend.

### Alertes métier persistantes

Le module Alertes réutilise les sorties des services métier ; il ne duplique ni leurs formules ni leurs seuils :

```text
AlertesView.vue
      |
      v
GET/POST /api/v1/alertes
      |
      v
AlerteApplicationService
      |-- AlerteGenerationService (déduplication et cycle de vie)
      |-- AlerteDetectionService
      |     |-- BilanFonderieService
      |     |-- BilanUsinage2Service
      |     |-- BilanUap2Service
      |     `-- DashboardReadRepository (statuts d'import)
      `-- AlerteRepository
            |
            v
       SQL Server / AlertesMetier
```

La clé active combine le code, la période, l'OF, l'article, le type matière et la source. Un index unique filtré complète le contrôle applicatif. Les listes sont paginées et utilisent `AsNoTracking`; les actions chargent seulement l'entité ciblée. Les dates relues depuis SQL sont explicitement restaurées avec le type UTC.

Les rôles `Admin`, `ResponsableFonderie` et `ResponsableUsinage2` passent les policies HTTP. Le service applicatif réduit ensuite les sources visibles et traitables au périmètre métier du rôle. La description complète est disponible dans [`AlertesMetier.md`](AlertesMetier.md).

La strategie de connexion progressive des fonctionnalites est detaillee dans [`MigrationProgressive.md`](MigrationProgressive.md).

La configuration de la verticale implementee est detaillee dans [`VerticaleTypesMatiereDechetsFonderie.md`](VerticaleTypesMatiereDechetsFonderie.md).
