# Analyse des fichiers X3 pour les consommations composants par OF

## Périmètre analysé

L'analyse est fondée exclusivement sur les deux fichiers de référence fournis hors du dépôt : `EncoursProduct` et `MVTSTOTRSVCR`. Aucun fichier source n'est copié dans le dépôt.

## EncoursProduct

- Une feuille (`Sheet1`) ; l'en-tête est en ligne 1.
- Vingt-et-une colonnes, de `N° Ordre` à `Statut`.
- 44 553 lignes métier ; les lignes restantes de la plage `.xls` sont vides et techniques.
- 14 921 valeurs distinctes de `N° Ordre` : 9 625 OF réels, 5 295 identifiants `SG...` et une valeur non conforme.
- Les 9 625 OF représentent 25 239 lignes d'opération. Un OF apparaît donc plusieurs fois.
- Aucun OF réel ne manque d'article ou de désignation, et aucun OF ne possède plusieurs articles ou désignations principales dans cet échantillon.
- La clé métier de l'OF est `N° Ordre`, à conserver uniquement lorsqu'elle commence par `OF`.
- La combinaison `N° Ordre`, `N°Op`, `Post.P` présente un doublon source. Une opération doit donc aussi conserver son identité de ligne source ; elle ne doit pas être dédupliquée arbitrairement.

Colonnes requises pour le rapprochement : `N° Ordre`, `Article`, `ITMDES1_0`.

Colonnes de traçabilité d'opération : `SituationOF`, `Atl`, `N°Op`, `Post.P`, `Typ.P`, `N.P`, `Eff.P`, `CAD`, `EfOp`, `Qté Prv.`, `QtéRest`, `D.Op.D`, `Date ObjOf`, `Statut`.

Les dates d'opération et objectif sont des valeurs Excel valides, mais couvrent une plage inhabituelle de 1984 à 2034. Elles sont conservées pour la traçabilité ; la date de consommation provient de `Date imput` du mouvement stock.

## MVTSTOTRSVCR

- Deux feuilles obligatoires.
- `Sheet1` contient un préambule en lignes 1 à 9, l'en-tête en ligne 10 et 65 526 lignes de mouvement.
- `Sheet2` est une continuation sans en-tête et contient 18 888 lignes de mouvement.
- Total physique : 84 414 lignes. Un lecteur ne doit jamais ignorer `Sheet2`.

Colonnes nécessaires au calcul :

- `Date imput`
- `Code` (code composant, et non type matière)
- `Désignation`
- `Quantité`
- `Unité`
- `Transaction`
- `Type Pce`
- `Pièce Origine`

Colonnes à conserver pour la traçabilité : `Date crea`, `Heure crea`, `Utilisateur`, `Lot`, `Désignation mouvement`, `Emplacement`, `Statut`, nom de feuille et numéro de ligne source.

Toutes les lignes métier valides portent `Transaction = Sortie OF` et `Type Pce = Suivi matière`. Deux cellules de date ne sont pas exploitables et une ligne ne porte pas ces deux critères ; elles doivent devenir des anomalies explicites. Les quantités valides sont toutes numériques : 81 859 négatives, 2 554 positives et aucune nulle.

Les unités rencontrées sont `UN`, `KG`, `BA`, `M` et `LI`. Il est donc interdit de calculer un total transversal sans unité.

`Pièce Origine` contient 82 603 valeurs commençant par `OF`, 1 810 valeurs non OF et une valeur vide. Seules les valeurs `OF...` sont des candidates au rapprochement avec l'encours.

Les 3 323 codes composants ont chacun une désignation cohérente dans cet échantillon.

## Rapprochement et résultat de référence

```text
MVTSTOTRSVCR.Pièce Origine = EncoursProduct.N° Ordre
```

Après filtrage, 7 036 mouvements correspondent à un OF de l'encours. Le regroupement par `Date imput`, OF, code composant et unité produit 5 396 consommations.

La quantité X3 signée est conservée. La consommation est calculée ainsi :

```text
Consommation = - Somme(Quantités X3 signées)
```

Un retour positif réduit donc une consommation négative et ne doit jamais être transformé par `Abs` ligne par ligne.

## Modèle cible

- `ImportBatch` et `ImportFile` : empreintes SHA-256 et idempotence du couple de fichiers.
- `OrdreFabrication` et `OperationOF` : contexte de l'OF et lignes d'opération source.
- `Composant` : code et désignation X3, indépendant de `TypeMatiere`.
- `MouvementStock` : ligne source signée, avec feuille et numéro de ligne.
- `ConsommationComposant` : groupe date, OF, composant et unité.
- `ImportAnomaly` : ligne non exploitable ou rapprochement impossible.

Contraintes candidates : hash du couple d'import, type de fichier par batch, numéro OF, code composant, `(batch, feuille, ligne source)` pour un mouvement et `(batch, date, OF, composant, unité)` pour une consommation.

## Conséquences pour l'implémentation

`TypeMatiere` ne doit intervenir ni dans le rapprochement ni dans le calcul. Les lignes `SG...`, les origines non OF, les dates invalides et les lignes incomplètes restent traçables comme anomalies ; elles ne génèrent pas de consommation officielle.
