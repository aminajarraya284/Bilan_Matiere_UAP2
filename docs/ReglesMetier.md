# Regles metier

Ce document recense les regles deja implementees et signale les domaines qui restent a valider avec les responsables metier.

## Fonderie

La premiere verticale operationnelle couvre le referentiel Types matiere et les Dechets Fonderie. Les calculs complets du bilan Fonderie restent a definir.

## Usinage 2

A definir.

## UAP2

A definir.

## Imports X3

A definir.

## Inventaire

A definir.

## Dechets

- Une declaration reference obligatoirement un type matiere actif.
- Le referentiel initialise contient `M3000L0`, `M3010L0` et `M3060L0`.
- La date de declaration est obligatoire et determine automatiquement la semaine ISO.
- Le type de dechet est `Bavure` ou `Poudre`.
- Le poids est strictement superieur a zero.
- L'observation est facultative, normalisee et limitee a 500 caracteres.
- Un dechet annule ne peut plus etre modifie.
- La suppression est logique : la ligne est marquee supprimee et son statut devient annule.

Ces regles correspondent a l'implementation actuelle de la verticale `/api/v1`. Elles doivent rester soumises a la validation metier avant une mise en production.

## Receptions U2

A definir.

## Validation mensuelle

A definir.
