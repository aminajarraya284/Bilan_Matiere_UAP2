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

- Le frontend reste principalement alimente par les donnees de test, tandis que les Types matiere et les Dechets Fonderie peuvent utiliser l'API reelle via `VITE_TYPES_MATIERE_SOURCE` et `VITE_DECHETS_SOURCE`.
- Le backend fournit une premiere verticale metier operationnelle pour les Types matiere et les Dechets Fonderie sous `/api/v1` : validations, Entity Framework Core, migration SQL Server, seed des trois matieres `M3000L0`, `M3010L0` et `M3060L0`, suppression logique, resume et tests.
- Les imports Excel, les bilans, la synchronisation X3 et les autres verticales metier ne sont pas encore connectes au backend. Les policies sont preparees, mais l'authentification JWT doit encore remplacer le mode de developpement avant la production.
- La configuration et les commandes de cette premiere verticale sont documentees dans `docs/VerticaleTypesMatiereDechetsFonderie.md`.
