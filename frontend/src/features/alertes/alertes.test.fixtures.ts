import type { AlerteDetail, AlerteListItem, PagedAlertesResponse } from './alertes.types';

export const createAlerteListItem = (overrides: Partial<AlerteListItem> = {}): AlerteListItem => ({
  id: 'a1000000-0000-0000-0000-000000000001',
  code: 'U2_QUANTITE_NON_RECEPTIONNEE',
  type: 'AnomalieMetier',
  severite: 'Avertissement',
  statut: 'Ouverte',
  titre: 'Quantité non réceptionnée',
  source: 'Bilan Usinage 2',
  periode: '2026-01',
  numeroOF: 'OF-001',
  codeArticle: 'ART-001',
  typeMatiere: 'M3000L0',
  detecteeLeUtc: '2026-01-31T10:00:00Z',
  derniereDetectionLeUtc: '2026-01-31T12:00:00Z',
  ...overrides
});

export const createAlerteDetail = (overrides: Partial<AlerteDetail> = {}): AlerteDetail => ({
  ...createAlerteListItem(),
  description: 'Le bilan Usinage 2 signale une quantité positive non réceptionnée.',
  acquitteeLeUtc: null,
  acquitteePar: null,
  resolueLeUtc: null,
  resoluePar: null,
  ignoreeLeUtc: null,
  ignoreePar: null,
  commentaireTraitement: null,
  resolutionAutomatique: true,
  donneesAssociees: { quantitePieces: '12', unite: 'pieces' },
  ...overrides
});

export const createPagedAlertes = (items: AlerteListItem[] = [createAlerteListItem()]): PagedAlertesResponse => ({
  items,
  total: items.length,
  page: 1,
  pageSize: 50,
  summary: {
    total: items.length,
    ouvertes: items.filter((item) => item.statut === 'Ouverte').length,
    acquittees: items.filter((item) => item.statut === 'Acquittee').length,
    resolues: items.filter((item) => item.statut === 'Resolue').length,
    ignorees: items.filter((item) => item.statut === 'Ignoree').length
  },
  derniereDetectionLeUtc: items[0]?.derniereDetectionLeUtc ?? null
});
