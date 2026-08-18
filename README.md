# Bilan Matière UAP2

Application SOPAL de bilan matière pour l'UAP2. Le dépôt rassemble le frontend Vue.js et une API ASP.NET Core connectée à SQL Server. Les saisies opérationnelles, les imports, les trois bilans matière, leur tableau de bord mensuel et les alertes métier persistantes sont désormais servis par le backend.

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

- **Connecté au backend** : Types matière, Déchets Fonderie, Contrôle poids Fonderie, Réceptions et Rebuts Usinage 2, États X3 et sorties magasin, Références de fabrication, Historique des imports, Inventaire Fonderie, les **trois bilans** (Fonderie, Usinage 2, UAP2), le **Dashboard mensuel**, les **Alertes métier persistantes** (`/api/v1/alertes`) et les **Rapports réels** (`/api/v1/rapports`).
- Le Dashboard orchestre les trois services de bilan existants et complète leur résultat avec des compteurs SQL d'inventaires, réceptions et imports. Il conserve séparément l'écart Fonderie en `kg` et les écarts Usinage 2/UAP2 en `pièces` ; aucune somme inter-unités n'est produite et une donnée absente reste `null`.
- **Encore en données de démonstration** : Journal d'audit et Administration. La synchronisation Sage X3 est `disabled` : ni endpoint, ni donnée de démonstration crédible.
- La source de chaque verticale est pilotee par `frontend/src/config/dataSources.ts` (variables `VITE_*_SOURCE`). Aucun repli silencieux : en mode `api`, une erreur reste une erreur. Le tableau complet est dans [`docs/Architecture.md`](docs/Architecture.md#etat-des-verticales).
- Les regles metier sont le referentiel unique : [`docs/ReglesMetier.md`](docs/ReglesMetier.md). Deux arbitrages SOPAL restent ouverts sur les bilans livres (conversion des sorties X3 en unites non-KG, signe du rebut U2 dans l'ecart UAP2).
- Le modèle, le catalogue, les statuts, la déduplication et les endpoints Alertes sont détaillés dans [`docs/AlertesMetier.md`](docs/AlertesMetier.md).
- Le catalogue RPT-001 à RPT-008, les projections, les filtres, les droits et l'export CSV sont détaillés dans [`docs/Rapports.md`](docs/Rapports.md).
- **Authentification** : le frontend prend en charge le mode local `development` et le mode Microsoft Entra ID `entra` via MSAL. Le démarrage backend hors `Development` reste bloqué tant que l'autorité et l'audience Entra ne sont pas configurées (`RM-SEC-003`).
