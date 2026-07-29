import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { controlePoidsFonderieApiService } from './controlePoidsFonderie.service';

describe('controle poids fonderie API service', () => {
  beforeEach(() => vi.clearAllMocks());

  it('loads all controls from the paged endpoint', async () => {
    mocks.get.mockResolvedValueOnce({ data: { items: [{ id: 'c1' }], total: 1 } });

    const result = await controlePoidsFonderieApiService.getAll();

    expect(result).toHaveLength(1);
    expect(mocks.get).toHaveBeenCalledWith('/v1/controles-poids-fonderie', { params: { page: 1, pageSize: 50 } });
  });

  it('creates a control with the required TypeMatiere id', async () => {
    mocks.post.mockResolvedValueOnce({ data: { id: 'c1', statut: 'Brouillon' } });

    await controlePoidsFonderieApiService.create({
      dateControle: '2026-07-26',
      numeroOF: 'OF123',
      codeArticle: 'ART1',
      typeMatiereId: 'type-1',
      numeroLotOuSerie: null,
      poidsPhysiqueUnitaireKg: 2.5,
      nombreEchantillons: 3,
      observation: null
    });

    expect(mocks.post).toHaveBeenCalledWith('/v1/controles-poids-fonderie', expect.objectContaining({
      typeMatiereId: 'type-1',
      poidsPhysiqueUnitaireKg: 2.5
    }));
  });

  it('validates a control by id', async () => {
    mocks.post.mockResolvedValueOnce({ data: { id: 'c1', statut: 'Valide' } });

    await controlePoidsFonderieApiService.valider('c1');

    expect(mocks.post).toHaveBeenCalledWith('/v1/controles-poids-fonderie/c1/valider');
  });

  it('cancels a control by id', async () => {
    mocks.post.mockResolvedValueOnce({ data: { id: 'c1', statut: 'Annule' } });

    await controlePoidsFonderieApiService.annuler('c1');

    expect(mocks.post).toHaveBeenCalledWith('/v1/controles-poids-fonderie/c1/annuler');
  });
});
