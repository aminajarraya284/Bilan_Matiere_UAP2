# SOPAL Bilan Matière Frontend

Application frontend Vue 3 pour la gestion du bilan matière, de l'inventaire et des déchets de la fonderie SOPAL UAP2.

## Table des matières

- [Description](#description)
- [Stack technique](#stack-technique)
- [Structure principale](#structure-principale)
- [Donnees de test locales](#donnees-de-test-locales)
  - [Regle type matiere](#regle-type-matiere)
  - [Jeux de donnees exposes](#jeux-de-donnees-exposes)
  - [Coherence metier attendue](#coherence-metier-attendue)
  - [Services consommateurs](#services-consommateurs)
  - [Inventaire ESR](#inventaire-esr)
  - [Rapports de test](#rapports-de-test)
  - [Reinitialisation des donnees mock](#reinitialisation-des-donnees-mock)
- [Fonctionnalités clés](#fonctionnalités-clés)
  - [Authentification et permissions](#authentification-et-permissions)
  - [Bilan Matière](#bilan-matière)
  - [Inventaire Fonderie](#inventaire-fonderie)
  - [Déchets Fonderie](#déchets-fonderie)
- [Mode de fonctionnement](#mode-de-fonctionnement)
  - [Mode mock local](#mode-mock-local)
  - [Mode API réelle](#mode-api-réelle)
- [Installation et exécution](#installation-et-exécution)
  - [Prérequis](#prérequis)
  - [Commandes](#commandes)
  - [Variables d'environnement](#variables-denvironnement)
- [Notes de développement](#notes-de-développement)
- [Verification avant livraison](#verification-avant-livraison)
- [Améliorations possibles](#améliorations-possibles)

## Description

Ce projet est un tableau de bord interne construit avec Vue 3, Pinia, Vue Router et Tailwind CSS. Il prend en charge :

- authentification utilisateur et permissions par rôle
- saisie d'inventaires par conteneur et catégorie
- gestion des déchets de fonderie
- calcul automatique du bilan matière par type matière
- validation, clôture et justification des écarts
- impression des rapports de bilan matière

## Stack technique

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router
- Tailwind CSS
- axios
- SheetJS (`xlsx`) — lecture des fichiers Excel importés

### À propos de la dépendance `xlsx`

Elle est installée depuis le CDN de l'éditeur, **et non depuis npm** :

```jsonc
"xlsx": "https://cdn.sheetjs.com/xlsx-0.20.3/xlsx-0.20.3.tgz"
```

C'est le mode d'installation **officiel** de SheetJS depuis la version 0.20 : l'éditeur
ne publie plus sur npm. Le `package-lock.json` porte l'empreinte `sha512` de l'archive,
donc l'intégrité est vérifiée à l'installation comme pour n'importe quel paquet.

**Ne pas « corriger » cette ligne** en revenant au paquet npm `xlsx` : celui-ci est figé
à la version 0.18.5, abandonnée, et porte des vulnérabilités connues (pollution de
prototype, ReDoS). Ce serait une régression de sécurité.

## Structure principale

- `src/main.ts` : point d'entrée de l'application
- `src/router/index.ts` : configuration des routes et des gardes d'accès
- `src/features/` : modules métiers (`auth`, `dashboard`, `bilan-matiere`, `inventaire`, `dechets-fonderie`, `rapports`, `admin`, `audit`, etc.)
- `src/components/layout/` : layout principal, header et sidebar
- `src/services/api.ts` : client HTTP Axios avec gestion du token
- `src/utils/permissions.ts` : règles de permission par rôle
- `src/utils/storage.ts` : stockage local pour le mode mock

## Donnees de test locales

La source officielle des donnees de test frontend est `src/data/testData.ts`.

Ce fichier sert de base coherente pour faire tourner l'application sans backend reel. Il alimente les pages :

- Dashboard
- Sorties magasin X3
- Bilan matiere Fonderie
- Bilan matiere Usinage 2
- Bilan matiere UAP2
- Inventaire Fonderie
- Alertes
- Rapports

Il remplace les anciennes references X3 statiques qui etaient dans :

- `public/reference/x3-reference.json`
- `public/reference/ListeOFX3.xlsx`
- `src/data/x3Reference.ts`

Le frontend ne doit plus dependre de ces fichiers. En mode mock local, les services lisent les donnees via `src/data/testData.ts`, puis `src/utils/storage.ts` les persiste dans le `localStorage` du navigateur.

### Regle type matiere

L'application travaille avec trois types matiere :

- `M3000L0`
- `M3010L0`
- `M3060L0`

Le champ `typeMatiere` ou `codeTypeMatiere` ne doit jamais contenir `Composant`, `Laiton`, `Aluminium`, `Zamak` ou un autre code matiere.

Quand un article est un composant, utiliser le champ dedie :

```ts
categorieArticle: 'Composant';
```

Champs concernes :

- `typeMatiere` dans les articles, bilans et donnees dashboard
- `codeTypeMatiere` dans les receptions, dechets et inventaires
- `codeMatiere` dans les sorties magasin

Si une nouvelle ligne est ajoutee dans `testData.ts`, elle doit respecter cette regle avant meme de verifier l'affichage.

### Jeux de donnees exposes

`src/data/testData.ts` expose notamment :

- `testArticles` : referentiel local des articles utilises par les OF, les receptions, les dechets et les inventaires. Chaque article a un `typeMatiere` valide et peut avoir `categorieArticle: 'Composant'`.
- `testOrdresFabrication` : ordres de fabrication de test. Chaque `codeArticle` doit exister dans `testArticles`.
- `testSortiesMagasin` : sorties magasin issues du scenario X3 local. Chaque OF reference doit exister dans `testOrdresFabrication`.
- `testReceptionsUsinage2` : receptions Usinage 2 utilisees par la page Receptions, les bilans Usinage 2/UAP2 et les alertes de poids.
- `testDechetsFonderie` : declarations de dechets fonderie, rattachees aux memes types matiere.
- `testInventaire` : inventaire physique de fin de periode avec trois familles de donnees : conteneurs, categories incluses dans le bilan et fours hors bilan.
- `testBilans` : bilans calcules ou precharges pour Fonderie, Usinage 2 et UAP2.
- `testAlertes` : alertes de demonstration liees aux ecarts de bilan.
- `testDashboard` : indicateurs consolides affiches sur le tableau de bord.
- `testRapports` : liste des rapports visibles dans la page Rapports.

### Coherence metier attendue

Les donnees de test ne doivent pas etre des lignes isolees. Elles doivent raconter le meme scenario de production.

Regles a respecter :

- un OF utilise dans `testSortiesMagasin`, `testReceptionsUsinage2` ou `testBilans.uap2` doit exister dans `testOrdresFabrication`
- un `codeArticle` utilise dans un OF, une reception, un dechet ou un inventaire conteneur doit exister dans `testArticles`
- les periodes doivent rester alignees avec `BUSINESS_RULES.periodeParDefaut`, actuellement `2026-06`
- les bilans doivent utiliser les memes ateliers, mois et types matiere que les sorties, receptions, dechets et inventaires
- les alertes doivent etre explicables par les ecarts visibles dans les bilans ou dans les receptions
- les donnees dashboard doivent rester coherentes avec les volumes principaux du scenario de test

Quand une valeur change dans une source metier, verifier les donnees derivees :

- modification d'une sortie magasin : verifier le bilan fonderie et le dashboard
- modification d'une reception Usinage 2 : verifier les bilans Usinage 2/UAP2 et les alertes de poids
- modification d'un dechet fonderie : verifier le bilan fonderie et le total de pertes
- modification d'un inventaire : verifier le bilan fonderie et le rapport Inventaire ESR

### Services consommateurs

Les services doivent consommer la data locale au lieu des anciennes references X3 :

- `src/features/sorties-x3/sortiesX3.service.ts` : sorties magasin locales
- `src/features/bilan-matiere/bilan.service.ts` : calculs et lectures des bilans
- `src/features/dashboard/dashboard.service.ts` : indicateurs dashboard
- `src/features/alertes/alertes.service.ts` : alertes de poids et alertes bilan
- `src/features/rapports/rapports.service.ts` : liste des rapports disponibles
- `src/features/rapports/exporter.ts` : generation des exports PDF texte et CSV
- `src/utils/storage.ts` : persistance mock dans `localStorage`

Chaque verticale sélectionne sa source dans `src/config/dataSources.ts` avec une variable `VITE_*_SOURCE`. Les services métier ne lisent jamais directement les variables Vite. `VITE_USE_REAL_API` reste uniquement un repli temporaire et déprécié dans ce module central.

### Inventaire ESR

Le rapport Inventaire ESR correspond a l'etat des stocks reels de fin de periode visible depuis la page Rapports.

Il s'appuie sur `testInventaire` et consolide :

- Partie 1 : inventaire par conteneur / OF
- Partie 2 : inventaire par categorie inclus dans le bilan fonderie
- Partie 3 : inventaire four hors bilan matiere, visible dans le rapport mais non inclus dans le calcul du bilan fonderie

Difference importante :

- `totalInventaireRapport` inclut Partie 1 + Partie 2 + Partie 3
- `totalInventaireBilan` inclut seulement Partie 1 + Partie 2
- les fours sont suivis pour information ESR, mais restent hors bilan pour eviter un double comptage

### Rapports de test

La page Rapports affiche les rapports fournis par `testRapports`.

- `id: 1` : Rapport Bilan Matiere Mensuel
- `id: 2` : Rapport Dechets Fonderie
- `id: 3` : Rapport Receptions Usinage 2
- `id: 4` : Rapport Sorties magasin
- `id: 5` : Rapport Inventaire ESR
- `id: 6` : Rapport Alertes

L'export du rapport Inventaire ESR est gere dans `src/features/rapports/exporter.ts` avec `reportId === 5`. Il consolide les conteneurs, les categories incluses dans le bilan fonderie et les fours hors bilan visibles dans le rapport inventaire.

Les exports disponibles sont :

- `PDF` : export texte telecharge avec une extension `.txt`
- `Excel` : export CSV telecharge avec une extension `.csv`

Quand un rapport est ajoute dans `testRapports`, il faut aussi verifier que son `id` est gere dans `src/features/rapports/exporter.ts`. Sinon la carte du rapport sera visible, mais le telechargement echouera.

### Reinitialisation des donnees mock

Les donnees mock sont initialisees depuis `testData.ts`, puis stockees dans `localStorage`. Si les donnees changent dans le code mais que le navigateur garde d'anciennes valeurs, vider le stockage local.

Depuis la console navigateur :

```js
localStorage.clear();
location.reload();
```

Cette operation recharge les donnees de test propres depuis `src/data/testData.ts`.

## Fonctionnalités clés

### Authentification et permissions

- login avec stockage JWT dans `localStorage`
- redirection par rôle : `Admin`, `ResponsableFonderie`, `ResponsableUsinage2`
- protection des routes via `requiresAuth`
- permissions par route dans `src/utils/permissions.ts`

### Bilan Matière

- calcul du bilan matière en mode mock ou via API réelle
- validation d'une ligne de bilan matière
- validation globale de tous les bilans affichés selon le filtre de type matière
- impression du tableau de bilan
- total consolidé calculé pour l'ensemble des rapports affichés

### Inventaire Fonderie

- saisie Partie 1 par conteneur / OF
- saisie Partie 2 par catégorie
- saisie Partie 3 pour les fours hors bilan, visible dans le rapport Inventaire ESR
- suppression ligne Partie 1 et Partie 2
- validation de l'inventaire
- le champ `Article` a été retiré de la Partie 1 sur la saisie actuelle

### Déchets Fonderie

- déclaration de déchets avec type de matière, cause et type de déchet
- suppression de la colonne `Article Concerné` de la page de déclaration des déchets
- validation et suppression des déclarations
- verticale operationnelle avec le backend et SQL Server lorsque les sources Types matiere et Dechets Fonderie sont configurees sur `api`

## Mode de fonctionnement

### Mode mock local

Le projet supporte un mode hybride par verticale. Les modules sans endpoint backend (`Dashboard`, `Bilan Fonderie`, `Bilan UAP2`, `Inventaire`, `Rapports`, `Audit`, `Administration` et `Alertes`) sont explicitement configurés sur `mock` :

- les donnees initiales proviennent de `src/data/testData.ts`
- `src/utils/storage.ts` persiste les donnees dans `localStorage`
- les services utilisent `localStorage` pour simuler des listes et des actions
- les anciens enregistrements locaux peuvent etre purges en vidant le `localStorage` du navigateur

### Mode API réelle

- configure `VITE_API_URL` pour pointer vers le backend réel
- configure la variable `VITE_*_SOURCE=api` de la verticale connectée
- les endpoints de cette verticale sont exposes sous `/api/v1`
- le client Axios ajoute automatiquement le token Bearer
- les erreurs `401` redirigent vers la page de login

## Installation et exécution

### Prérequis

- Node.js 18+ (ou version compatible)

### Commandes

```bash
npm install
npm run dev
npm run build
npm run preview
npm run lint
```

### Variables d'environnement

- `VITE_API_URL` : URL du backend API
- `VITE_*_SOURCE` : `api`, `mock` ou `disabled` pour chaque verticale
- `VITE_USE_REAL_API` : repli global temporaire déprécié, à ne plus utiliser dans une nouvelle configuration
- la liste complète et les valeurs recommandées figurent dans `.env.example`

## Notes de développement

- le projet utilise `@vitejs/plugin-vue` et `@tailwindcss/vite`
- le nom du package est `react-example`, mais le projet est un frontend Vue 3
- la page de démarrage est accessible via `http://localhost:3000` ou le port de Vite choisi par le serveur

## Verification avant livraison

Avant de livrer une modification, executer :

```bash
npm run typecheck
npm run build
```

Sous PowerShell Windows, utiliser `npm.cmd` si la politique d'execution bloque `npm.ps1` :

```powershell
npm.cmd run typecheck
npm.cmd run build
```

Verifier aussi qu'il ne reste aucune reference aux anciens fichiers X3 statiques :

```bash
rg "x3-reference|ListeOFX3|x3Reference|X3_REFERENCE"
```

Verifier que les types matiere restent limites a `M3000L0`, `M3010L0` et `M3060L0` :

```bash
rg "typeMatiere|codeTypeMatiere|codeMatiere" src/data/testData.ts
```

Controle fonctionnel rapide en mode mock :

- ouvrir le Dashboard et verifier que les indicateurs se chargent
- ouvrir Sorties magasin X3 et verifier que les sorties affichent uniquement `M3000L0`, `M3010L0` et `M3060L0`
- ouvrir Bilan matiere Fonderie, Usinage 2 et UAP2 pour verifier les memes periodes
- ouvrir Inventaire Fonderie et verifier les totaux rapport/bilan
- ouvrir Rapports et exporter `Rapport Inventaire ESR`
- ouvrir Alertes et verifier que les ecarts affiches correspondent aux bilans ou receptions

## Améliorations possibles

- nettoyer les dépendances inutilisées restées de templates précédents
- connecter progressivement au backend les inventaires, bilans et autres verticales encore en mode mock
- renforcer la validation des formulaires et l'expérience d'impression
- ajouter des tests unitaires et e2e
