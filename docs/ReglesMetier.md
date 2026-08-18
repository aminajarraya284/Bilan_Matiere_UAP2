# Règles métier — Bilan Matière UAP2

Ce document est le référentiel unique des règles métier de l'application. Chaque règle
implémentée dans le code doit y figurer, et chaque règle décrite ici doit être traçable
vers du code et des tests.

## Comment lire ce document

Chaque règle porte un identifiant stable. Un identifiant n'est jamais réutilisé ni
renuméroté : il peut être marqué obsolète, mais reste en place.

| Préfixe | Domaine |
| --- | --- |
| `RM-COM` | Règles communes à toutes les verticales |
| `RM-SEC` | Sécurité, authentification et autorisations |
| `RM-X3` | Imports et sources Sage X3 |
| `RM-U2` | Usinage 2 : réceptions, rebuts, bilan |
| `RM-FON` | Fonderie : déchets, contrôle poids, bilan |
| `RM-INV` | Inventaire Fonderie |
| `RM-UAP2` | Bilan matière UAP2 consolidé |
| `RM-ALERT` | Alertes métier persistées |
| `RM-VAL` | Validation, clôture et workflow mensuel |

### Statut d'une règle

| Statut | Signification |
| --- | --- |
| **Confirmée** | Validée par le métier SOPAL. Toute modification exige un nouvel arbitrage. |
| **À valider** | Implémentée d'une certaine façon, mais non confirmée par le métier. Le comportement actuel est conservé tel quel ; il ne doit pas être « amélioré » par supposition. |
| **Technique** | Règle d'implémentation sans enjeu métier (normalisation, format, limite de taille). |

### Convention d'unités

L'application manipule deux unités qui ne doivent **jamais** être additionnées entre elles :

```text
Pièces (pcs)   : quantités issues de YRJMCH, réceptions, rebuts, entrées OF
Kilogrammes (kg) : matière, déchets, inventaire, stocks
```

Toute conversion pièces → kg exige un poids unitaire identifié et documenté
(voir `RM-COM-005`).

### Terminologie

| Terme retenu | Ancien terme | Remarque |
| --- | --- | --- |
| **Sortie OF** | « Sortie MP » | Le code utilise encore `SortieMp` ; renommage à planifier |
| **EMR** | « ER » | Ne jamais écrire `ER` |
| **Entrée OF** | — | Mouvement X3 distinct de la Sortie OF |

---

## RM-COM — Règles communes

### RM-COM-001 — Normalisation des clés métier

**Objectif** : garantir qu'un même OF, article ou code matière saisi avec une casse ou
des espaces différents soit reconnu comme identique.

- **Source** : toute saisie utilisateur ou cellule Excel.
- **Formule** : `valeur.Trim().ToUpperInvariant()`, `null` devient chaîne vide.
- **Cas limite** : une valeur uniquement composée d'espaces devient une chaîne vide et
  est traitée comme absente.
- **Implémentation** : `BilanMetierRules.NormaliserCle`.
- **Tests** : `NormaliserCle_TrimEtMetEnMajuscules`.
- **Statut** : **Technique**.

### RM-COM-002 — Calcul d'un taux en pourcentage

**Objectif** : produire un taux d'écart comparable entre lignes.

- **Formule** : `taux = numérateur / dénominateur × 100`.
- **Comportement en cas de donnée manquante** : si le numérateur ou le dénominateur est
  `null`, **ou si le dénominateur vaut zéro**, le résultat est `null` — jamais `0`.
- **Option** : `valeurAbsolueNumerateur` applique `Math.Abs` au numérateur, utilisé pour
  les taux d'écart où seul l'ordre de grandeur compte.
- **Implémentation** : `BilanMetierRules.CalculerTaux`.
- **Tests** : `CalculerTaux_RetourneNullSiDenominateurNul`.
- **Statut** : **Confirmée**.

### RM-COM-003 — Une donnée manquante n'est jamais un zéro

**Objectif** : ne jamais présenter un résultat calculé comme fiable alors qu'une de ses
composantes est absente.

- **Règle** : lorsqu'une composante d'un calcul est indisponible, le résultat est `null`
  et un statut explicite est renvoyé (`DonneesManquantes`, `Anomalie`…). Un agrégat dont
  une seule ligne est non calculable devient lui aussi `null`.
- **Justification** : un faux zéro dans un bilan matière se lit comme « aucune perte »,
  ce qui est l'inverse du message à transmettre.
- **Statut** : **Confirmée**.

### RM-COM-004 — Cycle de vie et suppression logique

**Objectif** : préserver la traçabilité comptable.

- **Statuts** : `Brouillon (0) → Valide (1) → Annule (2)`, partagés par les réceptions,
  les déchets, les contrôles poids et les lignes d'inventaire.
- **Règles** :
  - une ligne **validée n'est plus modifiable** ;
  - une ligne **annulée n'est plus modifiable** ;
  - l'annulation est **idempotente** : annuler deux fois ne produit pas d'erreur ;
  - la suppression est **toujours logique** : la ligne sort des calculs mais reste en base.
- **Implémentation** : entités `DechetFonderie`, `ReceptionUsinage2`,
  `InventaireConteneurOf`, `InventaireCategorie`, `InventaireFour`.
- **Tests** : `Valider_FaitPasserDeBrouillonAValide`, `Modifier_Valide_LanceDomainValidationException`,
  `Annuler_DejaAnnule_Idempotent`, `Annuler_ConserveLeDechetDansLHistorique`,
  `UneLigneAnnulee_SortDuStockMaisResteDansLHistorique`.
- **Statut** : **Confirmée**.

### RM-COM-005 — Interdiction d'additionner pièces et kilogrammes

**Objectif** : empêcher une erreur d'unité silencieuse dans les bilans.

- **Règle** : une addition ne peut porter que sur des grandeurs de même unité. Toute
  conversion pièces → kg doit expliciter :
  1. la quantité source et son unité ;
  2. le poids unitaire employé ;
  3. l'origine de ce poids unitaire ;
  4. l'unité cible.
- **Comportement en cas de donnée manquante** : sans poids unitaire identifié, la
  conversion est **refusée** et signalée, jamais approximée.
- **Statut** : **Confirmée**.

### RM-COM-006 — Paramètres applicatifs

**Objectif** : éviter les valeurs métier codées en dur.

- **Clés** définies dans `ParametresApplicationCles` :

```text
SEUIL_ECART_FONDERIE     seuil d'alerte d'écart Fonderie
SEUIL_ECART_UAP2         seuil d'alerte d'écart UAP2
CODE_ATELIER_UAP2        code de l'atelier UAP2 (« 16 » en usage courant)
TRANSACTION_ENTREE_OF    code transaction X3 des entrées OF
TYPES_MATIERE            liste des types matière retenus
```

- **Comportement en cas de donnée manquante** : l'absence d'un paramètre requis produit
  l'anomalie `PARAMETRE_APPLICATION_MANQUANT:<clé>` et **n'est pas remplacée par une
  valeur par défaut**.
- **Statut** : **Confirmée**.

### RM-COM-007 — Période de référence

- **Format** : `YYYY-MM`, contrôlé par l'expression régulière `^\d{4}-(0[1-9]|1[0-2])$`.
- **Portée** : une période correspond à un mois calendaire.
- **Statut** : **Technique**.

### RM-COM-008 — Champs texte libres

- **Observation / commentaire** : facultatif, normalisé, **500 caractères maximum**.
  Une chaîne vide est stockée comme `null`.
- **Numéro d'OF, numéro de conteneur, code four** : 50 caractères maximum.
- **Code type matière** : 30 caractères maximum.
- **Statut** : **Technique**.

---

## RM-SEC — Sécurité et autorisations

### RM-SEC-001 — Rôles applicatifs

Trois rôles, sans hiérarchie implicite :

```text
Admin                  accès complet
ResponsableFonderie    déchets, contrôle poids, inventaire Fonderie
ResponsableUsinage2    réceptions, rebuts, bilan Usinage 2
```

- **Statut** : **Confirmée**.

### RM-SEC-002 — Double contrôle d'accès

- **Règle** : l'autorisation est vérifiée **côté serveur** par une policy ASP.NET Core.
  La garde de navigation frontend est un confort d'ergonomie, jamais une protection.
- **Conséquence** : contourner le routeur aboutit à un `403`, pas à un accès.
- **Statut** : **Confirmée**.

### RM-SEC-003 — Interdiction de démarrer sans authentification réelle

- **Règle** : hors environnement `Development`, l'application **refuse de démarrer** tant
  qu'une authentification de production (JWT / OIDC Entra ID) n'est pas configurée.
- **Configuration complète exigée** : `Authentication:Jwt:Authority` **et**
  `Authentication:Jwt:Audience`. Une autorité sans audience est refusée — elle laisserait
  passer les jetons émis pour une autre application du même tenant.
- **Non contournable** : forcer `Authentication:DevelopmentMode=true` hors `Development`
  ne réactive pas l'en-tête `X-Development-Role` ; le démarrage échoue quand même.
- **Implémentation** : `Program.cs`, `EntraIdAuthenticationOptions`,
  `EntraIdRoleMappingTransformation`. Voir [`AuthentificationEntraId.md`](AuthentificationEntraId.md).
- **Tests** : `ProductionAuthenticationGuardTests`, `EntraIdAuthenticationTests`.
- **Reste à faire** : le **frontend** n'acquiert pas encore de jeton Entra ID (MSAL) ; il
  utilise le flux de connexion de développement.
- **Statut** : **Confirmée**. Ce garde-fou ne doit jamais être affaibli.

### RM-SEC-004 — Mode d'authentification de développement

- **Règle** : le schéma `Development` (jeton local, en-tête `X-Development-Role`) n'est
  actif que si l'environnement est `Development` **et** `Authentication:DevelopmentMode`
  vaut `true`. La condition est vérifiée deux fois : à l'enregistrement du service et à
  chaque requête.
- **Clé de signature** : aléatoire à chaque démarrage, sauf surcharge par User Secrets.
  Aucun secret n'est stocké dans le dépôt.
- **Tests** : `DevelopmentAuthenticationTests`, `DevelopmentTokenServiceTests`.
- **Statut** : **Confirmée**.

---

## RM-X3 — Imports et sources Sage X3

### RM-X3-001 — Sources exploitées

| Source | Contenu | Usage principal |
| --- | --- | --- |
| `GAMMEFAB` | Gammes de fabrication | Atelier d'origine, éligibilité UAP2 |
| `BOMARTPARENT` | Nomenclatures | Type matière (repli), poids théorique |
| `YRJMCH` | Rapports machine | Quantités contrôlées et rebutées |
| `MVTSTOTRSVCR` | Mouvements de stock | Entrées OF et Sorties OF |
| `ENCOURSPRODUCT` | OF en cours | Article principal, situation OF |

- **Statut** : **Confirmée**.

### RM-X3-002 — Lecture par nom de colonne

**Objectif** : résister à un déplacement de colonnes dans les exports X3.

- **Règle** : les lecteurs Excel construisent une table de correspondance à partir de la
  ligne d'en-tête. Le mapping **positionnel** n'est utilisé qu'en dernier recours,
  lorsqu'aucun en-tête exploitable n'est trouvé.
- **Feuilles de continuation** : un onglet sans en-tête réutilise la correspondance du
  précédent.
- **Conséquence** : les correspondances par lettre de colonne mentionnées ci-dessous sont
  **indicatives** (elles décrivent les exports observés) et ne doivent pas être
  transformées en mapping positionnel figé.
- **Implémentation** : `ExcelWorkbook.CreateHeaderMap`, `RapportMachineExcelReader`,
  `X3ExcelReaders`, `GammeFabricationExcelReader`, `NomenclatureExcelReader`.
- **Statut** : **Technique**.

### RM-X3-003 — Classification du processus par préfixe de poste

**Objectif** : remplacer un référentiel manuel de 295 codes postes par une règle stable.

- **Source** : YRJMCH, code poste (colonne B dans les exports observés).
- **Formule** : seuls les **trois premiers caractères** du code poste sont significatifs.

```text
CTRL   (valeur exacte)  → CTRL
SCIxxx                  → SCI
EMRxxx                  → EMR
TRNxxx                  → TRN
tout autre préfixe      → Inconnue
```

- **Cas limite** : un code de moins de 3 caractères, vide ou `null` donne `Inconnue`.
- **Préfixes détectés non validés** : `NOY`, `GRN`, `COQ`, `CBP` sont reconnus mais
  **exclus des agrégats** en attente d'arbitrage métier. Les lignes concernées sont
  comptées et signalées (`NombreLignesNonMappees`), jamais silencieusement ignorées.
- **Implémentation** : `OrigineProcessusRules`.
- **Tests** : `OrigineProcessusRulesTests`, `ClassifierPoste_DelegueLaReglePureConfirmee`.
- **Statut** : **Confirmée**, sauf le rattachement des préfixes `NOY`/`GRN`/`COQ`/`CBP`
  qui reste **À valider**.

### RM-X3-004 — Quantité EMR nette

- **Source** : YRJMCH.
- **Colonnes observées** : `B` poste, `H` quantité totale contrôlée, `I` quantité rebut.
- **Formule** :

```text
QuantiteEMRNette = QuantiteTotaleControlee - QuantiteRebut
```

- **Unité** : pièces.
- **Cas limite** : une quantité totale ou rebut illisible rend la ligne invalide et
  génère une anomalie d'import, sans interrompre le lot.
- **Statut** : **Confirmée**.

### RM-X3-005 — Sens des quantités de mouvement de stock

- **Règle** : une quantité X3 signée est convertie en quantité métier par inversion du
  signe (`ToQuantiteMetier(q) = -q`).
- **Justification** : les sorties sont négatives dans X3 et positives en lecture métier.
- **Implémentation** : `BilanMetierRules.ToQuantiteMetier`.
- **Tests** : `ToQuantiteMetier_ConvertitLeSigneSortieMpEnUnSeulPoint`.
- **Statut** : **Confirmée**.

### RM-X3-006 — Distinction Entrée OF / Sortie OF

- **Source** : `MVTSTOTRSVCR`.
- **Règles** :
  - **Entrée OF** : alimente le terme `EntreeOF` du bilan Usinage 2 et du bilan UAP2 ;
  - **Sortie OF** : sert à résoudre le contexte OF et le **type matière** (voir
    `RM-U2-002`).
- **Statut** : **Confirmée**.

### RM-X3-007 — Réimport et idempotence

**Objectif** : permettre de rejouer un export X3 sans dupliquer ni perdre de données.

- **Règles** :
  1. tout fichier X3 concerné doit pouvoir être réimporté ;
  2. les nouvelles lignes sont ajoutées ;
  3. les lignes modifiées sont mises à jour ;
  4. les **doublons exacts sont ignorés** ;
  5. l'historique des lots est conservé ;
  6. l'archive du fichier source est conservée et téléchargeable ;
  7. les données dépendantes sont recalculées si nécessaire ;
  8. l'opération est **idempotente** : réimporter deux fois le même fichier laisse la
     base dans le même état.
- **Clé de détection** : empreinte `SHA-256` du fichier (`ImportBatch.HashFichierSha256`).
- **Compteurs** : chaque lot persiste des compteurs cohérents (lignes lues, ajoutées,
  mises à jour, ignorées, en anomalie) ; une équation incohérente est refusée.
- **Tests** : `TerminerImport_PersisteLesCompteursCoherents`,
  `TerminerImport_RefuseDesEquationsIncoherentes`,
  `SignalerErreurArchive_ConserveDuplicateMaisStockeErreur`, `RealFileImportTests`.
- **Statut** : **Confirmée**.

### RM-X3-008 — Clé de rapprochement technique

- **Règle** : les jointures techniques entre sources utilisent `NumeroOF + CodeArticle`.
- **Nuance** : le **regroupement métier** du bilan Usinage 2 reste le **Numéro OF seul**
  (voir `RM-U2-005`). La clé technique ne doit pas se substituer au regroupement métier.
- **Statut** : **Confirmée**.

---

## RM-U2 — Usinage 2

### RM-U2-001 — Résolution du contexte d'un OF

**Objectif** : reconstituer article, atelier et type matière à partir du seul numéro d'OF.

- **Sources croisées** : Sorties OF, `ENCOURSPRODUCT`, YRJMCH, Entrées OF, `GAMMEFAB`.
- **Principe directeur** : **un candidat unique donne une valeur ; zéro ou plusieurs
  candidats donnent `null` et une anomalie**. Le système ne tranche jamais lui-même.
- **Codes d'anomalie émis** :

```text
OF_X3_INTROUVABLE                       aucune trace de l'OF dans les quatre sources
ARTICLE_OF_INTROUVABLE                  aucun article candidat
PLUSIEURS_ARTICLES_POUR_OF              article ambigu
ATELIER_UAP2_INTROUVABLE                aucune gamme trouvée
HORS_ATELIER_UAP2                       gammes trouvées, mais hors atelier UAP2
ATELIER_UAP2_AMBIGU                     plusieurs groupes de postes de charge
TYPE_MATIERE_SORTIE_MP_AMBIGUE          plusieurs types matière en Sortie OF
TYPE_MATIERE_AMBIGUE                    plusieurs types matière en nomenclature
TYPE_MATIERE_INTROUVABLE                aucun type matière résolu
REFERENTIEL_TYPES_MATIERE_ACTIFS_VIDE   aucun type matière actif en base
PARAMETRE_APPLICATION_MANQUANT:<clé>    paramètre requis absent
```

- **Implémentation** : `OfContextResolver`.
- **Tests** : `OfContextResolverTests`.
- **Statut** : **Confirmée**.

### RM-U2-002 — Résolution du type matière

- **Priorité 1 — Sortie OF** : si les mouvements Sortie OF de l'OF donnent **exactement
  un** code type matière distinct, celui-ci est retenu (`SourceTypeMatiere.SortieMp`).
  Plusieurs codes distincts → anomalie `TYPE_MATIERE_SORTIE_MP_AMBIGUE`, aucun choix.
- **Priorité 2 — Nomenclature (repli)** : à défaut, chaque type matière **actif** est
  testé comme composant de l'article. Si **exactement un** correspond, il est retenu
  (`SourceTypeMatiere.Nomenclature`). Plusieurs → `TYPE_MATIERE_AMBIGUE`. Aucun →
  `TYPE_MATIERE_INTROUVABLE`.
- **Cas limite** : référentiel de types matière actifs vide →
  `REFERENTIEL_TYPES_MATIERE_ACTIFS_VIDE`.
- **Note d'implémentation** : le repli interroge la base une fois par type matière
  candidat. Sans incidence à trois types, mais à optimiser en requête unique si le
  référentiel s'étend.
- **Statut** : **Confirmée** pour la priorité 1. Le repli nomenclature est **Confirmée**
  en tant que mécanisme existant, documenté ici sans extension.

### RM-U2-003 — Atelier d'origine et éligibilité UAP2

- **Source** : `GAMMEFAB`.
- **Règle observée historiquement** : ligne où `NumeroOperation = 5`, valeur `Atelier`
  (colonnes `E` et `F` des exports observés — voir la réserve de `RM-X3-002`).
- **Éligibilité UAP2** : un OF est éligible dès qu'**au moins une** ligne `GAMMEFAB` porte
  le code atelier `CODE_ATELIER_UAP2`.
- **Ambiguïté** : plusieurs groupes de postes de charge laissent le **groupe** non résolu
  (`ATELIER_UAP2_AMBIGU`) mais **ne retirent pas l'éligibilité**.
- **Absence** : `ATELIER_ORIGINE_INTROUVABLE` / `ATELIER_UAP2_INTROUVABLE`.
- **Statut** : **Confirmée**.

### RM-U2-004 — Saisie d'une réception Usinage 2

- **Champs saisis** : `NumeroOF`, `DateReception`, `NombrePiecesReceptionnees`,
  `PoidsTotalReceptionneKg`, origine, observations.
- **Champs calculés** : article, type matière, atelier d'origine, poids physique
  unitaire, poids théorique unitaire, écart de poids, taux d'écart.
- **Validations** :

```text
NumeroOF                     obligatoire, 50 caractères max
DateReception                obligatoire
NombrePiecesReceptionnees    strictement > 0
PoidsTotalReceptionneKg      obligatoire, strictement > 0
Observation                  500 caractères max
```

- **Implémentation** : `CreateReceptionUsinage2RequestValidator`,
  `UpdateReceptionUsinage2RequestValidator`.
- **Tests** : `ReceptionUsinage2Tests`, `ReceptionsUsinage2ApiTests`.
- **Statut** : **Confirmée**.

### RM-U2-005 — Population et regroupement du Bilan Usinage 2

**Objectif** : garantir qu'aucune ligne de bilan n'apparaisse sans réception réelle.

- **Règle de population** : la population du Bilan Usinage 2 est construite
  **uniquement à partir des Réceptions Usinage 2**.
- **Sources d'enrichissement** (ne créent **jamais** une ligne à elles seules) :
  Entrées OF, Rebuts U2, `GAMMEFAB`, `ENCOURSPRODUCT`, Sorties OF, `YRJMCH`.
- **Regroupement métier** : **Numéro OF**.
- **Statut** : **Confirmée**.

### RM-U2-006 — Réceptions éligibles aux calculs

- **Règle** : seules les réceptions **validées** entrent dans les totaux du bilan.
- **Filtre atelier** : pour les calculs concernés, seules les réceptions validées dont
  l'**atelier d'origine est `CODE_ATELIER_UAP2`** (« 16 ») sont retenues.
- **Périmètre temporel** : les réceptions sont agrégées **par OF, article, année et mois**
  de la date de réception.
- **Conséquence** : les réceptions en brouillon, annulées ou hors atelier restent
  **consultables** mais sont **exclues des calculs**.
- **Statut** : **Confirmée**.

### RM-U2-007 — Formule du Bilan Usinage 2

- **Unité** : pièces.
- **Formule** :

```text
BilanUsinage2Pieces = QuantiteReceptionneePieces
                    - RebutUsinage2Pieces
                    - EntreeOFPieces
```

- **Comportement en cas de donnée manquante** : si `EntreeOFPieces` est indisponible, le
  bilan de la ligne est `null` (application de `RM-COM-003`). Un agrégat contenant une
  seule ligne non calculable est lui-même `null`.
- **Implémentation** : `BilanMetierRules.CalculerBilanUsinage2`, `BilanUsinage2Service`.
- **Tests** : `CalculerBilanUsinage2_AppliqueLaFormuleEnPieces`, `BilanUsinage2ServiceTests`,
  `BilansEtDiagnosticsApiTests`.
- **Statut** : **Confirmée**.

### RM-U2-008 — Quantité non réceptionnée et statut

- **Formule** :

```text
QuantiteNonReceptionnee = QuantiteEMRNette - TotalReceptionsU2Validees
                        = (TotalCtrlEMR - TotalRebutEMR) - PiecesRecuesU2
```

- **Unité** : pièces.
- **Statuts** :

```text
null → DonneesManquantes
0    → OK
> 0  → AVerifier
< 0  → Anomalie
```

- **Justification du statut négatif** : recevoir plus de pièces que contrôlées est
  incohérent et signale une erreur de saisie ou de rapprochement.
- **Implémentation** : `BilanMetierRules.DeterminerStatutNonReception`,
  `ReceptionUsinage2ContextService`.
- **Tests** : `AlerteNonReceptionServiceTests`.
- **Statut** : **Confirmée**.

### RM-U2-009 — Poids physique unitaire

**Objectif** : ne pas déduire un poids unitaire d'une division approximative.

- **Règle** : le poids physique unitaire provient **exclusivement du module Contrôle
  poids Fonderie**.
- **Interdiction explicite** :

```text
INTERDIT : PoidsPhysiqueUnitaire = PoidsTotalReceptionne / NombrePieces
```

- **Sélection du contrôle** — comportement actuel :

| Situation | Statut renvoyé | Poids |
| --- | --- | --- |
| Type matière introuvable | `Introuvable` | `null` |
| Aucun contrôle candidat | `PoidsPhysiqueManquant` | `null` |
| Aucun candidat validé | `ControleNonValide` | `null` |
| Un candidat validé à poids ≤ 0 | `ValeurInvalide` | `null` |
| **Plusieurs candidats validés** | `Ambigu` | `null` |
| Exactement un candidat validé | `Trouve` | poids retenu |

- **Point d'arbitrage** : en cas de pluralité, le système **refuse de choisir**. La règle
  SOPAL doit trancher entre : même OF, même lot ou série, dernier contrôle validé avant
  la date de réception, contrôle validé article + matière, ou moyenne des contrôles.
- **Implémentation** : `ControlePoidsFonderieLookupService` (marqueur `TODO_METIER`).
- **Tests** : `ControlePoidsFonderieLookupServiceTests`.
- **Statut** : **À valider** — conserver le comportement actuel jusqu'à arbitrage.

### RM-U2-010 — Poids théorique unitaire et écart de poids

**Objectif** : comparer le poids réel au poids attendu par la nomenclature.

- **Source visée** : `BOMARTPARENT`, valeur `QuantiteLien`.
- **État actuel** : **la chaîne n'est pas alimentée**. `poidsTheoriqueUnitaireKg` reste
  systématiquement `null` et l'avertissement suivant est émis à chaque appel :

```text
Poids théorique indisponible — règle métier à valider
```

- **Statut de nomenclature** : même lorsqu'une `QuantiteLien` unique et une unité
  cohérente sont trouvées, le statut renvoyé est `RegleMetierAValider` et **jamais**
  `Trouve`. Les autres statuts possibles sont `NomenclatureIntrouvable`,
  `NomenclatureAmbigue` et `UniteIncompatible`.
- **Conséquence en chaîne** : `EcartPoidsUnitaireKg`, `TauxEcartPoidsPourcent` et
  `StatutPoids` restent indéterminés. **Toute la logique d'alerte sur écart de poids est
  implémentée et testée, mais inerte.**
- **Interdiction** : ne pas cumuler arbitrairement plusieurs lignes de nomenclature et ne
  pas inventer de facteur de conversion.
- **Décision attendue de SOPAL** : confirmer la colonne de nomenclature à utiliser, son
  unité, et la règle de conversion vers un poids unitaire en kg.
- **Statut** : **À valider** — bloquant fonctionnel.

### RM-U2-011 — Seuil d'alerte sur écart de poids

- **Règle envisagée** : alerte au-delà de 3 % d'écart.
- **Point d'arbitrage** : `> 3 %` ou `>= 3 %` non tranché. Traitement des écarts négatifs
  non tranché.
- **Dépendance** : sans `RM-U2-010`, cette règle ne peut pas s'appliquer.
- **Statut** : **À valider**.

---

## RM-FON — Fonderie

### RM-FON-001 — Déclaration de déchet

- **Règles** :
  - une déclaration référence obligatoirement un **type matière actif** ;
  - le référentiel initialisé contient `M3000L0`, `M3010L0`, `M3060L0` ;
  - la **date de déclaration est obligatoire** et détermine automatiquement la semaine ISO ;
  - le type de déchet est `Bavure` ou `Poudre` ;
  - le **poids est strictement supérieur à zéro** ;
  - l'observation est facultative, normalisée, 500 caractères maximum ;
  - un déchet **annulé n'est plus modifiable** ;
  - la suppression est logique (`RM-COM-004`).
- **Tests** : `Creer_AvecDonneesValides_InitialiseLeDechet`, `Creer_CalculeLaSemaineIso`,
  `Creer_RefuseUnPoidsNonPositif`, `Modifier_MetAJourLesValeursEtLaSemaine`,
  `Annuler_ConserveLeDechetDansLHistorique`, `Annuler_EstIdempotent`.
- **Statut** : **Confirmée**.

### RM-FON-002 — Liste des types matière

- **État** : trois codes initialisés par migration (`M3000L0`, `M3010L0`, `M3060L0`).
- **Point d'arbitrage** : la liste officielle et complète des types matière doit être
  confirmée avant mise en production ou extension du référentiel.
- **Statut** : **À valider**.

### RM-FON-003 — Formule du Bilan Fonderie

- **Unité** : kilogrammes. Les unités doivent être homogènes **avant** toute addition.
- **Formule** :

```text
INPUT  = StockDebut + SortiesMagasinX3
OUTPUT = StockFin + DechetsFonderie + ReceptionsUsinage2
ECART  = INPUT - OUTPUT
```

- **Exclusion** : l'**inventaire du four est exclu du bilan** (voir `RM-INV-004`).
- **Origine du stock de début** : voir `RM-INV-005`.
- **Comportement en cas de donnée manquante** : une composante absente ne devient pas
  zéro ; le bilan est marqué en anomalie ou en donnée manquante (`RM-COM-003`).
- **État d'implémentation** : implémentée côté serveur, `GET /api/v1/bilans/fonderie/{periode}`
  (une ligne par type matière actif). Le frontend consomme cet endpoint
  (`VITE_BILAN_FONDERIE_SOURCE=api`) ; le calcul frontend sur données de démonstration
  n'est plus utilisé qu'en mode `mock`.
- **Origine de chaque composante** :

| Composante | Source persistée | Unité |
| --- | --- | --- |
| `StockDebut` / `StockFin` | projection `InventaireFonderie` (alimentée par le comptage) | kg |
| `SortiesMagasinX3` | `ConsommationsComposants` rapprochées | **unités mixtes** |
| `DechetsFonderie` | `DechetsFonderie` validés | kg |
| `ReceptionsUsinage2` | `ReceptionsUsinage2` validées, `PoidsTotalReceptionneKg` | kg si renseigné |

- **Limite d'unité, à arbitrer** : les consommations X3 arrivent en `UN, KG, BA, M, LI`.
  Seules les lignes en **KG** sont sommées ; les autres sont **exclues et signalées**
  (`SORTIES_X3_UNITES_NON_CONVERTIBLES:<unités>`), aucune règle de conversion n'étant
  confirmée. De même, les réceptions sans poids
  (`RECEPTIONS_U2_POIDS_MANQUANT:<n>`) ou sans type matière résolu
  (`RECEPTIONS_U2_TYPE_MATIERE_NON_RESOLU:<n>`) sont comptées et signalées, jamais
  assimilées à zéro.
- **Conséquence** : tant que ces conversions ne sont pas tranchées par SOPAL, un écart
  affiché sur une ligne portant ces anomalies **n'est pas exhaustif**. La ligne est
  marquée `Incomplet` et le total de la période devient `null` dès qu'une ligne l'est.
- **Tests** : `BilanFonderieServiceTests` (7 unitaires), `BilanFonderieApiTests` (5 HTTP).
- **Statut** : **Confirmée** quant à la formule ; **implémentée** côté serveur. La règle
  de conversion des unités non-KG reste **À valider**.

---

## RM-INV — Inventaire Fonderie

### RM-INV-001 — Trois natures de comptage

L'inventaire Fonderie distingue trois natures de lignes :

```text
Conteneurs / OF   comptage par conteneur rattaché à un OF
Catégories        Rebut, MpLingot, Autre
Fours             comptage par four
```

- **Statut** : **Confirmée**.

### RM-INV-002 — Validations de saisie

| Champ | Règle |
| --- | --- |
| `DateInventaire` | obligatoire, date par défaut refusée |
| `NumeroConteneur` | obligatoire, 50 caractères max |
| `NumeroOF` | obligatoire (lignes conteneur), 50 caractères max |
| `CodeFour` | obligatoire (lignes four), 50 caractères max |
| `Categorie` | doit appartenir à l'énumération |
| `CodeTypeMatiere` | obligatoire, 30 caractères max |
| `PoidsReelKg` / `QuantiteKg` | **≥ 0** — un poids nul est autorisé, un poids négatif est refusé |
| `Observation` / `Commentaire` | 500 caractères max |

- **Différence notable avec les déchets** : l'inventaire **autorise un poids de zéro**
  (`Creer_PoidsZero_EstAutorise`), là où un déchet exige un poids strictement positif.
  Un comptage à zéro est une information ; un déchet à zéro n'en est pas une.
- **Tests** : `InventaireConteneurOfTests`, `InventaireCategorieTests`, `InventaireFourTests`.
- **Statut** : **Confirmée**.

### RM-INV-003 — Lignes retenues dans le stock

- **Règles** :
  - seules les lignes **validées** alimentent le stock ;
  - une ligne **non validée n'alimente pas** le stock ;
  - une ligne **annulée sort du stock** mais **reste dans l'historique** ;
  - une ligne **validée n'est plus modifiable**.
- **Tests** : `LesLignesNonValidees_NAlimententPasLeStock`,
  `UneLigneAnnulee_SortDuStockMaisResteDansLHistorique`,
  `UneLigneValidee_NEstPlusModifiable`.
- **Statut** : **Confirmée**.

### RM-INV-004 — Composition du stock de fin et exclusion des fours

- **Formule** :

```text
StockFin = Σ Conteneurs validés + Σ Catégories validées
```

- **Règle d'exclusion** : les **fours sont exclus du stock de fin** — donc du Bilan
  Fonderie — mais **restent présents dans le rapport d'inventaire**.
- **Séparation** : les totaux sont calculés **par type matière**, jamais confondus.
- **Tests** : `StockFin_EstLaSommeDesConteneursEtDesCategories`,
  `LesFours_SontExclusDuStockFinMaisPresentsDansLeRapport`,
  `LesTotaux_SontSeparesParTypeMatiere`.
- **Statut** : **Confirmée**.

### RM-INV-005 — Chaînage des périodes

- **Règle** :

```text
StockDebut(période N) = StockFin(période N-1)
```

- **Portée** : ce chaînage est l'entrée `StockDebut` du Bilan Fonderie (`RM-FON-003`).
- **Cas limite** : en l'absence de période précédente clôturée, le stock de début doit
  être signalé comme donnée manquante et non forcé à zéro.
- **Tests** : `StockDebut_EstLeStockFinDeLaPeriodePrecedente`.
- **Statut** : **Confirmée**.

### RM-INV-006 — Statuts d'inventaire

- **Ligne d'inventaire** : `Brouillon → Valide → Annule` (`RM-COM-004`).
- **Période d'inventaire** : statut propre porté par `InventaireFonderiePeriode`.
- **Validation** : valider sans utilisateur identifié est refusé
  (`Valider_SansUtilisateur_LanceDomainValidationException`).
- **Aucune suppression physique** : l'API n'expose pas de `DELETE`. Retirer une ligne du
  stock passe par l'annulation, qui la conserve consultable dans la période. L'écran
  applique cette règle (bouton « Annuler la ligne », jamais « Supprimer »).
- **Statut** : **Confirmée**.

### RM-INV-007 — Deux modèles de persistance coexistent

- **Constat** : le contrôleur `InventairesFonderieController` expose **deux jeux
  d'entités distincts pour le même besoin métier** :

| | Modèle **Comptage** | Modèle **CRUD riche** |
| --- | --- | --- |
| Entités | `InventaireConteneur`, `InventaireCategorieComptage`, `InventaireFourComptage` | `InventaireConteneurOf`, `InventaireCategorie`, `InventaireFour` |
| Routes | `comptage/conteneurs`, `comptage/categories`, `comptage/fours` | `conteneurs`, `categories`, `fours` (+ recherche, pagination, `synthese`) |
| Projection `InventaireFonderie` | Recalculée par transaction à chaque écriture | Non recalculée |
| Consommé par le frontend | **Oui** — chemin unique de `inventaire.service.ts` | **Non — aucun appel** |

- **Modèle de référence** : le modèle **Comptage** est le seul en service. Il porte le
  chaînage `RM-INV-005` et les anomalies affichées, parce qu'il est le seul à recalculer
  la projection `InventaireFonderie` dont dépend le Bilan Fonderie (`RM-FON-003`).
- **Modèle CRUD riche** : couvert par des tests unitaires mais **sans consommateur**. Il
  n'est ni retiré ni étendu tant que SOPAL n'a pas tranché entre « piste abandonnée » et
  « écran de recherche/synthèse à venir ». Ne pas y investir de nouveaux tests
  d'intégration avant cet arbitrage.
- **Risque en cas d'oubli** : écrire par les deux chemins sur une même période produirait
  deux vérités de stock, dont une seule alimenterait le bilan.
- **Statut** : **Constat** — arbitrage **À valider** par SOPAL.

---

## RM-UAP2 — Bilan matière UAP2

### RM-UAP2-001 — Partie 1 : sortie matière

- **Source de la règle** : note manuscrite SOPAL (photos), transmise le 2026-08-04.
- **Point clé** : **`CTRL` est la quantité de référence**, pas un quatrième processus.
  Le code poste YRJMCH `CTRL` (valeur exacte) est classé à part de `SCIxxx`, `EMRxxx` et
  `TRNxxx` par `OrigineProcessusRules` — règle déjà confirmée par SOPAL. Les quantités de
  SCI/EMR/TRN ne s'ajoutent donc **pas** à CTRL.
- **Formule** :

```text
QuantiteSortieMpPieces = Σ QuantiteControlee(H) des lignes de poste CTRL
                       - Σ Rebuts(I) des processus SCI, EMR et TRN

SortieMpKg = QuantiteSortieMpPieces × PoidsPhysiqueUnitaireKg
```

- **Colonnes source** : `H` = `X3RapportMachine.QuantiteTotale`, `I` = `QuantiteRebut`.
- **Poids physique** : issu du module **Contrôle poids Fonderie** (`RM-U2-009`).
- **Unités** : la quantité nette est en **pièces**, le résultat en **kilogrammes**
  (`RM-COM-005`). Sans poids unitaire résolu, `SortieMpKg` est `null` et l'anomalie
  `POIDS_PHYSIQUE_NON_RESOLU` est listée — la conversion n'est jamais approximée.
- **Restitution** : `OF`, `Article`, `Désignation`, `Total CTRL pièces`, `Poids physique`,
  `Résultat kg`.
- **Tests** : `BilanUap2ServiceTests`.
- **Statut** : **Confirmée** ; **implémentée** côté serveur.

### RM-UAP2-002 — Partie 2 : écart UAP2

- **Unité** : **PIÈCES**, intégralement. Aucune conversion n'intervient dans cette
  partie, donc aucun risque d'erreur d'unité.
- **Formule** :

```text
EcartUap2Pieces = QuantiteCtrl
                - ( ResultatSci + ResultatEmr + ResultatTrn
                  + EntreeOF
                  + RebutU2 )

TauxEcartPourcent = EcartUap2Pieces / QuantiteCtrl × 100
```

- **Seuil** : `|TauxEcart| ≤ 1,5 %` acceptable, au-delà avertissement. La valeur provient
  du paramètre `SEUIL_ECART_UAP2` (`RM-COM-006`) ; en son absence, repli sur 1,5 % avec
  l'anomalie `PARAMETRE_APPLICATION_MANQUANT:SEUIL_ECART_UAP2`.
- **Signe** : jamais de `Math.Abs` sur l'écart lui-même — un manquant et un excédent ne
  se confondent pas. La valeur absolue ne sert qu'à comparer le taux au seuil.
- **Point ouvert** : le **signe du Rebut U2** reste à confirmer. L'implémentation suit la
  lecture littérale de la note (rebut U2 **dans** la parenthèse soustraite, comme
  l'Entrée OF), ce qui est aussi la lecture physiquement cohérente : c'est une sortie
  comptabilisée. Marqué `[A CONFIRMER]` dans `BilanUap2Service`.
- **Correction apportée** : la version antérieure de ce document mentionnait
  `ReceptionsU2Kg` et `EntreesSemiFiniesKg`, et exprimait l'écart en kilogrammes. Ces
  trois éléments sont **erronés** : ils ne figurent pas dans la note SOPAL. En
  particulier, aucune donnée « entrées semi-finies » n'existe dans l'application.
- **Tests** : `BilanUap2ServiceTests`, `BilanUap2ApiTests`.
- **Statut** : **Confirmée** quant à la formule ; **implémentée** côté serveur. Le signe
  du Rebut U2 est **À valider**.

### RM-UAP2-003 — Restitution attendue

Le résultat doit rester explicable par OF et article, et exposer au minimum :
`NumeroOF`, `CodeArticle`, `TypeMatiere`, `PoidsPhysiqueUnitaireKg`, quantités nettes
`SCI`/`EMR`/`TRN`, `SortieMatiereKg`, `EntreeOF`, `ReceptionsU2Kg`, `RebutU2Kg`,
`EntreesSemiFiniesKg`, `EcartOFKg`, statut, liste des données manquantes, liste des
anomalies de conversion.

- **Statuts** : réutiliser les valeurs existantes `OK`, `AVerifier`, `Anomalie`,
  `DonneesManquantes`. **Ne pas créer de nouvel énuméré** si un équivalent existe.
- **Interdiction** : ne pas masquer les données manquantes.
- **Statut** : **Confirmée**.

### RM-UAP2-004 — Historique : blocages levés le 2026-08-04

Trois points bloquaient l'implémentation serveur. La note manuscrite SOPAL les a levés,
en corrigeant au passage la formule documentée.

| # | Blocage | Résolution |
| --- | --- | --- |
| B1 | Référence CTRL ambiguë | `CTRL` est la **quantité de référence** (code poste exact), pas un quatrième processus sommé |
| B2 | Unité d'`EntreeOF` (pièces ou kg) | Sans objet : la partie 2 est **entièrement en pièces**, `EntreeOF` y reste en pièces |
| B3 | `EntreesSemiFiniesKg` sans source | Sans objet : ce terme **ne fait pas partie** de la règle réelle |

- **Reste ouvert** : le signe du Rebut U2 (`RM-UAP2-002`).
- **Statut** : **Résolue**.

--- | --- | --- | --- |
| B1 | **Référence CTRL** — `RM-UAP2-001` somme `SCI + EMR + TRN`, mais `TODO_METIER_UAP2_CTRL_REFERENCE` demande laquelle est *la* référence | Contradiction interne | Sommer les trois ou n'en retenir qu'une donne des résultats sans commune mesure |
| B2 | **Unité d'`EntreeOF`** — `EntreeOfComposant.Unite` vaut `"UN"` (pièces) alors que `RM-UAP2-002` attend des kg | Unité | Additionner pièces et kg viole `RM-COM-005` ; convertir exige `PoidsPhysiqueUnitaireKg`, souvent non résolu (`RM-U2-009`) |
| B3 | **`EntreesSemiFiniesKg`** — terme de la formule `RM-UAP2-002` | **Donnée inexistante** | Aucune entité, aucun import, aucun champ ne la porte : la retenir à zéro fausserait tout écart |

- **Décision retenue** : ne pas implémenter. Le service frontend lève une erreur
  explicite si la verticale est passée en `api` (`bilan.service.ts`), plutôt que
  d'appeler des routes inexistantes. La verticale reste en `mock`, où le calcul de
  démonstration est assumé comme tel.
- **Précédent** : un écart matière faux mais d'apparence crédible est plus nuisible
  qu'une fonctionnalité absente — même principe que le mode `disabled` de la
  synchronisation X3.
- **Statut** : **Bloquée** — B1, B2 et B3 **À valider** par SOPAL.

---

## RM-ALERT — Alertes métier persistées

### RM-ALERT-001 — Nature d'une alerte

- **Principe** : une alerte **persiste une anomalie déjà calculée par un bilan**. Elle ne
  crée aucune règle nouvelle : elle transforme une anomalie transitoire (recalculée à
  chaque appel) en enregistrement traçable, acquittable et historisé.
- **Correspondance** : les codes d'alerte reprennent les anomalies des bilans, par exemple
  `FON_SORTIE_X3_UNITE_NON_CONVERTIBLE` pour l'anomalie homonyme de `RM-FON-003`, ou
  `UAP2_CTRL_MANQUANT` pour celle de `RM-UAP2-001`.
- **Consequence** : modifier un seuil ou une formule de bilan modifie mecaniquement les
  alertes produites. Les deux ne doivent jamais diverger.
- **Statut** : **Technique**.

### RM-ALERT-002 — Cycle de vie

- **Statuts** : `Ouverte → Acquittée → Résolue`, ou `Ouverte → Ignorée`.
- **Regles** :
  - seule une alerte **ouverte** peut être acquittée ;
  - une alerte **ignorée** ne peut plus être résolue, et une alerte **résolue** ne peut
    plus être ignorée ;
  - **ignorer exige une justification** écrite ; acquitter et résoudre acceptent un
    commentaire facultatif ;
  - une date de traitement **ne peut pas précéder la détection** ;
  - toutes les dates sont en **UTC**, refusé sinon.
- **Idempotence** : réacquitter une alerte déjà acquittée est sans effet plutôt qu'une
  erreur, ce qui rend l'action rejouable.
- **Tests** : `AlerteMetierTests`.
- **Statut** : **Technique**.

### RM-ALERT-003 — Déduplication

- **Clé** : concaténation de `Code | Periode | NumeroOF | CodeArticle | TypeMatiere | Source`,
  normalisée en majuscules, `<NULL>` pour une composante absente, séparateur `|` échappé.
- **Effet** : un recalcul de la même période **ne duplique pas** une alerte déjà ouverte ;
  il met à jour sa sévérité, son libellé et sa date de dernière détection.
- **Portée** : la période fait partie de la clé, donc une même anomalie sur deux mois
  distincts produit bien deux alertes.
- **Statut** : **Technique**.

### RM-ALERT-004 — Résolution automatique

- **Règle** : une alerte marquée `ResolutionAutomatique` et **non redétectée** lors du
  recalcul de la **même période** est résolue par `SYSTEME`, avec le commentaire
  « Condition non détectée lors du recalcul de la même période. ».
- **Portée** : la résolution automatique n'agit **que** sur la période recalculée. Une
  alerte d'une autre période n'est jamais fermée par ce mécanisme.
- **Non couvert** : les alertes non marquées `ResolutionAutomatique` restent ouvertes
  jusqu'à traitement humain, même si la condition disparaît.
- **Statut** : **Technique**.

### RM-ALERT-005 — Points d'arbitrage ouverts

- **A1 — Sévérité par code** : l'affectation d'une sévérité (`Information`, `Avertissement`,
  `Erreur`) à chaque code d'alerte reflète le choix d'implémentation, **non un arbitrage
  SOPAL**. La liste des alertes qui doivent réellement bloquer une clôture reste à définir.
- **A2 — Politique de purge** : aucune règle de rétention n'est définie pour les alertes
  résolues ou ignorées. La table croît indéfiniment.
- **A3 — Notification** : aucune notification (courriel, tableau de bord temps réel) n'est
  prévue ; les alertes ne sont visibles qu'en consultant l'écran dédié.
- **Statut** : **À valider** par SOPAL.

Le catalogue exhaustif des codes réellement implémentés, avec condition, source, unité,
gravité, résolution automatique et statut de validation, est maintenu dans
[`AlertesMetier.md`](AlertesMetier.md#catalogue-livre). Aucun code non présent dans ce
catalogue ne doit être émis par le générateur.

---

## RM-VAL — Validation et clôture

### RM-VAL-001 — Workflow de bilan

- **Opérations attendues** : vérifier, justifier, clôturer un bilan.
- **État actuel** : **aucun endpoint backend n'existe**, quel que soit le bilan — y
  compris le Bilan Usinage 2 dont seul le calcul en lecture est exposé. Les opérations
  sont simulées côté frontend sur données de démonstration et **ne sont pas persistées**.
- **Conséquence métier** : un bilan ne peut pas être clôturé durablement. L'application
  ne remplace pas encore le classeur Excel sur ce point précis.
- **Décision attendue de SOPAL** : définir les statuts de bilan, les rôles autorisés à
  valider et à clôturer, et le comportement d'une période clôturée (saisies encore
  possibles ou non).
- **Statut** : **À valider** — fonctionnalité absente.

### RM-VAL-002 — Comportement en période clôturée

- **Point d'arbitrage** : une saisie postérieure à la clôture d'une période doit-elle être
  refusée, acceptée en tant que régularisation, ou rattachée à la période suivante ?
- **Statut** : **À valider**.

---

## Index des règles à valider

Les points suivants attendent un arbitrage SOPAL. Tant qu'ils ne sont pas tranchés, le
comportement actuel est conservé tel quel.

| Règle | Sujet | Impact |
| --- | --- | --- |
| `RM-U2-010` | Colonne, unité et conversion du poids théorique | **Bloquant** : écart de poids inerte |
| `RM-VAL-001` | Workflow vérifier / justifier / clôturer | **Bloquant** : aucun bilan clôturable |
| `RM-U2-009` | Choix entre plusieurs contrôles poids validés | Poids physique non résolu si pluralité |
| `RM-U2-011` | Seuil d'alerte `> 3 %` ou `>= 3 %`, écarts négatifs | Alerte non paramétrée |
| `RM-UAP2-002` | Signe du Rebut U2 dans l'écart | Écart faussé de deux fois le rebut U2 |
| `RM-FON-003` | Conversion des sorties X3 en unités non-KG | Sorties partiellement exclues du bilan |
| `RM-X3-003` | Rattachement des préfixes `NOY`, `GRN`, `COQ`, `CBP` | Lignes exclues des agrégats |
| `RM-FON-002` | Liste officielle des types matière | Référentiel limité à trois codes |
| `RM-VAL-002` | Saisie après clôture de période | Comportement indéfini |

## Règles confirmées mais non implémentées côté serveur

| Règle | Sujet | État |
| --- | --- | --- |
| `RM-VAL-001` | Workflow vérifier / justifier / clôturer | Démonstration uniquement ; aucun endpoint |

Les bilans Fonderie (`RM-FON-003`) et UAP2 (`RM-UAP2-001`, `RM-UAP2-002`) sont désormais
calculés côté serveur — voir le tableau ci-dessous.

## Endpoints de bilan exposés

| Règle | Endpoint | Unité du résultat |
| --- | --- | --- |
| `RM-FON-003` | `GET /api/v1/bilans/fonderie/{periode}` | kilogrammes |
| `RM-UAP2-001` | `GET /api/v1/bilans/uap2/{periode}` (`sortieMpKg`) | kilogrammes |
| `RM-UAP2-002` | `GET /api/v1/bilans/uap2/{periode}` (`ecartUap2Pieces`) | **pièces** |
| `RM-U2-*` | `GET /api/v1/bilans/usinage2/{periode}` | pièces |

---

### Traçabilité

Toute règle ajoutée ou modifiée doit l'être simultanément dans :

1. le **domaine ou l'application** (jamais dans un contrôleur, jamais dupliquée côté frontend) ;
2. les **tests** (unitaires pour la formule, intégration pour l'exposition HTTP) ;
3. **ce document**.

Une règle marquée **À valider** ne doit pas être « corrigée » par supposition : elle doit
être arbitrée par le métier, puis reclassée **Confirmée** avec la date et l'interlocuteur
de validation.
