import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  defaultReportFilters,
  fonderieApiPayload,
  reportCatalogue,
  reportMetadata
} from './rapports.test.fixtures';

const api = vi.hoisted(() => ({ get: vi.fn() }));
vi.mock('@/services/api', () => ({ default: api }));

import { rapportsService } from './rapports.service';

describe('rapportsService', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_RAPPORTS_SOURCE', 'api');
  });

  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllEnvs();
  });

  it('charge le catalogue réel autorisé', async () => {
    api.get.mockResolvedValue({ data: reportCatalogue });

    await expect(rapportsService.getCatalogue()).resolves.toEqual(reportCatalogue);

    expect(api.get).toHaveBeenCalledWith('/v1/rapports/catalogue');
  });

  it('normalise le bilan Fonderie sans transformer un null en zéro', async () => {
    api.get.mockResolvedValue({ data: fonderieApiPayload });

    const preview = await rapportsService.preview({
      code: 'RPT-001',
      filters: defaultReportFilters,
      page: 1,
      pageSize: 50
    });

    expect(api.get).toHaveBeenCalledWith('/v1/rapports/fonderie/2026-01');
    expect(preview.rows[0].cells[1]).toBeNull();
    expect(preview.summary[0].value).toBeNull();
    expect(preview.metadata.unitePrincipale).toBe('kg');
  });

  it('envoie uniquement les filtres utiles pour les imports paginés', async () => {
    api.get.mockResolvedValue({
      data: {
        metadata: { ...fonderieApiPayload.metadata, code: 'RPT-007', unitePrincipale: null },
        donnees: { items: [], total: 0, page: 2, pageSize: 25 }
      }
    });

    await rapportsService.preview({
      code: 'RPT-007',
      filters: { ...defaultReportFilters, dateDepuis: ' 2026-01-01 ', dateJusqua: '' },
      page: 2,
      pageSize: 25
    });

    expect(api.get).toHaveBeenCalledWith('/v1/rapports/imports', {
      params: { dateDepuis: '2026-01-01', page: 2, pageSize: 25 }
    });
  });

  it("affiche la description et la traçabilité d'une alerte traitée", async () => {
    api.get.mockResolvedValue({
      data: {
        metadata: { ...reportMetadata, code: 'RPT-005', unitePrincipale: null },
        donnees: {
          items: [
            {
              id: 'alerte-1',
              code: 'U2_QUANTITE_NON_RECEPTIONNEE',
              type: 'AnomalieMetier',
              severite: 'Avertissement',
              statut: 'Resolue',
              titre: 'Quantité non réceptionnée',
              description: 'Une quantité reste à réceptionner.',
              source: 'Bilan Usinage 2',
              periode: '2026-01',
              numeroOF: 'OF-001',
              codeArticle: 'ART-001',
              typeMatiere: null,
              detecteeLeUtc: '2026-01-15T10:00:00Z',
              derniereDetectionLeUtc: '2026-01-15T11:00:00Z',
              resolueLeUtc: '2026-01-15T12:00:00Z',
              resoluePar: 'Responsable U2',
              commentaireTraitement: 'Réception enregistrée.'
            }
          ],
          total: 1,
          page: 1,
          pageSize: 50,
          summary: { total: 1, ouvertes: 0, acquittees: 0, resolues: 1, ignorees: 0 },
          derniereDetectionLeUtc: '2026-01-15T11:00:00Z'
        }
      }
    });

    const preview = await rapportsService.preview({
      code: 'RPT-005',
      filters: defaultReportFilters,
      page: 1,
      pageSize: 50
    });

    expect(preview.columns.map((column) => column.label)).toContain('Commentaire');
    expect(preview.rows[0].cells).toContain('Une quantité reste à réceptionner.');
    expect(preview.rows[0].cells).toContain('Responsable U2');
    expect(preview.rows[0].cells).toContain('Réception enregistrée.');
  });

  it('télécharge le CSV fourni par le backend', async () => {
    const blob = new Blob(['csv'], { type: 'text/csv' });
    api.get.mockResolvedValue({
      data: blob,
      headers: { 'content-disposition': 'attachment; filename="rpt-001-2026-01.csv"' }
    });

    const result = await rapportsService.exportCsv('RPT-001', defaultReportFilters);

    expect(result).toEqual({ blob, fileName: 'rpt-001-2026-01.csv' });
    expect(api.get).toHaveBeenCalledWith('/v1/rapports/RPT-001/export', expect.objectContaining({ responseType: 'blob' }));
  });

  it("propage l'erreur API sans fallback vers les mocks", async () => {
    api.get.mockRejectedValue(new Error('API rapports indisponible'));

    await expect(rapportsService.getCatalogue()).rejects.toThrow('API rapports indisponible');
    expect(api.get).toHaveBeenCalledOnce();
  });

  it('refuse explicitement le mode mock', async () => {
    vi.stubEnv('VITE_RAPPORTS_SOURCE', 'mock');

    await expect(rapportsService.getCatalogue()).rejects.toThrow('Rapports nécessite une source API');
    expect(api.get).not.toHaveBeenCalled();
  });
});
