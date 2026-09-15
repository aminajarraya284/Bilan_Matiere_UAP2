# Recette imports reels References, Rapport machine et Contexte U2

## Objectif

Valider les mappings avec les fichiers SOPAL reels sans modifier les fichiers Excel sources.

Fichiers a importer :

- `GAMMEFAB`
- `BOMARTPARENT`
- `YRJMCH`

## Prerequis

- Backend lance en `Development`.
- Base SQL Server de developpement disponible.
- Role `Admin` ou `ResponsableFonderie` pour les imports.
- Conserver une copie non modifiee de chaque fichier Excel.

## Imports

### GAMMEFAB

```http
POST /api/v1/references/gammes/import?profilImport=GAMMEFAB&sourceSystem=X3&isCompleteExport=false
Content-Type: multipart/form-data
```

Verifier dans la reponse et le diagnostic :

- `ImportBatchId` si expose par le service d'import ;
- hash SHA-256 ;
- profil d'import ;
- lignes lues, importees, mises a jour, ignorees et en anomalie ;
- statut final ;
- absence de doublons sur la cle metier ;
- idempotence du second import du meme fichier.

### BOMARTPARENT

```http
POST /api/v1/references/nomenclatures/import?profilImport=BOMARTPARENT&sourceSystem=X3&isCompleteExport=false
Content-Type: multipart/form-data
```

Points d'attention :

- utilisation de `QuantiteLien` directe ;
- preuve que `QuantiteLienCumulee` n'est pas utilisee pour le poids theorique U2 ;
- detection des titres et lignes techniques ;
- formats reels des decimales.

### YRJMCH

```http
POST /api/v1/rapports-machine/import?profilImport=YRJMCH&sourceSystem=X3
Content-Type: multipart/form-data
```

Points d'attention :

- aggregation par `NumeroOF + CodeArticle` ;
- total `QuantiteEMR` ;
- total `QuantiteRebutEMR` ;
- nombre de lignes agregees ;
- idempotence du second import.

## Diagnostic global

```http
GET /api/v1/diagnostics/imports-reels
```

Le diagnostic retourne :

- nombre de gammes actives ;
- nombre de nomenclatures actives ;
- nombre de rapports machine actifs ;
- 10 derniers `ImportBatches` ;
- 20 anomalies recentes.

Limite actuelle : le modele `ImportBatch` ne stocke pas encore le nom de fichier, le nombre de feuilles ni une duree explicite. La duree peut etre calculee avec `DateFinUtc - DateCreationUtc` lorsque les deux valeurs sont presentes.

## Verification contexte U2

Pour chaque OF reel choisi :

```http
GET /api/v1/receptions-usinage2/contexte/{numeroOF}?dateReception=YYYY-MM-DD
```

Controler :

- article depuis `ENCOURSPRODUCT` ;
- designation et situation X3 ;
- atelier depuis `GAMMEFAB` ;
- type matiere depuis mouvements sortie OF ;
- `QuantiteLien` directe depuis `BOMARTPARENT` ;
- poids theorique ;
- quantite EMR et rebut EMR depuis `YRJMCH` ;
- nombre de lignes `YRJMCH` agregees ;
- statut de chaque resolution ;
- avertissements.

Cas minimum a tester :

- OF complet ;
- OF introuvable ;
- article ambigu ;
- atelier introuvable ;
- plusieurs ateliers ;
- matiere introuvable ;
- nomenclature introuvable ;
- plusieurs `QuantiteLien` differentes ;
- rapport machine absent ;
- plusieurs equipes et postes pour le meme OF/article.

## Regle temporaire controle poids

Le lookup `IControlePoidsFonderieLookupService` ne choisit pas arbitrairement le dernier controle.

Comportement provisoire :

- aucun candidat : `PoidsPhysiqueManquant` ;
- candidats sans controle valide : `ControleNonValide` ;
- un seul controle valide : `Trouve` ;
- plusieurs controles valides : `Trouve`, avec la moyenne arithmetique des poids valides exploitables.

Les contrôles `Brouillon` et `Annulé` sont exclus ; un poids nul ou négatif invalide la résolution. Pour plusieurs contrôles valides applicables, aucun contrôle individuel n'est désigné comme source : la moyenne et la liste des candidats sont conservées.
