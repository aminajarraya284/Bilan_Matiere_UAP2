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

La verticale Types matiere / Dechets Fonderie est operationnelle de bout en bout. En environnement Development, elle utilise la base `BilanMatiereUAP2` sur l'instance `.\SQLEXPRESS` et initialise les matieres `M3000L0`, `M3010L0` et `M3060L0`. Les autres domaines restent au stade de prototype frontend ou de squelette backend.

La strategie de connexion progressive des fonctionnalites est detaillee dans [`MigrationProgressive.md`](MigrationProgressive.md).

La configuration de la verticale implementee est detaillee dans [`VerticaleTypesMatiereDechetsFonderie.md`](VerticaleTypesMatiereDechetsFonderie.md).
