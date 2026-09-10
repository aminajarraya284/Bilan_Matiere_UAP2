# Clôture et archivage des périodes

## Architecture

Une clôture appelle les services officiels `IBilanFonderieService`, `IBilanUsinage2Service` et `IBilanUap2Service`. Les formules ne sont pas dupliquées. Le résultat complet, les anomalies et les paramètres réellement consommés sont sérialisés dans `PeriodesBilansArchivees.SnapshotJson`.

Le snapshot utilise le schéma version 1 et la version de calcul `2026.09`. Une contrainte unique SQL sur `Periode` interdit le remplacement silencieux. Il n’existe aucun endpoint `PUT`, `PATCH` ou `DELETE` pour une archive.

## API et droits

Toutes les routes utilisent la policy `ManagePeriodArchives`, réservée au rôle `Admin` :

- `POST /api/v1/periodes-bilan/{periode}/enregistrer` : recalcule les trois bilans côté serveur et crée le snapshot ;
- `GET /api/v1/periodes-bilan` : historique paginé, filtrable par période et statut ;
- `GET /api/v1/periodes-bilan/{periode}` : détail lu exclusivement depuis SQL ;
- `GET /api/v1/periodes-bilan/{periode}/rapport` : rapport Markdown généré depuis le snapshot.

Un second enregistrement de la même période renvoie un conflit `PERIODE_DEJA_ENREGISTREE` et ne modifie pas l’archive initiale.

## Fidélité du snapshot

- Le service interne `IBilanUsinage2Service.GetAsync` fournit toutes les lignes de la période, y compris au-delà de 50 ou 200 OF. La pagination reste réservée à `QueryAsync` pour les écrans de consultation.
- Fonderie reste en kilogrammes.
- Usinage 2 et UAP2 restent en pièces.
- Les taux restent des pourcentages.
- `null` reste `null` dans SQL/JSON et devient `—` dans l’interface et le rapport.
- Les anomalies sont copiées au moment de la clôture et ne sont jamais recalculées à la lecture.
- Les paramètres archivés sont `CODE_ATELIER_UAP2` et `SEUIL_ECART_UAP2`, seuls paramètres actuellement lus par les services de bilan.

## Rapport

Le format officiel existant est [Template_Rapport_Bilan_Matiere.md](Template_Rapport_Bilan_Matiere.md). La première version produit donc un fichier Markdown UTF-8. Le contenu métier vient exclusivement du snapshot SQL.

## Limites

Une seule version officielle est autorisée par période. La création d’une nouvelle version et la génération PDF/Excel sont hors périmètre. Une archive peut être clôturée avec des anomalies ou des données manquantes ; son statut les expose explicitement.

La correction du 9 septembre 2026 empêche la troncature des nouvelles archives Usinage 2 à la première page. Elle ne modifie pas les snapshots déjà enregistrés. Pour repérer une archive historiquement tronquée, comparer le nombre d'éléments de `snapshot.usinage2.lignes` avec `snapshot.usinage2.total`. Une éventuelle reprise de ces archives exige une procédure explicite conservant leur traçabilité ; aucun recalcul automatique n'est effectué à la lecture.
