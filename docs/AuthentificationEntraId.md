# Authentification Microsoft Entra ID

Ce document décrit les deux modes d'authentification de l'application et complète
`RM-SEC-001` à `RM-SEC-004` de [ReglesMetier.md](ReglesMetier.md).

## Principe

| Mode frontend | Schéma backend | Condition |
| --- | --- | --- |
| `VITE_AUTH_MODE=development` | `DevelopmentAuthenticationHandler` | environnement ASP.NET Core `Development` et `Authentication:DevelopmentMode=true` |
| `VITE_AUTH_MODE=entra` | JWT Bearer Entra ID | `Authentication:Jwt:Authority` et `Authentication:Jwt:Audience` renseignés |

Le mode Development ne peut pas être réactivé hors `Development`. Forcer
`Authentication:DevelopmentMode=true` en Production ne change rien : le démarrage échoue
tant que la configuration Entra ID est absente. Une autorité sans audience est également
refusée afin de ne pas accepter un jeton destiné à une autre application.

## Configuration backend

```jsonc
{
  "Authentication": {
    "DevelopmentMode": false,
    "Jwt": {
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
      "Audience": "api://<BACKEND_API_CLIENT_ID>",
      "ValidIssuers": [],
      "RoleClaimType": "roles",
      "NameClaimType": "name",
      "CorrespondanceRoles": {}
    }
  }
}
```

Les valeurs de recette et de production sont fournies par variables d'environnement ou
secrets de déploiement :

```text
Authentication__Jwt__Authority=<À FOURNIR PAR SOPAL>
Authentication__Jwt__Audience=<À FOURNIR PAR SOPAL>
```

## Configuration frontend MSAL

Le frontend utilise uniquement la bibliothèque officielle `@azure/msal-browser`.
L'instance `PublicClientApplication` est unique, son cache est placé dans
`sessionStorage` et les access tokens ne sont jamais copiés manuellement dans
`localStorage`.

```env
VITE_AUTH_MODE=entra
VITE_ENTRA_CLIENT_ID=<À FOURNIR PAR SOPAL>
VITE_ENTRA_TENANT_ID=<À FOURNIR PAR SOPAL>
VITE_ENTRA_AUTHORITY=https://login.microsoftonline.com/<À FOURNIR PAR SOPAL>/v2.0
VITE_ENTRA_REDIRECT_URI=<À FOURNIR PAR SOPAL>
VITE_ENTRA_POST_LOGOUT_REDIRECT_URI=<À FOURNIR PAR SOPAL>
VITE_ENTRA_API_SCOPE=<À FOURNIR PAR SOPAL>
```

`VITE_ENTRA_AUTHORITY` peut être omise lorsque `VITE_ENTRA_TENANT_ID` est fourni :
l'autorité v2 est alors construite automatiquement. En mode `entra`, le Client ID, le
scope API et l'autorité ou le Tenant ID sont obligatoires. Une configuration incomplète
produit une erreur visible et ne rebascule jamais en mode Development.

Aucun client secret n'est nécessaire ni autorisé dans une application frontend publique.

## Connexion, token et déconnexion

Le flux principal est `loginRedirect`. Après le retour Microsoft, le service traite le
résultat de redirection, définit le compte actif et normalise l'utilisateur pour le store
Pinia. Les composants ne manipulent jamais d'objet MSAL.

Pour chaque appel API, l'algorithme est :

1. récupérer le compte actif ;
2. appeler `acquireTokenSilent` avec le seul scope API ;
3. ajouter `Authorization: Bearer <access-token>` via l'intercepteur Axios central ;
4. utiliser `acquireTokenRedirect` uniquement si MSAL signale qu'une interaction est requise.

Les acquisitions concurrentes partagent la même promesse. Aucun faux token n'est produit
et aucun token n'est écrit dans les journaux.

La déconnexion Entra utilise `logoutRedirect`. La déconnexion Development conserve le
comportement local historique.

## Claims et rôles

Le contrat réellement configuré dans le backend est :

| Élément | Valeur attendue |
| --- | --- |
| Authority | tenant SOPAL, à fournir |
| Audience | Client ID ou URI de l'API, à fournir |
| API Scope | scope délégué exposé par l'API, à fournir |
| Claim utilisateur | `name` |
| Claim des rôles | `roles` |
| Header HTTP | `Authorization: Bearer <token>` |
| Token absent ou invalide | `401` |
| Rôle insuffisant | `403` |

Les seuls rôles applicatifs reconnus sont :

```text
Admin
ResponsableFonderie
ResponsableUsinage2
```

Un rôle inconnu est ignoré. Un compte authentifié sans rôle autorisé conserve sa session,
mais est redirigé vers `/forbidden`. Aucun rôle par défaut, et notamment aucun rôle Admin,
n'est attribué en mode Entra.

Le backend permet une `CorrespondanceRoles` si les app roles Entra ont d'autres libellés.
Pour que les permissions d'interface correspondent exactement aux policies serveur, SOPAL
doit privilégier les trois libellés applicatifs ci-dessus dans le claim `roles` reçu par le
frontend.

## Gestion des erreurs

- `401` : l'état applicatif est invalidé, la route demandée est mémorisée et l'utilisateur
  est envoyé vers la connexion sans boucle de retry ;
- `403` : la session est conservée et l'utilisateur est envoyé vers `/forbidden` ;
- configuration incomplète : message explicite, aucun fallback Development ;
- compte sans rôle : message explicite et refus des routes métier.

## Configuration attendue de SOPAL

1. créer les enregistrements d'application frontend et API ;
2. exposer le scope délégué de l'API et autoriser le client frontend ;
3. déclarer et assigner les trois app roles ;
4. fournir Tenant ID, Client IDs, audience et API Scope ;
5. enregistrer les URI de redirection et de déconnexion de recette ;
6. fournir des utilisateurs de test et l'URL backend de recette.

## Vérification

```bash
cd frontend
npm run test -- src/config/auth.config.test.ts src/features/auth/msal.service.test.ts src/router/auth.guard.test.ts

cd ../backend
dotnet test --filter "FullyQualifiedName~EntraIdAuthenticationTests|FullyQualifiedName~ProductionAuthenticationGuardTests"
```

Une connexion réelle au tenant reste `TODO_INFRA` tant que les valeurs de recette et les
comptes de test SOPAL ne sont pas fournis. Les tests automatisés simulent le contrat MSAL,
mais ne prétendent pas valider un tenant réel.
