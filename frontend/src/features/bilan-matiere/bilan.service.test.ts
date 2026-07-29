import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  apiGet: vi.fn(),
  apiPost: vi.fn(),
  getDataset: vi.fn(),
  calculateBilanFonderie: vi.fn(),
  calculateBilanUap2: vi.fn(),
  calculateBilanUsinage2: vi.fn()
}));

vi.mock('@/services/api', () => ({
  default: {
    get: mocks.apiGet,
    post: mocks.apiPost
  }
}));

vi.mock('@/services/testDataRepository.service', () => ({
  testDataRepository: {
    getDataset: mocks.getDataset
  }
}));

vi.mock('@/services/bilanCalculation.service', () => ({
  calculateBilanFonderie: mocks.calculateBilanFonderie,
  calculateBilanUap2: mocks.calculateBilanUap2,
  calculateBilanUsinage2: mocks.calculateBilanUsinage2
}));

import { bilanService } from './bilan.service';

describe('bilanService - Bilan Usinage 2 réel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('VITE_BILAN_U2_SOURCE', 'api');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('appelle uniquement le endpoint backend officiel', async () => {
    const response = {
      periode: '2026-07',
      lignes: [],
      quantiteTotaleReceptionneePieces: 0,
      nombreLignes: 0,
      nombreLignesDonneesManquantes: 0
    };
    mocks.apiGet.mockResolvedValue({ data: response });

    await expect(bilanService.getBilansUsinage('2026-07')).resolves.toEqual(response);

    expect(mocks.apiGet).toHaveBeenCalledWith('/v1/bilans/usinage2/2026-07');
    expect(mocks.getDataset).not.toHaveBeenCalled();
    expect(mocks.calculateBilanUsinage2).not.toHaveBeenCalled();
  });

  it("propage l'erreur API sans fallback vers les mocks", async () => {
    const apiError = new Error('API indisponible');
    mocks.apiGet.mockRejectedValue(apiError);

    await expect(bilanService.getBilansUsinage('2026-07')).rejects.toBe(apiError);

    expect(mocks.getDataset).not.toHaveBeenCalled();
    expect(mocks.calculateBilanUsinage2).not.toHaveBeenCalled();
  });
});

describe('bilanService - bilans de démonstration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('VITE_BILAN_FONDERIE_SOURCE', 'mock');
    vi.stubEnv('VITE_BILAN_UAP2_SOURCE', 'mock');
    mocks.getDataset.mockReturnValue({ source: 'test' });
    mocks.calculateBilanFonderie.mockReturnValue([]);
    mocks.calculateBilanUap2.mockReturnValue([]);
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("ne contacte pas l'API pour le Bilan Fonderie", async () => {
    await expect(bilanService.getBilansFonderie('2026-07')).resolves.toEqual([]);
    expect(mocks.calculateBilanFonderie).toHaveBeenCalled();
    expect(mocks.apiGet).not.toHaveBeenCalled();
    expect(mocks.apiPost).not.toHaveBeenCalled();
  });

  it("ne contacte pas l'API pour le Bilan UAP2", async () => {
    await expect(bilanService.getBilansUap2('2026-07')).resolves.toEqual([]);
    expect(mocks.calculateBilanUap2).toHaveBeenCalled();
    expect(mocks.apiGet).not.toHaveBeenCalled();
    expect(mocks.apiPost).not.toHaveBeenCalled();
  });
});
