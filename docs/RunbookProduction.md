# Runbook production

## Exploitation

- Démarrer/arrêter l’application avec le gestionnaire de services IIS ou du reverse proxy retenu.
- Contrôler `GET /health` après chaque démarrage.
- Consulter les logs applicatifs via la destination configurée par SOPAL, en recherchant le `CorrelationId`.
- Surveiller l’espace SQL, la taille de `PeriodesBilansArchivees` et les erreurs d’authentification.

## Migration

Toujours sauvegarder SQL, générer un script EF idempotent, le relire et obtenir la validation SOPAL avant exécution. Ne jamais lancer automatiquement `database update` au démarrage en production.

## Sauvegarde et incident

Vérifier périodiquement une restauration complète. En incident : préserver les logs, noter le Correlation ID, suspendre les clôtures si l’intégrité est incertaine, puis restaurer la dernière version applicative et/ou la sauvegarde validée.

## Mise à jour

1. Valider les tests en recette.
2. Sauvegarder SQL.
3. Appliquer la migration approuvée.
4. Déployer l’API puis le frontend.
5. Vérifier santé, Entra, RBAC, lecture SQL, clôture de test autorisée et rapport.
