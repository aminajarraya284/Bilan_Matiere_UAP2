import { afterEach, describe, expect, it, vi } from 'vitest';
import { createAlerteDetail, createPagedAlertes } from './alertes.test.fixtures';

const api = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));
vi.mock('@/services/api', () => ({ default: api }));

import { alertesService } from './alertes.service';

const filters = {
  periode: '2026-01',
  statut: 'Ouverte' as const,
  severite: '' as const,
  source: ' Bilan Usinage 2 ',
  numeroOF: '',
  codeArticle: ' ART-001 ',
  recherche: ''
};

describe('alertesService', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it('recherche avec pagination et retire les filtres vides', async () => {
    const payload = createPagedAlertes();
    api.get.mockResolvedValue({ data: payload });

    await expect(alertesService.search({ filters, page: 2, pageSize: 25 })).resolves.toEqual(payload);

    expect(api.get).toHaveBeenCalledWith('/v1/alertes', {
      params: {
        periode: '2026-01',
        statut: 'Ouverte',
        source: 'Bilan Usinage 2',
        codeArticle: 'ART-001',
        page: 2,
        pageSize: 25
      }
    });
  });

  it('appelle le détail, le recalcul et les trois actions métier', async () => {
    const detail = createAlerteDetail();
    api.get.mockResolvedValue({ data: detail });
    api.post
      .mockResolvedValueOnce({
        data: {
          periode: '2026-01',
          recalculeLeUtc: '2026-01-31T12:00:00Z',
          nombreDetections: 1,
          nombreCreees: 1,
          nombreMisesAJour: 0,
          nombreResoluesAutomatiquement: 0
        }
      })
      .mockResolvedValue({ data: detail });

    await alertesService.get(detail.id);
    await alertesService.recalculer('2026-01');
    await alertesService.acquitter(detail.id, { commentaire: null });
    await alertesService.resoudre(detail.id, { commentaire: 'Corrigée' });
    await alertesService.ignorer(detail.id, { commentaire: 'Justification' });

    expect(api.get).toHaveBeenCalledWith(`/v1/alertes/${detail.id}`);
    expect(api.post).toHaveBeenNthCalledWith(1, '/v1/alertes/recalculer', null, {
      params: { periode: '2026-01' }
    });
    expect(api.post).toHaveBeenNthCalledWith(2, `/v1/alertes/${detail.id}/acquitter`, { commentaire: null });
    expect(api.post).toHaveBeenNthCalledWith(3, `/v1/alertes/${detail.id}/resoudre`, {
      commentaire: 'Corrigée'
    });
    expect(api.post).toHaveBeenNthCalledWith(4, `/v1/alertes/${detail.id}/ignorer`, {
      commentaire: 'Justification'
    });
  });

  it("propage l'erreur API sans fallback vers une alerte fictive", async () => {
    api.get.mockRejectedValue(new Error('API indisponible'));

    await expect(alertesService.search({ filters, page: 1, pageSize: 50 })).rejects.toThrow('API indisponible');
    expect(api.get).toHaveBeenCalledOnce();
  });

  it('refuse explicitement le mode mock au lieu de basculer silencieusement', async () => {
    vi.stubEnv('VITE_ALERTES_SOURCE', 'mock');

    await expect(alertesService.search({ filters, page: 1, pageSize: 50 })).rejects.toThrow(
      'Alertes nécessite une source API'
    );
    expect(api.get).not.toHaveBeenCalled();
  });
});
