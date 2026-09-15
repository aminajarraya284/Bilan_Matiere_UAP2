# Migration progressive du frontend vers le backend

## Objectif

La migration progressive consiste à connecter les fonctionnalités au backend une par une, sans interrompre le fonctionnement des écrans qui utilisent encore les données de démonstration. Le présent document conserve le point de départ historique puis décrit l'état atteint après le Lot 3.

Au démarrage de la migration, le frontend fonctionnait principalement selon le flux suivant :

```text
Dataset de test
    ↓
Services mock
    ↓
localStorage
    ↓
Écrans Vue
```

Le backend ASP.NET Core est actif. Il expose le health check `/health` et les verticales métier décrites dans [`Architecture.md`](Architecture.md#etat-des-verticales) sous `/api/v1`.

Le frontend possédait initialement un interrupteur global :

```env
VITE_USE_REAL_API=false
```

Une bascule directe à `true` aurait demandé aux modules non migrés d'appeler simultanément des endpoints qui n'existaient pas encore :

- authentification ;
- réceptions Usinage 2 ;
- inventaires ;
- bilans ;
- dashboard ;
- alertes ;
- référentiels ;
- synchronisation X3 ;
- rapports ;
- administration ;
- audit.

Cette bascule globale rendrait une partie importante du frontend inutilisable.

La stratégie recommandée est donc :

> Connecter un module au backend, le tester de bout en bout, puis migrer le module suivant tout en conservant explicitement les autres modules en mode mock.

## Point de départ historique : première verticale Déchets Fonderie

À l'issue de la première migration :

```text
Types matière      → API réelle + SQL Server
Déchets Fonderie   → API réelle + SQL Server
Réceptions U2      → données mock
Inventaire         → données mock
Bilans             → données mock
Dashboard          → données mock
```

L'application reste utilisable et l'écran Déchets Fonderie est connecté au backend lorsque ses sources sont configurées sur `api`.

Le référentiel minimal des types matière fait partie de cette première verticale, car une déclaration de déchet doit référencer un type matière valide. Dans l'état actuel du code, les trois codes utilisés sont :

```text
M3000L0
M3010L0
M3060L0
```

Cette liste est initialisée par la migration SQL existante. Elle doit être confirmée avec les responsables métier avant une mise en production ou une extension du référentiel.

## Pourquoi éviter une bascule complète

Une migration simultanée de tous les modules cumulerait trop d'inconnues :

- endpoints manquants ;
- contrats JSON incompatibles ;
- modèle SQL incomplet ;
- règles métier non validées ;
- gestion des erreurs non stabilisée ;
- difficultés à identifier la source d'une régression ;
- risque de rendre tout le frontend inutilisable.

Avec une verticale limitée, le périmètre de diagnostic reste clair :

```text
Écran Déchets
    ↓
Repository Déchets
    ↓
API Déchets
    ↓
Cas d'usage Application
    ↓
Repository EF Core
    ↓
SQL Server
```

## Deux sources explicites par fonctionnalité

Chaque fonctionnalité migrée progressivement possède un contrat commun et deux implémentations.

### Source mock

La source mock utilise le dataset et le stockage actuels :

```text
sopalTestDataset.ts
    ↓
testDataRepository
    ↓
localStorage
```

### Source API

La source API appelle ASP.NET Core :

```text
Repository API
    ↓
Client Axios
    ↓
ASP.NET Core
    ↓
SQL Server
```

L'écran Vue ne doit pas savoir quelle implémentation est active. Il dépend uniquement du contrat commun.

Il ne faut pas basculer automatiquement vers les mocks lorsqu'un appel API échoue. Un tel fallback masquerait les erreurs réelles et pourrait présenter à l'utilisateur des données différentes de celles enregistrées en base.

## Configuration par fonctionnalité

La configuration explicite suivante existe dans `.env.example` :

```env
VITE_TYPES_MATIERE_SOURCE=mock
VITE_DECHETS_SOURCE=mock
```

Valeurs autorisées :

```text
mock
api
```

La lecture de ces variables est centralisée dans :

```text
frontend/src/config/dataSources.ts
```

Les vues et composants ne doivent pas lire directement `import.meta.env`.

## Structure proposée pour Déchets Fonderie

```text
frontend/src/features/dechets-fonderie/
├── DechetsFonderieView.vue
├── dechets.types.ts
├── dechets.service.ts
└── data/
    ├── dechets.repository.ts
    ├── dechets.mock.repository.ts
    ├── dechets.api.repository.ts
    └── dechets.repository.factory.ts
```

Le service existant pourra continuer à exposer les méthodes attendues par la vue et déléguer au repository sélectionné.

## Contrat frontend commun

Les identifiants actuels sont numériques. Le contrat doit donc utiliser `number` et conserver le type existant `DechetMouvement`.

```typescript
import type { DechetMouvement } from '../dechets.types';

export type CreateDechetRequest = Omit<
  DechetMouvement,
  'id' | 'statut' | 'utilisateur'
>;

export type UpdateDechetRequest = Partial<
  Omit<DechetMouvement, 'id' | 'utilisateur'>
>;

export interface DechetsFilters {
  periode?: string;
  codeTypeMatiere?: string;
}

export interface DechetsRepository {
  list(filters?: DechetsFilters): Promise<DechetMouvement[]>;
  getById(id: number): Promise<DechetMouvement>;
  create(data: CreateDechetRequest): Promise<DechetMouvement>;
  update(id: number, data: UpdateDechetRequest): Promise<DechetMouvement>;
  remove(id: number): Promise<void>;
}
```

## Sélection du repository

```typescript
import { dataSources } from '@/config/dataSources';
import { ApiDechetsRepository } from './dechets.api.repository';
import { MockDechetsRepository } from './dechets.mock.repository';

export const dechetsRepository =
  dataSources.dechets === 'api'
    ? new ApiDechetsRepository()
    : new MockDechetsRepository();
```

L'écran ou le service appelle ensuite :

```typescript
const dechets = await dechetsRepository.list();
```

Il ne contient aucune condition technique de ce type :

```typescript
if (useRealApi) {
  // appel HTTP
} else {
  // accès localStorage
}
```

## URLs API

Le client Axios actuel utilise déjà cette base :

```text
http://localhost:5000/api
```

Le service frontend appelle des chemins relatifs versionnés sans répéter `/api` :

```typescript
await api.get('/v1/types-matiere');
await api.get('/v1/dechets-fonderie');
await api.get(`/v1/dechets-fonderie/${id}`);
await api.post('/v1/dechets-fonderie', data);
await api.put(`/v1/dechets-fonderie/${id}`, data);
await api.delete(`/v1/dechets-fonderie/${id}`);
```

Avec la configuration actuelle, les routes backend correspondantes sont :

```http
GET    /api/v1/types-matiere
GET    /api/v1/dechets-fonderie
GET    /api/v1/dechets-fonderie/{id}
POST   /api/v1/dechets-fonderie
PUT    /api/v1/dechets-fonderie/{id}
DELETE /api/v1/dechets-fonderie/{id}
```

Le préfixe `/api/v1` est appliqué dans les contrôleurs backend et dans les appels du service frontend.

## Fonctionnement en mode mock

```text
DechetsFonderieView.vue
        ↓
dechets.service.ts
        ↓
MockDechetsRepository
        ↓
testDataRepository
        ↓
localStorage
```

Ce mode permet de conserver le prototype, les démonstrations et les tests sans SQL Server.

## Fonctionnement en mode API

```text
DechetsFonderieView.vue
        ↓
dechets.service.ts
        ↓
ApiDechetsRepository
        ↓
Axios
        ↓
ASP.NET Core API
        ↓
Application
        ↓
Infrastructure / EF Core
        ↓
SQL Server
```

La vue reste stable. Seule la source des données change. En environnement Development, l'API utilise la base `BilanMatiereUAP2` sur l'instance `.\SQLEXPRESS`.

## État de la première migration

### Étape 0 — Validation métier

Les entités et la migration SQL existent. Les points métier suivants restent à confirmer avant la production et doivent guider leurs évolutions :

- les types matière officiels ;
- les types de déchets autorisés ;
- la règle `poids > 0` ;
- les champs obligatoires ;
- les statuts possibles ;
- les règles de modification ;
- suppression physique ou annulation logique ;
- comportement lorsque la période est clôturée ;
- rôles autorisés ;
- données d'audit nécessaires.

### Étape 1 — Sélection progressive des sources

La sélection des sources est centralisée dans `frontend/src/config/dataSources.ts`.

La verticale peut être activée indépendamment des autres modules :

```env
VITE_TYPES_MATIERE_SOURCE=api
VITE_DECHETS_SOURCE=api
```

### Étape 2 — Types matière et Déchets Fonderie

Le référentiel minimal `TypeMatiere`, la fonctionnalité `DechetFonderie`, les contrats, les validateurs, la persistance EF Core et les endpoints API sont implémentés.

La verticale opérationnelle utilise :

```env
VITE_TYPES_MATIERE_SOURCE=api
VITE_DECHETS_SOURCE=api
```

À cette étape historique, les autres modules restaient explicitement en mode mock.

## État actuel après le Lot 3 — Dashboard réel

Le Dashboard a rejoint les verticales API. Sa configuration par défaut est :

```env
VITE_DASHBOARD_SOURCE=api
```

Son flux est volontairement réduit à un seul appel réseau :

```text
DashboardView.vue
        ↓
dashboard.store.ts
        ↓
dashboard.service.ts
        ↓
GET /api/v1/dashboard?periode=YYYY-MM
        ↓
DashboardService + services de bilan existants
        ↓
SQL Server
```

La période est obligatoire et strictement validée au format `YYYY-MM`. Une erreur HTTP efface la donnée précédente du store et reste visible ; elle ne déclenche aucun fallback mock. Le mock Dashboard restant est un état vide explicite, réservé au développement et aux tests, sans valeur métier fictive.

Les écarts ne sont ni recalculés dans la vue ni additionnés entre unités : la Fonderie reste en kilogrammes, Usinage 2 et UAP2 restent en pièces. Les zéros retournés par un calcul réel sont affichés, tandis qu'une absence de source est conservée sous forme de `null` avec le statut `NonDisponible` ou `Partiel`.

Le Dashboard ne nécessite ni nouvelle table ni migration EF Core. Il réutilise les bilans existants et ajoute des projections de lecture mensuelles sur les inventaires, les réceptions et les lots d'import.

## État actuel après le Lot 4 — Alertes persistantes

La verticale Alertes a rejoint les sources API :

```env
VITE_ALERTES_SOURCE=api
```

Le flux complet est désormais :

```text
AlertesView.vue
        ↓
alertes.store.ts
        ↓
alertes.service.ts
        ↓
/api/v1/alertes
        ↓
services de détection et génération
        ↓
AlertesMetier / SQL Server
```

Le recalcul est explicite et dédupliqué. Il conserve la première date de détection, actualise la dernière, et ne résout automatiquement que les codes dont la condition courante est sûre. Les actions utilisateur sont persistées avec date UTC, utilisateur et commentaire. Une erreur API efface la liste courante et reste visible ; aucun mock n'est chargé en remplacement.

La migration `AddPersistentBusinessAlerts` crée la table et les index nécessaires. Elle n'est pas appliquée automatiquement et ne doit jamais être exécutée sur une base distante sans autorisation.

## État actuel après le Lot 6 — Journal d'audit persistant

La verticale Audit utilise désormais :

```env
VITE_AUDIT_SOURCE=api
```

Le frontend interroge uniquement `GET /api/v1/audit` et `GET /api/v1/audit/{id}`. La liste est paginée et filtrée côté serveur ; le détail restitue des valeurs ciblées avant/après sans afficher un JSON brut. Une panne API reste visible et n'active aucun mock ou `localStorage`.

La production des traces est transverse : les services Application d'inventaire, déchets, contrôle poids, réceptions, rebuts, imports X3 et alertes appellent `IAuditService`. Les lectures Dashboard, Bilans et Rapports ne créent aucune trace. La migration `AddPersistentAuditTrail` crée uniquement `AuditTraces` et ses index ; elle n'est pas appliquée automatiquement.

La page et l'API sont limitées à `Admin`. Les étapes suivantes concernent l'Administration réelle, puis la recette et le déploiement SOPAL ; elles ne doivent pas réintroduire de fallback Audit.

## Migrations suivantes

### Référentiels et données OF

Migrer progressivement :

- types matière complets ;
- ordres de fabrication ;
- gammes ;
- nomenclatures ;
- composants OF ;
- lectures nécessaires aux futurs calculs.

L'import Excel et la synchronisation X3 constituent une étape séparée. Ils ne doivent pas être mélangés avec la création initiale des modèles et des endpoints de consultation.

### Contrôle poids Fonderie

Persister les contrôles actuellement conservés dans `localStorage` et exposer leurs endpoints.

Le code actuel ne relie pas directement les contrôles poids Fonderie aux Réceptions Usinage 2. Cette dépendance doit être confirmée fonctionnellement avant de concevoir les clés étrangères ou les règles de calcul.

### Réceptions Usinage 2

Après validation des référentiels, implémenter :

- recherche des valeurs par défaut d'un OF ;
- saisie et consultation des réceptions ;
- calcul du poids physique unitaire ;
- récupération du poids théorique depuis la nomenclature ;
- calcul de l'écart ;
- seuil d'alerte ;
- contrôle des quantités non réceptionnées.

Les règles `> 3 %` ou `>= 3 %`, ainsi que le traitement des quantités négatives, doivent être validées avant le portage des calculs vers le backend.

### Inventaire et bilans

Migrer ensuite :

- inventaire conteneurs/OF ;
- inventaire par catégorie ;
- fours hors bilan ;
- bilan Usinage 2 ;
- bilan Fonderie ;
- bilan UAP2 ;
- dashboard ;
- alertes ;
- rapports.

Chaque calcul backend devra être comparé aux résultats du dataset et des tests frontend existants avant activation.

## Ordre recommandé

```text
0. Validation des règles métier et des contrats          [fait]
1. Configuration progressive des sources frontend        [fait]
2. Types matière minimaux + Déchets Fonderie             [fait]
3. Référentiels OF, gammes et nomenclatures              [fait]
4. Contrôle poids Fonderie                               [fait]
5. Réceptions Usinage 2                                  [fait]
6. Inventaire                                            [fait]
7. Imports et synchronisation X3                         [imports faits ; synchronisation disabled]
8. Bilan Usinage 2                                       [fait]
9. Bilan Fonderie                                        [fait]
10. Bilan UAP2                                           [fait]
11. Dashboard                                             [fait — API]
12. Alertes                                               [fait — API persistante]
13. Rapports                                              [fait — API réelle]
14. Journal d'audit                                      [fait — API persistante, Admin]
15. Administration                                       [à faire — mock]
```

L'état détaillé, verticale par verticale, est tenu à jour dans
[`Architecture.md`](Architecture.md#etat-des-verticales).

Deux points d'arbitrage métier restent ouverts sur les bilans déjà livrés
(voir [`ReglesMetier.md`](ReglesMetier.md#index-des-règles-à-valider)) :
conversion des sorties X3 en unités non-KG (`RM-FON-003`) et signe du rebut U2
dans l'écart UAP2 (`RM-UAP2-002`).

L'authentification et les autorisations backend sont obligatoires avant toute mise en production avec de vrais utilisateurs. Elles peuvent être développées parallèlement aux premières verticales, mais aucun endpoint métier ne doit être exposé en production sans contrôle d'accès serveur.

## Quand supprimer les mocks

Les mocks ne doivent pas être supprimés immédiatement.

Pendant la migration :

```text
Mock → démonstration, tests et référence de comparaison
API  → source réelle explicitement sélectionnée
```

Un module peut passer par défaut en mode API lorsque :

1. ses règles métier sont validées ;
2. ses contrats OpenAPI sont stabilisés ;
3. ses migrations SQL sont reproductibles ;
4. les tests unitaires et d'intégration réussissent ;
5. l'écran fonctionne avec SQL Server ;
6. les erreurs API sont affichées correctement ;
7. les résultats correspondent au prototype validé ;
8. les logs et données d'audit nécessaires sont présents.

Le repository mock peut rester disponible pour les tests automatisés et les démonstrations. Il ne doit jamais servir de fallback silencieux à une panne de l'API en production.

## Critère de réussite d'une verticale

Chaque verticale doit fournir un flux complet et vérifiable :

```text
Vue
  ↓
Contrat frontend
  ↓
Repository API
  ↓
Contrat HTTP / OpenAPI
  ↓
Application
  ↓
Domaine validé
  ↓
Infrastructure / SQL Server
  ↓
Tests et observabilité
```

Les verticales **Dashboard**, **Alertes**, **Rapports** et **Journal d'audit** sont opérationnelles sur les données réelles disponibles. `audit` utilise `api` par défaut, ne lit plus `localStorage` et n'a aucun fallback vers les anciennes traces fictives. L'API Audit est en lecture seule et réservée à `Admin`; les événements sont produits par les services métier instrumentés. La prochaine verticale est l'Administration réelle et sécurisée.
