# Migration progressive du frontend vers le backend

## Objectif

La migration progressive consiste à connecter les fonctionnalités au backend une par une, sans interrompre le fonctionnement des écrans qui utilisent encore les données de démonstration.

À ce jour, le frontend fonctionne principalement selon le flux suivant :

```text
Dataset de test
    ↓
Services mock
    ↓
localStorage
    ↓
Écrans Vue
```

Le backend fournit le socle technique ASP.NET Core, mais il ne possède encore aucun endpoint métier. Il expose uniquement le health check `/health`.

Le frontend possède actuellement un interrupteur global :

```env
VITE_USE_REAL_API=false
```

Le passage direct à `true` demanderait à plusieurs modules d'appeler simultanément des endpoints qui n'existent pas encore :

- authentification ;
- déchets Fonderie ;
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

## Exemple : première verticale Déchets Fonderie

Pendant la première migration :

```text
Types matière      → API réelle + SQL Server
Déchets Fonderie   → API réelle + SQL Server
Réceptions U2      → données mock
Inventaire         → données mock
Bilans             → données mock
Dashboard          → données mock
```

L'application reste utilisable, mais l'écran Déchets Fonderie devient réellement connecté au backend.

Le référentiel minimal des types matière fait partie de cette première verticale, car une déclaration de déchet doit référencer un type matière valide. Dans l'état actuel du code, les trois codes utilisés sont :

```text
M3000L0
M3010L0
M3060L0
```

Cette liste doit être validée avec les responsables métier avant la création de la première migration SQL.

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

Une configuration explicite peut être ajoutée dans `.env.example` :

```env
VITE_DATA_SOURCE_DECHETS=mock
VITE_DATA_SOURCE_RECEPTIONS=mock
VITE_DATA_SOURCE_INVENTAIRE=mock
VITE_DATA_SOURCE_BILANS=mock
VITE_DATA_SOURCE_REFERENCES=mock
VITE_DATA_SOURCE_CONTROLE_POIDS=mock
```

Valeurs autorisées :

```text
mock
api
```

La lecture de ces variables doit être centralisée dans un fichier tel que :

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

Les repositories frontend doivent donc appeler des chemins relatifs sans répéter `/api` :

```typescript
await api.get('/dechets-fonderie');
await api.get(`/dechets-fonderie/${id}`);
await api.post('/dechets-fonderie', data);
await api.put(`/dechets-fonderie/${id}`, data);
await api.delete(`/dechets-fonderie/${id}`);
```

Avec la configuration actuelle, les routes backend correspondantes sont :

```http
GET    /api/dechets-fonderie
GET    /api/dechets-fonderie/{id}
POST   /api/dechets-fonderie
PUT    /api/dechets-fonderie/{id}
DELETE /api/dechets-fonderie/{id}
```

La version `/api/v1` ne doit pas être introduite tant qu'une stratégie de versionnement n'a pas été décidée et configurée dans le backend. Si elle est ajoutée plus tard, elle devra être appliquée de manière cohérente à la base Axios, aux contrôleurs et à la documentation OpenAPI.

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

La vue reste stable. Seule la source des données change.

## Première migration recommandée

### Étape 0 — Validation métier

Avant de créer les entités et la migration SQL, valider :

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

Créer la configuration centralisée et le contrat `DechetsRepository`.

Tous les modules restent en mode mock :

```env
VITE_DATA_SOURCE_DECHETS=mock
VITE_DATA_SOURCE_RECEPTIONS=mock
VITE_DATA_SOURCE_INVENTAIRE=mock
VITE_DATA_SOURCE_BILANS=mock
```

### Étape 2 — Types matière et Déchets Fonderie

Créer le référentiel minimal `TypeMatiere`, la fonctionnalité `DechetFonderie`, les contrats, les validateurs, la persistance EF Core et les endpoints API.

Après validation des tests :

```env
VITE_DATA_SOURCE_DECHETS=api
```

Les autres modules restent explicitement en mode mock.

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
0. Validation des règles métier et des contrats
1. Configuration progressive des sources frontend
2. Types matière minimaux + Déchets Fonderie
3. Référentiels OF, gammes et nomenclatures
4. Contrôle poids Fonderie
5. Réceptions Usinage 2
6. Inventaire
7. Imports et synchronisation X3
8. Bilan Usinage 2
9. Bilan Fonderie
10. Bilan UAP2
11. Dashboard, alertes et rapports
12. Administration et audit complet
```

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

La première verticale recommandée reste **Types matière minimaux + Déchets Fonderie**, après validation de ses règles métier.
