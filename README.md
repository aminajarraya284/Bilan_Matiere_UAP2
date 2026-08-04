# Bilan Matière UAP2

Application SOPAL de bilan matiere pour l'UAP2. Le depot rassemble le frontend Vue.js et une API ASP.NET Core active, connectee a SQL Server pour la verticale Types matiere / Dechets Fonderie.

## Architecture

- **Frontend** : Vue 3, TypeScript et Vite dans `frontend/`.
- **Backend** : ASP.NET Core Web API en architecture en couches dans `backend/`.
- **Base de donnees** : SQL Server via Entity Framework Core.
- **Documentation** : architecture, squelette des regles metier, OpenAPI et UML dans `docs/`.

```text
BilanMatiereUAP2/
|-- frontend/
|   |-- public/
|   |-- src/
|   |-- package.json
|   `-- vite.config.ts
|-- backend/
|   |-- BilanMatiereUAP2.sln
|   |-- src/
|   |   |-- BilanMatiereUAP2.Api/
|   |   |-- BilanMatiereUAP2.Application/
|   |   |-- BilanMatiereUAP2.Contracts/
|   |   |-- BilanMatiereUAP2.Domain/
|   |   `-- BilanMatiereUAP2.Infrastructure/
|   `-- tests/
|       `-- BilanMatiereUAP2.Api.IntegrationTests/
|-- database/
|   |-- migrations/
|   `-- scripts/
`-- docs/
    |-- OpenAPI/
    |-- UML/
    |-- Architecture.md
    `-- ReglesMetier.md
```

## Prerequis

- Node.js et npm compatibles avec les dependances du frontend.
- SDK .NET 8.
- SQL Server Express pour executer localement la verticale connectee.

## Frontend

```bash
cd frontend
npm install
npm run dev
npm run build
npm run lint
npm run test
npm run typecheck
```

Le serveur Vite existant est configure sur `http://127.0.0.1:3000`. Cette configuration a ete conservee pour ne pas modifier inutilement le comportement du frontend.

## Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/BilanMatiereUAP2.Api
```

Avec le profil HTTP defini dans `launchSettings.json` :

- Swagger : `http://localhost:5000/swagger`
- Health check : `http://localhost:5000/health`
- API metier versionnee : `http://localhost:5000/api/v1`

Le profil HTTPS utilise `https://localhost:7000`.

## Configuration locale

En environnement Development, `ConnectionStrings:DefaultConnection` utilise l'authentification Windows locale et ne contient aucun mot de passe :

```text
Server=.\SQLEXPRESS;Database=BilanMatiereUAP2;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

Les origines CORS sont lues depuis `Cors:AllowedOrigins`. Elles incluent le port `5173` ainsi que les ports frontend `3000` et `3001` pour `localhost` ou `127.0.0.1`.

Pour les migrations EF Core, le projet cible est `BilanMatiereUAP2.Infrastructure` et le projet de demarrage est `BilanMatiereUAP2.Api`. Elles ne doivent pas etre dupliquees dans `database/migrations/`. Le dossier `database/` accueille uniquement les scripts SQL manuels et la documentation associee.

## Etat actuel

- **Connecte au backend** : Types matiere, Dechets Fonderie, Controle poids Fonderie, Receptions et Rebuts Usinage 2, Etats X3 et sorties magasin, References de fabrication, Historique des imports, Inventaire Fonderie, et les **trois bilans** (Fonderie, Usinage 2, UAP2).
- **Encore en donnees de demonstration** : Tableau de bord, Rapports, Alertes, Journal d'audit, Administration. La synchronisation Sage X3 est `disabled` : ni endpoint, ni donnee de demonstration credible.
- La source de chaque verticale est pilotee par `frontend/src/config/dataSources.ts` (variables `VITE_*_SOURCE`). Aucun repli silencieux : en mode `api`, une erreur reste une erreur. Le tableau complet est dans [`docs/Architecture.md`](docs/Architecture.md#etat-des-verticales).
- Les regles metier sont le referentiel unique : [`docs/ReglesMetier.md`](docs/ReglesMetier.md). Deux arbitrages SOPAL restent ouverts sur les bilans livres (conversion des sorties X3 en unites non-KG, signe du rebut U2 dans l'ecart UAP2).
- **Avant production** : l'authentification de developpement doit etre remplacee par Entra ID. Le demarrage hors `Development` est deja bloque tant que ce n'est pas configure (`RM-SEC-003`).
