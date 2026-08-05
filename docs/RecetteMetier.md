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
| SEC-001 | Autorisations | Utilisateur non Admin | Ouvrir Administration/Audit | Accès refusé par le backend | — | Non exécuté |
| OPS-001 | Synchronisation X3 | Aucun connecteur fourni | Vérifier le module | Fonctionnalité désactivée, aucune synchronisation simulée | — | Non exécuté |

## Données et responsables attendus

La recette nécessite des fichiers X3 anonymisés, une base de recette restaurée,
des comptes pour les trois rôles applicatifs, une période de référence et la
validation métier des règles encore ouvertes. Les responsables de validation
sont SOPAL pour les formules, les seuils et les rôles, et l'équipe technique
pour l'installation, les imports et la sécurité d'exécution.
