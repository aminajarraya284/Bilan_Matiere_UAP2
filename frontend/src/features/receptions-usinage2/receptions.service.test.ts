import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks }));
vi.stubEnv('VITE_RECEPTIONS_U2_SOURCE', 'api');

import { receptionsService } from './receptions.service';

const apiReception = {
  id: '11111111-1111-1111-1111-111111111111',
  numeroOF: 'OF123',
  dateReception: '2026-07-26',
  nombrePiecesReceptionnees: 100,
  poidsTotalReceptionneKg: 250,
  origine: 'FONDERIE',
  observation: null,
  codeArticle: '07CG201-1',
  designationArticle: 'CORPS TEST',
  situationX3: 'En cours',
  atelierOrigine: '16',
  codeTypeMatiere: 'M3010L0',
  quantiteLienUtilisee: 2.5,
  uniteQuantiteLien: 'KG',
  poidsTheoriqueUnitaireKg: 2.5,
  poidsTheoriqueTotalKg: 250,
  poidsPhysiqueUnitaireKg: 2.5,
  ecartPoidsUnitaireKg: 0,
  tauxEcartPoidsPct: 0,
  totalCtrlSci: null,
  totalCtrlEmr: null,
  totalCtrlTrn: null,
  totalRebutSci: null,
  totalRebutEmr: null,
  totalRebutTrn: null,
  quantiteNonReceptionnee: null,
  statutReception: 'Brouillon' as const,
  creePar: 'Admin',
  dateCreationUtc: '2026-07-26T10:00:00Z'
};

describe('receptions usinage 2 real API service', () => {
  beforeEach(() => vi.clearAllMocks());

  it('calls the real list endpoint (no hyphen) and maps the paged response', async () => {
    mocks.get.mockResolvedValueOnce({ data: { items: [apiReception], total: 1 } });

    const result = await receptionsService.getReceptions();

    expect(mocks.get).toHaveBeenCalledWith('/v1/receptions-usinage2', { params: { page: 1, pageSize: 100 } });
    expect(result).toHaveLength(1);
    expect(result[0].apiId).toBe(apiReception.id);
    expect(result[0].numeroOF).toBe('OF123');
    expect(result[0].poidsTotalReceptionneKg).toBe(250);
    expect(result[0].statut).toBe('Brouillon');
  });

  it('creates a reception against the real endpoint and maps the response', async () => {
    mocks.post.mockResolvedValueOnce({ data: apiReception });

    const result = await receptionsService.createReception({
      numeroOF: 'OF123',
      dateReception: '2026-07-26',
      origine: 'FONDERIE',
      poidsTotalReceptionneKg: 250,
      nombrePiecesReceptionnees: 100
    });

    expect(mocks.post).toHaveBeenCalledWith('/v1/receptions-usinage2', expect.any(Object));
    expect(result.apiId).toBe(apiReception.id);
  });

  it('validates a reception through the real endpoint', async () => {
    mocks.post.mockResolvedValueOnce({});

    await receptionsService.validerReception(apiReception.id);

    expect(mocks.post).toHaveBeenCalledWith(`/v1/receptions-usinage2/${apiReception.id}/valider`, {
      derogationAdmin: false,
      derogationJustification: null
    });
  });

  it('cancels a reception through the real endpoint', async () => {
    mocks.post.mockResolvedValueOnce({});

    await receptionsService.annulerReception(apiReception.id);

    expect(mocks.post).toHaveBeenCalledWith(`/v1/receptions-usinage2/${apiReception.id}/annuler`);
  });
});
