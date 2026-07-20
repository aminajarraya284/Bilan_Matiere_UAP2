export interface AuditLogEntry {
  id: number;
  dateHeure: string;
  utilisateur: string;
  role: string;
  action: string;
  module: string;
  detail: string;
  statut: 'Succès' | 'Échec' | 'Attention';
}
