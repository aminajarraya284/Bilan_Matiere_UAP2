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
| Tableau de bord | `mock` | aucun |
| Rapports | `mock` | aucun |
| Alertes | `mock` | aucun |
| Journal d'audit | `mock` | aucun |
| Administration | `mock` | aucun |
| Synchronisation Sage X3 | `disabled` | aucun |

Les verticales en `mock` conservent des appels vers des routes qui n'existent pas encore ; ces appels sont inaccessibles tant que la source reste `mock`, et documentes comme tels dans les services concernes.

La strategie de connexion progressive des fonctionnalites est detaillee dans [`MigrationProgressive.md`](MigrationProgressive.md).

La configuration de la verticale implementee est detaillee dans [`VerticaleTypesMatiereDechetsFonderie.md`](VerticaleTypesMatiereDechetsFonderie.md).
