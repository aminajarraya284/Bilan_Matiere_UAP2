# Synchronisation Sage X3

## État actuel

La synchronisation automatique Sage X3 est **désactivée** (`disabled`). Les données
X3 sont intégrées par import manuel de fichiers Excel. Aucun connecteur fictif,
aucune tâche planifiée et aucun appel vers un service X3 ne doivent être ajoutés
tant que les informations d'intégration ne sont pas validées par SOPAL.

## Mode futur

Le futur connecteur devra être défini après fourniture des éléments suivants :

- URL et environnement de recette ;
- protocole et documentation API ;
- authentification et compte technique ;
- pagination, limites et fréquence ;
- stratégie de reprise et d'idempotence ;
- responsabilités d'exploitation et supervision.

Statut : `TODO_INFRA`.
