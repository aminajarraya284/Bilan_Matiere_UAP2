# Recette métier Bilan Matière UAP2

Les scénarios ci-dessous sont préparés mais non exécutés automatiquement. Le
résultat doit être renseigné pendant la recette SOPAL.

| ID | Module | Préconditions | Étapes | Résultat attendu | Résultat obtenu | Statut |
|---|---|---|---|---|---|---|
| AUTH-001 | Authentification | Compte de recette Entra | Se connecter puis se déconnecter | Session et rôle affichés, retour à la route initiale | — | Non exécuté |
| X3-001 | Imports X3 | Fichier valide | Importer le fichier | Import terminé, lignes tracées et fichier archivé | — | Non exécuté |
| X3-002 | Imports X3 | Fichier déjà importé | Réimporter le même fichier | Import identifié comme doublon, aucune duplication | — | Non exécuté |
| INV-001 | Inventaire | Période valide | Créer puis valider un comptage | Ligne validée non modifiable | — | Non exécuté |
| U2-001 | Réceptions U2 | OF connu | Résoudre le contexte puis valider | Article, atelier et matière cohérents | — | Non exécuté |
| U2-002 | Réceptions U2 | OF sans contexte complet | Résoudre le contexte | Anomalie explicite, aucune valeur absente transformée en zéro | — | Non exécuté |
| BIL-001 | Bilan Fonderie | Données complètes | Charger une période | Bilan calculé avec unités et statut | — | Non exécuté |
| BIL-002 | Bilan UAP2 | Données partielles | Charger une période | Données manquantes signalées, total non présenté comme fiable | — | Non exécuté |
| DASH-001 | Dashboard | Période avec données complètes | Charger `YYYY-MM` | Trois écarts séparés : Fonderie en kg, Usinage 2 et UAP2 en pièces ; aucun total inter-unités | Couvert par tests automatisés | Recette SOPAL requise |
| DASH-002 | Dashboard | Période sans donnée | Charger la période | État vide explicite, écarts `null` affichés « Non disponible », aucun faux zéro | Couvert par tests automatisés | Recette SOPAL requise |
| DASH-003 | Dashboard | Une source de bilan partielle | Charger la période | Source marquée `Partiel`, avertissement visible et statut global non présenté comme `OK` | Couvert par tests automatisés | Recette SOPAL requise |
| DASH-004 | Dashboard | API indisponible après un chargement réussi | Actualiser | Erreur visible et anciennes données effacées, sans fallback de démonstration | Couvert par tests automatisés | Recette SOPAL requise |
| DASH-005 | Dashboard API | Jetons des trois rôles applicatifs | Appeler `GET /api/v1/dashboard?periode=YYYY-MM` | `200` pour Admin, Responsable Fonderie et Responsable Usinage 2 | Couvert par tests d'intégration | Recette Entra requise |
| DASH-006 | Dashboard API | Période absente/invalide ou rôle non autorisé | Appeler l'endpoint | `400` pour la période ; `401/403` selon l'authentification et le rôle | Couvert par tests d'intégration | Recette Entra requise |
| SEC-001 | Autorisations | Utilisateur non Admin | Ouvrir Administration/Audit | Accès refusé par le backend | — | Non exécuté |
| OPS-001 | Synchronisation X3 | Aucun connecteur fourni | Vérifier le module | Fonctionnalité désactivée, aucune synchronisation simulée | — | Non exécuté |

## Données et responsables attendus

La recette nécessite des fichiers X3 anonymisés, une base de recette restaurée,
des comptes pour les trois rôles applicatifs, une période de référence et la
validation métier des règles encore ouvertes. Les responsables de validation
sont SOPAL pour les formules, les seuils et les rôles, et l'équipe technique
pour l'installation, les imports et la sécurité d'exécution.

Pour le Dashboard, la recette doit aussi confirmer la priorité métier des statuts et l'acceptation de la limite actuelle suivante : le compteur mensuel des contrôles poids ambigus reste « Non disponible » tant qu'une règle d'agrégation fiable n'est pas définie.
