import type { AuditDetail, AuditListItem, PagedAuditResponse } from './audit.types';

export const auditListItem: AuditListItem = {
  id: '11111111-1111-1111-1111-111111111111',
  dateUtc: '2026-08-18T10:30:00Z',
  action: 'Validation',
  domaine: 'ReceptionUsinage2',
  entite: 'ReceptionUsinage2',
  entiteId: '22222222-2222-2222-2222-222222222222',
  referenceMetier: 'OF-2026-001|ART-001',
  utilisateurNom: 'Administrateur SOPAL',
  role: 'Admin',
  source: 'ReceptionUsinage2Service',
  correlationId: 'CORR-AUDIT-001',
  resume: "Validation d'une réception Usinage 2."
};

export const createAuditDetail = (overrides: Partial<AuditDetail> = {}): AuditDetail => ({
  ...auditListItem,
  utilisateurId: 'de000000-0000-0000-0000-000000000001',
  utilisateurEmail: 'admin@sopal.local',
  anciennesValeurs: {
    statut: 'Brouillon',
    quantitePieces: 100,
    poidsTotalReceptionneKg: null
  },
  nouvellesValeurs: {
    statut: 'Valide',
    quantitePieces: 100,
    poidsTotalReceptionneKg: 250
  },
  informations: {
    origine: 'Fonderie'
  },
  ...overrides
});

export const createPagedAudit = (items: AuditListItem[] = [auditListItem]): PagedAuditResponse => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 50,
  derniereActiviteUtc: items.length ? items[0].dateUtc : null
});
