import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn()
}));

vi.mock('@/config/dataSources', () => ({
  dataSources: { dechetsFonderie: 'api', typesMatiere: 'api' },
  // Le service garde ses appels derrière assertApiSource : en mode API, la garde passe.
  assertApiSource: vi.fn(),
  isApiSource: () => true
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { dechetsService } from './dechets.service';

const payload = {
  dateDeclaration: '2026-07-22',
  typeMatiereId: '30000000-0000-0000-0000-000000000001',
  typeDechet: 'Bavure' as const,
  poidsKg: 125.5,
  observation: 'Bavures triées',
  // L'origine fait partie du référentiel « Déchets et rebuts UAP2 » et est obligatoire.
  origine: 'Fonderie' as const
};

describe('real waste API service', () => {
  beforeEach(() => vi.clearAllMocks());

  it('loads active material types from the versioned API', async () => {
    mocks.get.mockResolvedValueOnce({ data: [{ id: payload.typeMatiereId, code: 'M3000L0', estActif: true }] });

    const result = await dechetsService.getTypesMatiere();

    expect(result[0].code).toBe('M3000L0');
    expect(mocks.get).toHaveBeenCalledWith('/v1/types-matiere', { params: { actif: true } });
  });

  it('loads a filtered and paged waste list', async () => {
    mocks.get.mockResolvedValueOnce({ data: { items: [], total: 0, page: 1, pageSize: 20 } });

    await dechetsService.getDechetsFonderie({ periode: '2026-07', typeDechet: 'Poudre', page: 1, pageSize: 20 });

    expect(mocks.get).toHaveBeenCalledWith('/v1/dechets-fonderie', {
      params: { periode: '2026-07', typeDechet: 'Poudre', page: 1, pageSize: 20 }
    });
  });

  it('creates a waste without sending calculated or audit fields', async () => {
    mocks.post.mockResolvedValueOnce({ data: { id: 'waste-id', ...payload } });

    await dechetsService.createDechetFonderie(payload);

    expect(mocks.post).toHaveBeenCalledWith('/v1/dechets-fonderie', payload);
    expect(mocks.post.mock.calls[0][1]).not.toHaveProperty('semaineIso');
    expect(mocks.post.mock.calls[0][1]).not.toHaveProperty('statut');
  });

  // Aucune suppression physique : la « suppression » passe par le cycle de vie Annulé,
  // qui conserve le déchet dans l'historique et le sort des totaux.
  it('routes deletion to the cancellation endpoint, never to DELETE', async () => {
    mocks.post.mockResolvedValueOnce({ data: { id: 'waste-id', statut: 'Annule' } });

    await dechetsService.deleteDechetFonderie('waste-id');

    expect(mocks.post).toHaveBeenCalledWith('/v1/dechets-fonderie/waste-id/annuler');
    expect(mocks.delete).not.toHaveBeenCalled();
  });
});
