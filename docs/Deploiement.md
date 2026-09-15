# Déploiement en environnement de recette

## Prérequis

- .NET 8 SDK ;
- Node.js et npm compatibles avec `frontend/package-lock.json` ;
- SQL Server accessible depuis l'API ;
- application Entra ID configurée par SOPAL ;
- HTTPS et reverse proxy configurés.

## Variables backend

Configurer via le gestionnaire de secrets de l'environnement, sans les committer :

- `ConnectionStrings__DefaultConnection` : `<À FOURNIR PAR SOPAL>` ;
- `Authentication__Jwt__Authority` : `<À FOURNIR PAR SOPAL>` ;
- `Authentication__Jwt__Audience` : `<À FOURNIR PAR SOPAL>` ;
- `Cors__AllowedOrigins` : URL frontend de recette ;
- paramètres de stockage des archives d'import.

Hors `Development`, l'API refuse de démarrer si l'autorité et l'audience JWT ne
sont pas configurées ensemble.

## Variables frontend

Partir de `frontend/.env.example` et fournir uniquement les valeurs publiques
nécessaires au client MSAL :

- `VITE_API_URL` ;
- `VITE_AUTH_MODE=entra` ;
- `VITE_ENTRA_CLIENT_ID` ;
- `VITE_ENTRA_TENANT_ID` ou `VITE_ENTRA_AUTHORITY` ;
- URI de redirection et de déconnexion ;
- `VITE_ENTRA_API_SCOPE`.
- `VITE_AUDIT_SOURCE=api`.

Ne jamais mettre de client secret dans le frontend.
Le scope est celui exposé par l'enregistrement de l'API ; il ne doit pas être confondu
avec le Client ID frontend ni avec l'audience validée par le backend.

## Build et démarrage

Depuis `frontend/` :

```bash
npm ci
npm run typecheck
npm run lint
npm run test
npm run build
```

Depuis `backend/` :

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/BilanMatiereUAP2.Api
```

## Migrations

Depuis `backend/` :

```bash
dotnet ef migrations list --project src/BilanMatiereUAP2.Infrastructure --startup-project src/BilanMatiereUAP2.Api
dotnet ef database update --project src/BilanMatiereUAP2.Infrastructure --startup-project src/BilanMatiereUAP2.Api
```

La commande `database update` ne doit être exécutée sur la base de recette qu'après
validation explicite de SOPAL et sauvegarde préalable.

La migration du Journal d'audit est `20260818171215_AddPersistentAuditTrail`. Elle crée uniquement la table `AuditTraces` et ses index. Avant toute application autorisée, générer et relire le script SQL entre la migration précédente et celle-ci. Le Lot 6 ne l'applique ni localement ni à distance.

## Vérifications post-déploiement

- `GET /health` ;
- ouverture du frontend ;
- connexion Entra ;
- appel API authentifié ;
- vérification d'un `401` sans jeton et d'un `403` avec rôle insuffisant ;
- lecture SQL ;
- import Excel de test ;
- calcul d'un bilan sur une période connue ;
- `VITE_RAPPORTS_SOURCE=api`, lecture de `GET /api/v1/rapports/catalogue`, aperçu d'un rapport autorisé puis export CSV ;
- contrôle du nom de fichier, du BOM UTF-8, des unités et des cellules vides pour les données manquantes ;
- `VITE_AUDIT_SOURCE=api`, lecture Admin de `GET /api/v1/audit`, détail d'une trace et vérification d'un `403` pour les deux rôles responsables ;
- mutation de test avec `X-Correlation-ID`, puis contrôle de la même valeur dans la réponse, les logs et `AuditTraces` ;
- contrôle qu'aucun mot de passe, token, cookie, secret ou chaîne de connexion n'est présent dans les réponses et la table Audit ;
- vérification des journaux et de la sauvegarde.

Le Lot 5 n'ajoutait aucune migration SQL pour les Rapports. Le Lot 6 ajoute uniquement `AddPersistentAuditTrail`; les tables existantes de bilans, inventaires, imports, références et alertes restent inchangées.
