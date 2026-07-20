# Bilan Matière UAP2

Application SOPAL de bilan matiere pour l'UAP2. Le depot rassemble le frontend Vue.js existant et le socle technique d'une API ASP.NET Core destinee a communiquer avec SQL Server.

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
- SQL Server pour les futurs acces aux donnees.

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

Le profil HTTPS utilise `https://localhost:7000`.

## Configuration locale

La chaine `ConnectionStrings:DefaultConnection` utilise l'authentification Windows locale et ne contient aucun mot de passe :

```text
Server=localhost;Database=BilanMatiereUAP2Db;Trusted_Connection=True;TrustServerCertificate=True;
```

Les origines CORS sont lues depuis `Cors:AllowedOrigins`. Elles incluent le port demande `5173` ainsi que le port actuel du frontend `3000`.

Pour les futures migrations EF Core, le projet cible sera `BilanMatiereUAP2.Infrastructure` et le projet de demarrage sera `BilanMatiereUAP2.Api`. Elles ne doivent pas etre dupliquees dans `database/migrations/`. Le dossier `database/` accueille uniquement les scripts SQL manuels et la documentation associee.

## Etat actuel

- Le frontend existant a ete conserve sans modification de sa logique, de ses routes, de ses composants ou de ses donnees de test.
- Le backend fournit uniquement le socle technique : Swagger/OpenAPI, Entity Framework Core avec un `DbContext` vide, SQL Server, Serilog, FluentValidation, CORS, health checks et gestion globale des exceptions.
- La logique metier, les entites, les tables, les imports Excel, les migrations metier et l'authentification JWT ne sont pas encore developpes.
