# Recette finale

## État au 10 septembre 2026

Les corrections de robustesse sont validées localement avec SQL Server accessible. Les vérifications automatisées donnent 383 tests unitaires backend, 195 tests d'intégration et 353 tests frontend réussis. Le typecheck, le lint et le build Vite réussissent également.

La recette locale en lecture seule confirme des réponses HTTP 200 pour les types matière, réceptions U2, contrôles poids, trois bilans, dashboard, alertes, catalogue des rapports, audit et archives. Un jeton Development invalide renvoie 401, le rôle Responsable Fonderie reçoit 403 sur l'archivage et le préflight CORS depuis `http://127.0.0.1:3000` réussit.

L'authentification Entra est validée au niveau de la logique applicative avec des jetons signés de test. La validation avec le tenant Microsoft SOPAL réel reste obligatoire avant la mise en production.

Les anciennes archives ne sont ni recalculées ni modifiées. Une éventuelle reprise doit être explicite, auditée et conserver la photographie historique.

## Checklist

- [ ] Connexion Entra et création automatique du profil
- [x] Admin autorisé ; Responsable Fonderie et Responsable Usinage 2 refusés sur l’archivage (tests automatisés)
- [ ] Imports et saisies validées disponibles
- [x] Formules Fonderie, U2 et UAP2 inchangées (tests automatisés)
- [x] CTRL limité au poste exact `CTRL` et absence conservée (tests automatisés)
- [x] Clôture d’une période par l’Admin (tests d’intégration)
- [x] Snapshot SQL complet, y compris au-delà de 50 lignes U2 (tests d’intégration)
- [x] Deuxième clôture de la même période refusée en 409 (tests d’intégration)
- [x] Historique paginé, états vide/erreur/chargement et nouvelle tentative (tests frontend)
- [x] Détail en lecture seule sans appel aux bilans live (tests d’intégration)
- [x] `null` affiché `—` et unités non mélangées (tests frontend)
- [x] Rapport produit depuis le snapshot archivé (tests d’intégration)
- [x] Trace Audit avec utilisateur, UTC et Correlation ID (tests automatisés)
- [x] Aucun secret ajouté dans le diff, le snapshot, le rapport ou l’Audit
- [x] Tests backend, frontend, lint, typecheck et builds verts
- [ ] HTTPS, CORS, SQL, sauvegarde et restauration validés
