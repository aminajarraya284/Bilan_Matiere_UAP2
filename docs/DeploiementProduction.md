# Déploiement en production

## Prérequis

- .NET 8 Runtime/Hosting Bundle ;
- serveur web HTTPS (IIS ou reverse proxy) ;
- SQL Server et compte applicatif à privilèges minimaux ;
- deux inscriptions Entra ID : frontend SPA et API ;
- sauvegarde SQL testée avant migration.

## Secrets et variables

Fournir par variables d’environnement ou coffre de secrets, jamais dans Git :

- `ConnectionStrings__DefaultConnection` ;
- `Authentication__Jwt__Authority` ;
- `Authentication__Jwt__Audience` ;
- `Cors__AllowedOrigins__0` et suivants.

Construire le frontend avec `VITE_AUTH_MODE=entra`, `VITE_API_URL`, `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID` ou `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_API_SCOPE` et les URI de redirection publiques. Aucun client secret ne doit être placé dans le frontend.

## Publication

1. Sauvegarder SQL et vérifier la restauration.
2. Générer puis relire le script EF idempotent : `dotnet ef migrations script --idempotent`.
3. Appliquer le script après validation SOPAL.
4. Publier l’API en Release et configurer HTTPS.
5. Construire le frontend avec `npm ci && npm run build`.
6. Autoriser uniquement l’origine HTTPS réelle dans CORS.
7. Vérifier `/health`, la connexion Entra, les rôles et les scénarios de recette.

## Retour arrière

Arrêter les écritures, restaurer la sauvegarde SQL si la migration a été appliquée, republier la version précédente de l’API et du frontend, puis vérifier `/health`. Une archive officielle ne doit jamais être supprimée manuellement sans procédure métier validée.

