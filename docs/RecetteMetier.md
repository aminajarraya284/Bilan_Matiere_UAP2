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
| ALT-001 | Alertes | Période avec une anomalie source | Lancer le recalcul | Une nouvelle alerte persistante référence la source | Couvert par tests automatisés | Recette SOPAL requise |
| ALT-002 | Alertes | Même période et même anomalie | Recalculer deux fois | Une seule alerte active ; première date conservée, dernière actualisée | Couvert par tests unitaires et HTTP | Recette SOPAL requise |
| ALT-003 | Alertes | Alerte ouverte | Acquitter | Statut `Acquittee`, utilisateur et date UTC conservés | Couvert par tests unitaires et HTTP | Recette SOPAL requise |
| ALT-004 | Alertes | Alerte ouverte ou acquittée | Résoudre | Statut `Resolue`, historique conservé, source non modifiée | Couvert par tests unitaires et HTTP | Recette SOPAL requise |
| ALT-005 | Alertes | Alerte active | Ignorer avec puis sans commentaire | Commentaire obligatoire ; alerte ignorée toujours consultable | Couvert par tests unitaires, HTTP et Vue | Recette SOPAL requise |
| ALT-006 | Alertes | Plusieurs périodes | Filtrer par `YYYY-MM` | Seule la période demandée est retournée | Couvert par tests HTTP | Recette SOPAL requise |
| ALT-007 | Alertes | Plusieurs statuts | Filtrer par statut | Résultat et résumé cohérents | Couvert par tests HTTP/frontend | Recette SOPAL requise |
| ALT-008 | Alertes | Plusieurs gravités | Filtrer par gravité | Seules les alertes de la gravité demandée sont retournées | Couvert par tests application/frontend | Recette SOPAL requise |
| ALT-009 | Alertes | Source avec route existante | Ouvrir le détail puis la source | Route réelle construite selon source, période et OF | Couvert par tests frontend | Recette visuelle requise |
| ALT-010 | Alertes | Responsable Fonderie et alerte U2 | Consulter ou traiter l'alerte | `403`, aucune modification | Couvert par tests application et HTTP | Recette Entra requise |
| ALT-011 | Alertes | Condition sûre disparue | Recalculer la même période | Résolution automatique par `SYSTEME` uniquement si le code est éligible | Couvert par tests unitaires | Recette SOPAL requise |
| ALT-012 | Alertes | Poids ou quantité source absent | Recalculer puis afficher | Valeur absente explicite, jamais remplacée par zéro | Couvert par tests unitaires/frontend | Recette SOPAL requise |
| ALT-013 | Alertes | Import au statut `Echec` | Recalculer le mois | Alerte `X3_IMPORT_ECHEC`, gravité `Erreur`, non auto-résolue | Couvert par tests de détection à compléter | Recette SOPAL requise |
| ALT-014 | Alertes | Ligne éligible sans poids physique | Recalculer | Alerte de poids manquant avec unité `kg/piece`, aucun zéro | Couvert par service source et catalogue | Recette SOPAL requise |
| ALT-015 | Alertes | Quantité non réceptionnée positive du bilan U2 | Recalculer | Alerte en pièces, quantité et signe issus du bilan sans seuil ajouté | Couvert par tests générateur et services source | Recette SOPAL requise |
| RPT-001 | Rapports | Période Fonderie complète | Ouvrir RPT-001 | Projection identique au service Fonderie, colonnes en kg | Couvert par tests HTTP/frontend | Recette SOPAL requise |
| RPT-002 | Rapports | Période Fonderie partielle | Ouvrir RPT-001 | Badge partiel et avertissement, total inconnu conservé `null` | Couvert par tests service/frontend | Recette SOPAL requise |
| RPT-003 | Rapports | Sortie X3 non-KG | Ouvrir RPT-001 | Anomalie source visible, aucune conversion inventée | Couvert par service Bilan Fonderie | Recette SOPAL requise |
| RPT-004 | Rapports | Période U2 existante | Ouvrir RPT-002 | Quantités et écart en pièces, pagination cohérente | Couvert par tests HTTP | Recette SOPAL requise |
| RPT-005 | Rapports | Période UAP2 existante | Ouvrir RPT-003 | CTRL de référence, écart en pièces et sortie MP kg séparée | Couvert par tests HTTP/frontend | Recette SOPAL requise |
| RPT-006 | Rapports | OF existant | Ouvrir RPT-004 avec OF et période | Sections contexte, réceptions, processus, mouvements, poids, références, bilans et alertes | Couvert structurellement par tests HTTP | Recette avec données requise |
| RPT-007 | Rapports | OF inconnu | Ouvrir RPT-004 | Rapport partiel et données manquantes explicites, aucun faux contexte | Couvert par tests HTTP | Recette SOPAL requise |
| RPT-008 | Rapports | Alertes persistantes | Ouvrir RPT-005 et filtrer | Liste et compteurs issus de `/alertes`, aucun mock | Couvert par tests HTTP/frontend | Recette SOPAL requise |
| RPT-009 | Rapports | Inventaire mensuel validé | Ouvrir RPT-006 | Synthèse par matière, fours séparés du total bilan | Couvert par tests HTTP | Recette SOPAL requise |
| RPT-010 | Rapports | Imports SQL présents | Ouvrir RPT-007 | Historique paginé, compteurs et statut réels, aucun hash exposé | Couvert par tests HTTP | Recette avec imports requise |
| RPT-011 | Rapports | Journée avec non-réceptions | Ouvrir RPT-008 | Quantités en pièces et données manquantes explicites | Couvert par tests HTTP | Recette SOPAL requise |
| RPT-012 | Rapports | Rapport exportable | Exporter CSV | BOM UTF-8, `;`, guillemets échappés, nom sûr, mêmes filtres que l'aperçu | Couvert par tests unitaires/HTTP/frontend | Recette téléchargement requise |
| RPT-013 | Rapports | Catalogue chargé | Rechercher l'export Excel | Excel non annoncé ; CSV est le seul format validé du Lot 5 | Couvert par catalogue HTTP/frontend | Non applicable tant qu'Excel n'est pas retenu |
| RPT-014 | Rapports | Source avec valeur absente | Afficher puis exporter | `null` affiché « Non renseigné » et cellule CSV vide, jamais zéro | Couvert par tests service/frontend | Recette SOPAL requise |
| RPT-015 | Rapports | Responsable Fonderie | Ouvrir le catalogue et appeler RPT-002 | Rapports Fonderie autorisés, rapport U2 refusé `403` | Couvert par tests HTTP | Recette Entra requise |
| RPT-016 | Rapports | Responsable Usinage 2 | Ouvrir le catalogue | RPT-002, RPT-003 et RPT-008 disponibles ; Fonderie/Inventaire absents | Couvert par tests service/permissions | Recette Entra requise |
| RPT-017 | Rapports | Admin | Ouvrir le catalogue | Les huit codes de rapport sont accessibles | Couvert par tests HTTP | Recette Entra requise |
| RPT-018 | Rapports | API indisponible | Actualiser l'aperçu | Erreur visible, aperçu effacé, aucun fallback mock | Couvert par tests store/Vue | Recette locale requise |
| RPT-019 | Rapports | Aperçu chargé | Inspecter les en-têtes | `kg`, `pièces` et `kg/pièce` sont affichés sur les colonnes concernées | Couvert par tests frontend | Recette visuelle requise |
| RPT-020 | Rapports | Rapport UAP2 ou détail OF | Inspecter les résumés | Aucun total additionnant kilogrammes et pièces | Couvert par conception et tests bilans | Recette SOPAL requise |
| AUD-001 | Audit | Comptage d'inventaire brouillon | Valider le comptage puis consulter Audit | Trace `Validation/InventaireFonderie`, utilisateur, période, avant/après et Correlation ID | Couvert par test d'intégration | Recette SOPAL requise |
| AUD-002 | Audit | Réception U2 validée | Annuler la réception puis consulter Audit | Trace `Annulation/ReceptionUsinage2`, réception conservée et valeurs ciblées | Couvert par test d'intégration | Recette SOPAL requise |
| AUD-003 | Audit | Fichier X3 valide | Importer puis filtrer le domaine `ImportX3` | Une trace globale `ImportReussi`, compteurs et nom du fichier, aucune ligne détaillée | Couvert par test unitaire | Recette avec fichier anonymisé requise |
| AUD-004 | Audit | Même fichier X3 déjà importé | Réimporter le fichier | Une trace `ImportDuplicate`, aucune duplication métier et aucun contenu de fichier dans l'audit | Couvert par test unitaire | Recette avec fichier anonymisé requise |
| AUD-005 | Audit | Alerte ouverte | Acquitter puis consulter Audit | Trace `Acquittement/Alerte` avec utilisateur et transition de statut | Couvert par tests HTTP | Recette SOPAL requise |
| AUD-006 | Audit | Alerte ouverte ou acquittée | Résoudre puis consulter Audit | Trace `Resolution/Alerte`, historique et source métier inchangés | Couvert par tests HTTP | Recette SOPAL requise |
| AUD-007 | Audit | Mutation comportant un avant/après | Ouvrir le détail de la trace | Tableau lisible Champ/Avant/Après, sans JSON brut | Couvert par tests Application et Vue | Recette visuelle requise |
| AUD-008 | Audit | Données contenant `Password`, token ou clé imbriquée | Produire puis consulter la trace | Aucun nom ni valeur sensible dans le JSON ou la réponse HTTP | Couvert par tests sanitizer et HTTP | Recette sécurité requise |
| AUD-009 | Audit | Authentification locale Development | Effectuer une mutation | Identifiant, nom et rôle du compte Development enregistrés | Couvert par tests Application/HTTP | Recette locale requise |
| AUD-010 | Audit | Compte Entra de recette | Effectuer une mutation | Claims Entra (ID, nom, email éventuel, rôles) enregistrés sans jeton | Couvert au niveau Application | Recette Entra requise |
| AUD-011 | Audit | Traitement sans utilisateur interactif | Produire une trace système | `UtilisateurId=SYSTEM`, nom `Système`, rôle `System`, jamais Admin | Couvert par test Application | Recette d'exploitation requise |
| AUD-012 | Audit | Requête avec `X-Correlation-ID` valide | Acquitter une alerte puis lire la trace | Même identifiant dans réponse HTTP, logs de requête et trace SQL | Couvert par test HTTP | Recette locale requise |
| AUD-013 | Audit | Plus de traces que `pageSize` | Parcourir les pages | Total, page et taille cohérents, aucune table chargée intégralement | Couvert par tests Application/HTTP/frontend | Recette avec volume requise |
| AUD-014 | Audit | Traces de plusieurs utilisateurs | Filtrer par utilisateur | Seules les traces correspondant à l'ID, au nom ou à l'email sont retournées | Couvert par tests Application | Recette Entra requise |
| AUD-015 | Audit | Traces de plusieurs domaines | Filtrer par domaine | Seul le domaine demandé est retourné, ordre UTC décroissant | Couvert par tests Application/HTTP | Recette locale requise |
| AUD-016 | Audit | Compte Admin authentifié | Ouvrir `/audit` et appeler les deux endpoints | Page visible, liste et détail HTTP `200` | Couvert par tests HTTP/frontend | Recette Entra requise |
| AUD-017 | Audit | Responsable Fonderie authentifié | Appeler `GET /api/v1/audit` | `403`, aucune donnée du journal exposée | Couvert par test HTTP | Recette Entra requise |
| AUD-018 | Audit | Responsable Usinage 2 authentifié | Appeler `GET /api/v1/audit` | `403`, aucune donnée du journal exposée | Couvert par test HTTP | Recette Entra requise |
| AUD-019 | Audit | Trace persistée | Rechercher une action de modification/suppression Audit | Aucun endpoint d'écriture et aucun mécanisme Domain/Repository de mutation | Couvert par tests Domain/HTTP | Revue sécurité requise |
| AUD-020 | Audit | Snapshot contenant une valeur absente et un vrai zéro | Ouvrir le détail | La valeur absente reste `null`; le zéro reste `0` | Couvert par tests Domain/Application/Vue | Recette SOPAL requise |
| SEC-001 | Autorisations | Utilisateur non Admin | Ouvrir Administration/Audit | Accès refusé par le backend | — | Non exécuté |
| OPS-001 | Synchronisation X3 | Aucun connecteur fourni | Vérifier le module | Fonctionnalité désactivée, aucune synchronisation simulée | — | Non exécuté |

## Données et responsables attendus

La recette nécessite des fichiers X3 anonymisés, une base de recette restaurée,
des comptes pour les trois rôles applicatifs, une période de référence et la
validation métier des règles encore ouvertes. Les responsables de validation
sont SOPAL pour les formules, les seuils et les rôles, et l'équipe technique
pour l'installation, les imports et la sécurité d'exécution.

Pour le Dashboard, la recette doit aussi confirmer la priorité métier des statuts et l'acceptation de la limite actuelle suivante : le compteur mensuel des contrôles poids ambigus reste « Non disponible » tant qu'une règle d'agrégation fiable n'est pas définie.
