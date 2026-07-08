# SOPAL Bilan Matière Frontend

Application frontend Vue 3 pour la gestion du bilan matière, de l'inventaire et des déchets de la fonderie SOPAL UAP2.

## Table des matières

- [Description](#description)
- [Stack technique](#stack-technique)
- [Structure principale](#structure-principale)
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

## Structure principale

- `src/main.ts` : point d'entrée de l'application
- `src/router/index.ts` : configuration des routes et des gardes d'accès
- `src/features/` : modules métiers (`auth`, `dashboard`, `bilan-matiere`, `inventaire`, `dechets-fonderie`, `rapports`, `admin`, `audit`, etc.)
- `src/components/layout/` : layout principal, header et sidebar
- `src/services/api.ts` : client HTTP Axios avec gestion du token
- `src/utils/permissions.ts` : règles de permission par rôle
- `src/utils/storage.ts` : stockage local pour le mode mock

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
- suppression ligne Partie 1 et Partie 2
- validation de l'inventaire
- le champ `Article` a été retiré de la Partie 1 sur la saisie actuelle

### Déchets Fonderie

- déclaration de déchets avec type de matière, cause et type de déchet
- suppression de la colonne `Article Concerné` de la page de déclaration des déchets
- validation et suppression des déclarations

## Mode de fonctionnement

### Mode mock local

Le projet supporte un mode mock local si `VITE_USE_REAL_API !== 'true'` :

- les données proviennent de `src/utils/storage.ts`
- les services utilisent `localStorage` pour simuler des listes et des actions

### Mode API réelle

- configure `VITE_API_URL` pour pointer vers le backend réel
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
- `VITE_USE_REAL_API` : `true` pour utiliser l'API réelle, `false` pour utiliser les données mock

## Notes de développement

- le projet utilise `@vitejs/plugin-vue` et `@tailwindcss/vite`
- le nom du package est `react-example`, mais le projet est un frontend Vue 3
- la page de démarrage est accessible via `http://localhost:3000` ou le port de Vite choisi par le serveur

## Améliorations possibles

- nettoyer les dépendances inutilisées restées de templates précédents
- ajouter un vrai backend d'API pour la gestion des inventaires et bilans
- renforcer la validation des formulaires et l'expérience d'impression
- ajouter des tests unitaires et e2e
