# Authentification Microsoft Entra ID

Ce document décrit la configuration attendue pour exécuter l'application hors
environnement `Development`. Il complète `RM-SEC-001` à `RM-SEC-003` de
[`ReglesMetier.md`](ReglesMetier.md).

## Principe

Deux schémas d'authentification, mutuellement exclusifs :

| Environnement | Schéma | Condition |
| --- | --- | --- |
| `Development` | `DevelopmentAuthenticationHandler` (en-tête `X-Development-Role`) | `Authentication:DevelopmentMode=true` |
| Tout autre | JWT Bearer / OIDC Entra ID | `Authentication:Jwt:Authority` **et** `Authentication:Jwt:Audience` renseignés |

**Le mode Development ne peut pas être réactivé hors `Development`.** Forcer
`Authentication:DevelopmentMode=true` en Production ne change rien : le démarrage échoue
tant que la configuration Entra ID est absente. C'est vérifié par
`EntraIdAuthenticationTests`.

De même, `Authority` sans `Audience` est refusé : sans validation de l'audience, un jeton
émis pour **une autre application du même tenant** serait accepté.

## Configuration

```jsonc
{
  "Authentication": {
    "DevelopmentMode": false,
    "Jwt": {
      // Autorité OIDC du tenant SOPAL.
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
      // Client ID (ou URI d'application) de l'API enregistrée dans Entra ID.
      "Audience": "api://<APPLICATION_ID>",

      // Facultatif : si le tenant émet à la fois des jetons v1 et v2.
      "ValidIssuers": [
        "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
        "https://sts.windows.net/<TENANT_ID>/"
      ],

      // Facultatif : correspondance app role Entra ID -> rôle applicatif.
      // Inutile si les app roles portent déjà les libellés applicatifs.
      "CorrespondanceRoles": {
        "BilanMatiere.Admin": "Admin",
        "BilanMatiere.Fonderie": "ResponsableFonderie",
        "BilanMatiere.Usinage2": "ResponsableUsinage2"
      }
    }
  }
}
```

Ne jamais committer `Authority` et `Audience` de production dans le dépôt : les fournir
par variables d'environnement ou secrets de déploiement
(`Authentication__Jwt__Authority`, `Authentication__Jwt__Audience`).

## Rôles applicatifs

Les trois rôles de `RM-SEC-001` sont **sans hiérarchie** : `Admin`,
`ResponsableFonderie`, `ResponsableUsinage2`.

Côté Entra ID, ils doivent être déclarés comme **app roles** de l'enregistrement
d'application, puis assignés aux utilisateurs ou aux groupes. Entra ID les publie dans la
revendication `roles`, que l'API lit comme revendication de rôle.

Si les libellés Entra ID diffèrent des libellés applicatifs, renseigner
`CorrespondanceRoles`. La transformation **ajoute** les rôles applicatifs sans retirer les
rôles d'origine : une correspondance incomplète ne retire jamais un accès légitime, elle
se contente de ne pas en ajouter.

## Ce qui reste à faire côté SOPAL

1. Créer l'enregistrement d'application dans le tenant Entra ID.
2. Déclarer les trois app roles et les assigner.
3. Fournir `TENANT_ID` et `APPLICATION_ID`.
4. Configurer le frontend pour obtenir un jeton d'accès (MSAL) et l'envoyer en
   `Authorization: Bearer`. **Non implémenté à ce jour** : le frontend utilise encore le
   flux de connexion de développement.

## Vérification

```bash
cd backend
dotnet test --filter "FullyQualifiedName~EntraIdAuthenticationTests"
```

Les cas couverts :

- démarrage réussi avec `Authority` + `Audience` ;
- démarrage refusé avec `Authority` seule ;
- démarrage refusé si `DevelopmentMode` est forcé hors `Development` ;
- requête sans jeton refusée (`401`) ;
- en-tête `X-Development-Role` sans effet hors `Development`.
