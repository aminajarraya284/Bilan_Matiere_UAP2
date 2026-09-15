# Journal d'audit persistant

## Objectif et périmètre

Le Journal d'audit conserve un événement historique immuable pour les mutations métier importantes. Il répond aux questions : qui a fait quoi, sur quelle entité, quand, avec quelles valeurs ciblées avant/après et dans quelle requête HTTP.

Il ne remplace pas les données métier, ne trace pas les lectures et ne contient ni fichier importé ni secret. Les endpoints Audit sont strictement en lecture et réservés au rôle `Admin`.

## Architecture

```text
Service métier existant
    -> IAuditService
    -> AuditDataSanitizer
    -> TraceAudit
    -> IAuditRepository
    -> SQL Server / AuditTraces

AuditView.vue
    -> audit.store.ts
    -> audit.service.ts
    -> GET /api/v1/audit
    -> AuditQueryService
    -> IAuditRepository (AsNoTracking + pagination)
```

Les hooks sont placés dans les services Application, jamais dans les contrôleurs. Les services et le repository d'audit partagent le même `BilanMatiereUAP2DbContext` par requête. Une mutation déjà suivie par une transaction explicite, notamment le comptage d'inventaire, conserve donc la trace dans cette transaction. Aucun second mécanisme Unit of Work n'a été ajouté.

## Modèle persistant

La table `AuditTraces` contient :

| Champ | Usage |
|---|---|
| `Id` | Identifiant technique de la trace |
| `DateUtc` | Date de l'événement, obligatoirement UTC |
| `Action` | Action stable de l'enum `ActionAudit` |
| `Domaine` | Domaine stable de l'enum `DomaineAudit` |
| `Entite`, `EntiteId` | Type et identifiant de l'objet concerné |
| `ReferenceMetier` | Période, OF, article ou référence lisible selon le domaine |
| `UtilisateurId`, `UtilisateurNom`, `UtilisateurEmail`, `Role` | Identité issue des claims Development ou Entra |
| `Source` | Service applicatif ayant produit la trace |
| `CorrelationId` | Identifiant commun à la requête et aux logs |
| `Resume` | Résumé court de l'action |
| `AnciennesValeursJson`, `NouvellesValeursJson` | Champs métier ciblés avant/après |
| `InformationsJson` | Compteurs ou métadonnées ciblées ne relevant pas d'un avant/après |

L'entité expose uniquement des getters avec setters privés. Elle ne possède aucune méthode de modification ou de suppression. Le repository ne fournit ni `Update` ni `Delete`, et l'API n'expose aucun `POST`, `PUT` ou `DELETE` sur `/audit`.

Les valeurs JSON sont limitées à 16 000 caractères chacune. `null` est conservé comme `null` et reste distinct de `0`.

## Actions et domaines

Actions réellement utilisées :

```text
Creation, Modification, Validation, Annulation,
ImportReussi, ImportPartiel, ImportEchoue, ImportDuplicate,
Acquittement, Resolution, Ignorance, Recalcul
```

Domaines réellement utilisés :

```text
InventaireFonderie, DechetFonderie, ControlePoidsFonderie,
ReceptionUsinage2, RebutUsinage2, ImportX3, Alerte
```

## Catalogue des événements

| Domaine | Actions | Valeurs ciblées | Référence métier |
|---|---|---|---|
| Inventaire Fonderie | Création, modification, validation, annulation | période, type matière, quantité/poids en kg, statut et champs propres à la ligne | période, type matière, ID de ligne |
| Déchets Fonderie | Création, modification, validation, annulation | date, type matière, type de déchet, poidsKg, statut | date, type matière, ID |
| Contrôle poids Fonderie | Création, modification, validation, annulation | date, article, OF éventuel, poidsUnitaireKg, nombre d'échantillons, statut | article, OF ou `SANS_OF`, ID |
| Réceptions Usinage 2 | Création, modification, validation, annulation, recalcul du contexte | date, OF, article, quantitePieces, poidsTotalReceptionneKg, atelier, type matière, statut | OF, article, ID |
| Rebuts Usinage 2 | Création, modification, validation, annulation | date, OF, article, quantitePieces, motif, statut | OF, article, ID |
| Imports X3 | succès, partiel, échec, doublon | type, nom de fichier, batch éventuel, statut et compteurs globaux disponibles | type et nom du fichier |
| Alertes | acquittement, résolution, ignorance | code, source, période, OF, article, statut, commentaire | code, période, OF |
| Alertes | recalcul | période, détections, créations, mises à jour, résolutions automatiques | période |

Un import produit au maximum une trace de résultat. Aucun contenu de fichier, classeur, ligne industrielle ou hash n'est enregistré dans le Journal d'audit.

Le résultat actuel du générateur d'alertes ne distingue pas une catégorie « inchangée » : toute détection déjà active met à jour sa dernière date et entre dans `nombreMisesAJour`. Le Journal conserve donc uniquement les compteurs réels exposés par le service, sans fabriquer `nombreInchangees`.

## Identité utilisateur

`HttpAuditContext` lit l'identifiant, le nom, l'email et les rôles depuis le `ClaimsPrincipal` déjà authentifié :

- Development : claims produits par `DevelopmentAuthenticationHandler` ou par le jeton local signé ;
- Entra : `nameidentifier`, `oid`, `sub`, `name`, `preferred_username`, `upn`, `roles` et claims standards déjà validés par l'authentification JWT ;
- traitement non interactif : `UtilisateurId=SYSTEM`, `UtilisateurNom=Système`, `Role=System`.

Le rôle `Admin` n'est jamais utilisé comme valeur de remplacement d'une tâche système.

## Correlation ID

`CorrelationIdMiddleware` traite le header `X-Correlation-ID` :

1. une valeur fournie est acceptée uniquement si elle contient au plus 100 caractères sûrs (`A-Z`, `a-z`, chiffres, `.`, `_`, `:`, `-`) ;
2. sinon un GUID technique est généré ;
3. la valeur est placée dans `HttpContext.Items`, renvoyée dans le header de réponse et ajoutée au contexte Serilog ;
4. toutes les traces de la requête utilisent cette même valeur.

## Sécurité et assainissement

`AuditDataSanitizer` filtre, sans distinction de casse et également dans les objets/tableaux imbriqués, toute clé contenant :

```text
Password, PasswordHash, Secret, ClientSecret, AccessToken, RefreshToken,
Authorization, Cookie, ConnectionString, ApiKey, PrivateKey,
CertificatePassword
```

Les objets inconnus ne sont pas sérialisés automatiquement : ils deviennent `[NON_SERIALISE]`. Le code produit des dictionnaires explicites ; il ne sérialise ni `HttpContext`, ni entité EF complète, ni requête contenant un fichier.

## API et autorisation

| Méthode | Route | Policy | Usage |
|---|---|---|---|
| `GET` | `/api/v1/audit` | `Audit.Read` (`Admin`) | Recherche filtrée, tri `DateUtc DESC`, pagination |
| `GET` | `/api/v1/audit/{id}` | `Audit.Read` (`Admin`) | Détail et valeurs structurées avant/après |

Filtres : `dateDepuis`, `dateJusqua`, `utilisateur`, `role`, `action`, `domaine`, `entite`, `referenceMetier`, `correlationId`, `recherche`, `page`, `pageSize`. La taille de page est comprise entre 1 et 200.

Un utilisateur authentifié sans rôle Admin reçoit `403`. Un jeton invalide ou une requête non authentifiée hors Development reçoit `401`.

## Frontend

La verticale `audit` utilise `api` par défaut. `AuditView.vue` affiche :

- total et date de dernière activité ;
- filtres et pagination serveur ;
- table des événements ;
- panneau de détail avec métadonnées, comparaison lisible avant/après et informations complémentaires ;
- états chargement, erreur et vide.

Une erreur API efface les anciennes données et reste visible. Il n'existe aucun fallback vers `localStorage` ou des traces fictives. Le menu et la route restent visibles uniquement pour `Admin`, sans remplacer la policy backend.

## Migration

Migration EF Core : `20260818171215_AddPersistentAuditTrail`.

Elle crée uniquement `AuditTraces` et les index suivants :

```text
DateUtc
UtilisateurId
Action
Domaine
CorrelationId
ReferenceMetier
Entite + EntiteId + DateUtc
```

Commande de contrôle :

```bash
cd backend
dotnet ef migrations list \
  --project src/BilanMatiereUAP2.Infrastructure \
  --startup-project src/BilanMatiereUAP2.Api
```

La migration n'a pas été appliquée dans le Lot 6. Son application sur une base locale, de recette ou de production exige une autorisation explicite et une sauvegarde préalable. Aucune migration distante n'a été exécutée.

## Limites connues

- Le Journal ne trace volontairement pas les lectures.
- Il ne constitue pas un système d'archivage des fichiers X3.
- La rétention, l'archivage et la purge réglementaire des traces restent à définir avec SOPAL ; aucune suppression n'est exposée dans l'application.
- Le compteur « alertes inchangées » n'existe pas dans le contrat Alertes actuel ; aucun compteur artificiel n'est enregistré.
- Une recette Entra réelle reste nécessaire pour confirmer les claims du tenant SOPAL, même si les variantes de claims sont prises en charge et testées au niveau applicatif.
