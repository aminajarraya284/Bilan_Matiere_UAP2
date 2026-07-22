# Verticale Types matière et Déchets Fonderie

## Périmètre

Cette verticale connecte uniquement la page Déchets Fonderie à ASP.NET Core et SQL Server. Les imports X3, bilans, inventaires et autres modules conservent leur fonctionnement mock actuel.

Le flux est le suivant :

```text
Vue Déchets Fonderie
  -> Axios /api/v1
  -> contrôleurs ASP.NET Core
  -> services Application
  -> repositories Infrastructure / EF Core
  -> SQL Server
```

## Sources frontend

Les sources sont sélectionnées indépendamment dans `frontend/.env` :

```env
VITE_API_URL=http://localhost:5000/api
VITE_TYPES_MATIERE_SOURCE=api
VITE_DECHETS_SOURCE=api
```

Les valeurs possibles sont `mock` et `api`. `VITE_USE_REAL_API` reste le fallback des autres modules et peut rester à `false`.

## Authentification temporaire

Les endpoints utilisent déjà des policies de rôles. En attendant JWT, le schéma `Development` est activé uniquement si :

- `ASPNETCORE_ENVIRONMENT=Development` ;
- `Authentication:DevelopmentMode=true`.

Il crée par défaut un utilisateur de développement avec le rôle `Admin`. Les en-têtes suivants permettent de tester les policies localement :

```text
X-Development-Role: ResponsableFonderie
X-Development-User: Responsable Fonderie
X-Development-UserId: 00000000-0000-0000-0000-000000000001
```

Ce mécanisme est inactif hors de l'environnement Development. Il devra être remplacé par JWT avant la production.

## Migration et base

La migration `AddTypesMatiereAndDechetsFonderie` contient les tables, index, relation et les trois matières initiales. Elle n'est pas appliquée automatiquement.

Depuis la racine du dépôt :

```powershell
dotnet restore backend/BilanMatiereUAP2.sln
dotnet build backend/BilanMatiereUAP2.sln
dotnet test backend/BilanMatiereUAP2.sln

dotnet ef database update `
  --project backend/src/BilanMatiereUAP2.Infrastructure `
  --startup-project backend/src/BilanMatiereUAP2.Api
```

Pour régénérer la migration dans une branche où elle n'existe pas encore :

```powershell
dotnet ef migrations add AddTypesMatiereAndDechetsFonderie `
  --project backend/src/BilanMatiereUAP2.Infrastructure `
  --startup-project backend/src/BilanMatiereUAP2.Api
```

Ne pas exécuter cette dernière commande une deuxième fois tant que la migration fournie est présente.

## Démarrage

```powershell
dotnet run --project backend/src/BilanMatiereUAP2.Api
```

Le health check `/health` retourne l'état global et le contrôle `sql-server`. Une base inaccessible produit un état `Unhealthy`.
