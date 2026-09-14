# Automatisation des alertes : audit et solution proposée

## Décision de ce lot

**Pas de déclencheur automatique ajouté.** Le recalcul explicite et son bouton
« Recalculer » sont conservés. Les conditions d'alerte et l'idempotence existante
ne changent pas. Cette décision applique l'option de la mission autorisant une
recommandation documentée si le couplage transactionnel est important.

## Constats vérifiés dans le code

- `AlerteGenerationService.RecalculerAsync` appelle le détecteur existant pour un
  mois, déduplique, met à jour/résout les occurrences et appelle `SaveChangesAsync`.
  Les repositories métier, alertes et audit partagent le DbContext scoped.
- `AuditService.EnregistrerAsync` persiste immédiatement une trace ; ce n'est pas
  un événement « transaction métier terminée ». Un branchement général ici serait
  trop tôt et pourrait aussi se rappeler via les traces de recalcul d'alertes.
- `InventaireComptageService` utilise une transaction explicite : changement de
  statut, audit, premier enregistrement, recalcul de projection, second
  enregistrement, puis commit. Une détection sur la trace de validation lirait une
  projection encore ancienne.
- La validation d'un contrôle poids persiste d'abord le contrôle, puis rafraîchit
  les contextes des réceptions concernées, puis les persiste. Ces réceptions
  peuvent appartenir à plusieurs mois. Utiliser seulement le mois du contrôle
  manquerait des périodes ; déclencher avant le dernier enregistrement serait
  prématuré. L'annulation ne suit pas exactement cette même chaîne : ce lot ne
  change pas ses règles métier.
- Les imports peuvent modifier des sources historiques de plusieurs mois. Les
  traces d'import sont des résumés de lot, pas une liste fiable des périodes dont
  les bilans ont changé. `ImportDependencyRefreshService` retourne actuellement
  `Task.CompletedTask` : ce n'est pas une file durable de traitements.
- `AlerteApplicationService` exige un utilisateur autorisé puis audite le recalcul.
  Il ne doit pas être appelé depuis un worker en lui inventant un rôle Admin.

Sources : `Application/Features/Alertes/AlerteGenerationUseCases.cs`,
`Application/Features/Alertes/AlerteApplicationUseCases.cs`,
`Application/Features/Audit/AuditServices.cs`,
`Application/Features/InventairesFonderie/InventaireComptageUseCases.cs`,
`Application/Features/ControlePoidsFonderie/ControlePoidsFonderieUseCases.cs`,
`Application/Features/Audit/AuditedImportServices.cs` et
`Application/Services/ImportDependencyRefreshService.cs`, sous `backend/src/BilanMatiereUAP2.Application`.

## Solution technique recommandée, non implémentée

1. Recenser les périodes réellement touchées dans chaque opération métier, y
   compris les dépendances mises à jour. Enregistrer une demande durable de
   recalcul par période dans la transaction de l'opération, une fois ses
   projections cohérentes. Pour les opérations à plusieurs sauvegardes sans
   transaction englobante, définir explicitement la frontière de réussite avant
   de brancher cette publication.
2. Après commit, un worker ouvre un scope/DbContext indépendant et appelle
   **`IAlerteGenerationService.RecalculerAsync(periode)`**, sans dupliquer les
   règles ni recalculer toutes les périodes. Il trace son action sous l'identité
   système existante, avec identifiant de corrélation et opération d'origine.
3. Regrouper les demandes du même mois et préserver toute demande reçue pendant
   un recalcul (version de demande ou traitement d'événements distincts).
   Sérialiser par période les recalculs automatiques **et manuels**. L'index unique
   des alertes actives ne suffit pas, à lui seul, à éviter tous les conflits de
   recalculs simultanés.
4. En cas de panne, conserver la demande et réessayer avec temporisation, journal
   d'erreur et suivi des échecs. Une erreur de détection ne revient pas annuler
   une mutation métier déjà commitée. Éviter `Task.Run` non attendu et les files
   uniquement en mémoire, qui perdent des demandes au redémarrage.

| Déclencheur candidat | Périodes à identifier |
| --- | --- |
| Validation/annulation déchet | Mois de déclaration |
| Validation/annulation inventaire | Mois des projections effectivement changées, avec dépendances de stock |
| Validation/annulation contrôle poids | Mois des réceptions et bilans effectivement affectés |
| Validation/annulation réception U2 | Mois de réception |
| Validation/annulation rebut U2 | Mois de déclaration |
| Import X3 terminé | Mois des lignes intégrées/modifiées et de leurs dépendances ; période du suivi d'import si pertinente |

## Tests à prévoir avant activation

Vérifier : rollback sans demande consommable, publication après projection,
redémarrage entre commit et traitement, échec du détecteur sans rollback métier,
doublons idempotents, concurrence manuel/automatique, nouvelle mutation pendant le
recalcul, imports multi-mois, permissions HTTP inchangées et audit système.
Les archives et rapports depuis snapshot restent hors de cette chaîne : aucun
recalcul automatique ne doit modifier une photographie historique.

## Risque restant aujourd'hui

Sans recalcul explicite, la liste persistée peut être en retard sur les sources.
En recette, relancer « Recalculer » pour les mois affectés après validation,
annulation ou import. L'automatisation nécessite un lot technique dédié avec
persistance des demandes, orchestration et tests de concurrence.
