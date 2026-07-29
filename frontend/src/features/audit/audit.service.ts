import api from '@/services/api';
import { AuditLogEntry } from './audit.types';
import { storage } from '@/utils/storage';
import { isMockSource } from '@/config/dataSources';

const mockLogs: AuditLogEntry[] = [
  {
    id: 1,
    dateHeure: '2026-06-30 08:45:12',
    utilisateur: 'Amina Jarraya',
    role: 'Administrateur',
    action: 'Connexion Réussie',
    module: 'Sécurité',
    detail: "Authentification via JWT validée depuis l'adresse IP interne.",
    statut: 'Succès'
  },
  {
    id: 2,
    dateHeure: '2026-06-30 08:46:25',
    utilisateur: 'Amina Jarraya',
    role: 'Administrateur',
    action: 'Lancement Calcul Bilan',
    module: 'Bilan Matière',
    detail: 'Recalcul global des encours et des rendements pour la période 2026-06.',
    statut: 'Succès'
  },
  {
    id: 3,
    dateHeure: '2026-06-30 09:12:00',
    utilisateur: 'Ahmed Trabelsi',
    role: 'Resp. Fonderie',
    action: 'Validation Inventaire',
    module: 'Inventaire Fonderie',
    detail: 'Validation physique de la Partie 1 (Conteneurs) pour le mois de Juin.',
    statut: 'Succès'
  },
  {
    id: 4,
    dateHeure: '2026-06-30 09:15:30',
    utilisateur: 'Khalil Ben Ali',
    role: 'Resp. Usinage 2',
    action: 'Saisie Réception',
    module: 'Réceptions Usinage 2',
    detail: "Saisie manuelle du lot d'OF18100565 pour l'atelier 09.",
    statut: 'Succès'
  },
  {
    id: 5,
    dateHeure: '2026-06-30 10:22:40',
    utilisateur: 'Amina Jarraya',
    role: 'Administrateur',
    action: 'Modification Paramètre',
    module: 'Configuration',
    detail: "Modification du Seuil d'Alerte Écart de 2.5% à 3.0%.",
    statut: 'Succès'
  }
];

export const auditService = {
  async getLogs(): Promise<AuditLogEntry[]> {
    if (isMockSource('audit')) {
      return [...storage.getAuditLogs(), ...mockLogs].sort((a, b) => b.dateHeure.localeCompare(a.dateHeure));
    }
    const res = await api.get<AuditLogEntry[]>('/audit');
    return res.data;
  }
};
