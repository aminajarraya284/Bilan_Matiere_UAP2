# Architecture

## Vue d'ensemble

Le depot est un monorepo simple :

- `frontend/` contient l'application Vue.js existante ;
- `backend/` contient l'API ASP.NET Core et ses couches ;
- `database/` est reserve aux scripts SQL manuels et a la documentation de base de donnees ;
- `docs/` centralise la documentation d'architecture, les futures specifications OpenAPI et les diagrammes UML.

Le frontend et le backend peuvent etre installes, executes et testes independamment.

## Projets backend

- **BilanMatiereUAP2.Domain** : futur coeur metier. Il ne depend d'aucun autre projet interne.
- **BilanMatiereUAP2.Contracts** : futurs contrats d'echange de l'API. Il ne depend d'aucun projet metier interne.
- **BilanMatiereUAP2.Application** : futurs cas d'usage et validations. Il depend de Domain et Contracts.
- **BilanMatiereUAP2.Infrastructure** : integration technique, Entity Framework Core et SQL Server. Il depend d'Application et Domain.
- **BilanMatiereUAP2.Api** : point d'entree HTTP et composition des dependances. Il depend d'Application, Infrastructure et Contracts.
- **BilanMatiereUAP2.Api.IntegrationTests** : tests de l'API en memoire. Il depend d'Api.

Ces dependances sont unidirectionnelles et ne forment aucun cycle.

## Flux applicatif cible

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

La couche API recoit les requetes du frontend. La couche Application portera les cas d'usage. Infrastructure executera les acces techniques vers SQL Server. Domain contiendra les concepts metier lorsqu'ils seront definis.

Le backend actuel constitue uniquement le socle technique : aucune entite, table, migration ou logique metier n'est encore implementee.
