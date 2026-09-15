# Analyse des problèmes restants

## Corrections implémentées dans ce lot

- Plusieurs contrôles poids Fonderie valides applicables sont agrégés par moyenne arithmétique.
- Les contrôles Brouillon et Annulé restent exclus par le dépôt de candidats.
- Une valeur de poids nulle ou négative reste invalide.
- Le rebut U2 est soustrait dans l'écart UAP2.
- Une Entrée OF absente ou dont l'unité n'est pas `UN` produit `null` pour l'entrée, l'écart et le taux.
- Une CTRL absente produit `null` pour l'écart et le taux, avec le statut `DonneesManquantes`.
- Les anomalies Entrée OF sont exposées par le service d'alertes.
- Les contrats TypeScript représentent maintenant correctement ces valeurs nullables.

## Problème bloquant restant : BOMARTPARENT

La règle métier demandée indique que la colonne H de `BOMARTPARENT` est le poids théorique U2.
Cependant, le lecteur et les tests présents dans le dépôt utilisent actuellement cette zone comme
`Qté lien`, avec une structure variable selon le niveau de nomenclature. Aucun fichier
`BOMARTPARENT` réel n'est présent dans `recette-artifacts` pour vérifier l'en-tête, l'unité et
la position exacte de la valeur.

Par sécurité, aucune conversion automatique de `QuantiteLien` en `kg/piece` n'est appliquée.
Il faut fournir un export réel ou un échantillon anonymisé comprenant la ligne d'en-tête et
quelques lignes de données. La correction devra alors :

1. fixer le mapping de H dans le lecteur ;
2. conserver la valeur brute si l'unité est inconnue ;
3. ne convertir en `PoidsTheoriqueUnitaireKg` que si l'unité est explicitement compatible ;
4. signaler `POIDS_THEORIQUE_AMBIGU` si plusieurs valeurs applicables existent ;
5. ajouter une migration et les tests d'import correspondants si un nouveau champ persistant est requis.

## Validation locale obtenue

- Backend unitaires : 390/390.
- Backend intégration : 218/218.
- Frontend : 376/376.
- TypeScript, lint et build frontend : verts.
- Build API Release : 0 avertissement, 0 erreur.

## Conclusion

Le code peut être testé et relu, mais le commit ne doit pas être déclaré production-ready tant que
le mapping réel de `BOMARTPARENT` et son unité ne sont pas confirmés. Les archives historiques ne
doivent pas être recalculées automatiquement après cette correction.
