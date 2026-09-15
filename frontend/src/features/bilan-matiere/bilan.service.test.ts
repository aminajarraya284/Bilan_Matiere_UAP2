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

describe('bilanService - Bilan Fonderie réel', () => {
  const ligneApi = {
    typeMatiereId: '30000000-0000-0000-0000-000000000001',
    codeTypeMatiere: 'M3000L0',
    periode: '2026-07',
    stockDebutKg: null,
    sortiesMagasinX3Kg: 40,
    inputKg: null,
    stockFinKg: 90,
    dechetsFonderieKg: 12,
    receptionsUsinage2Kg: 25,
    outputKg: 127,
    ecartKg: null,
    tauxEcartPourcent: null,
    anomalies: ['STOCK_DEBUT_INDISPONIBLE'],
    statut: 'Incomplet'
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('VITE_BILAN_FONDERIE_SOURCE', 'api');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('appelle la route versionnée par période', async () => {
    mocks.apiGet.mockResolvedValue({ data: { periode: '2026-07', lignes: [ligneApi] } });

    await bilanService.getBilansFonderie('2026-07');

    expect(mocks.apiGet).toHaveBeenCalledWith('/v1/bilans/fonderie/2026-07');
    expect(mocks.calculateBilanFonderie).not.toHaveBeenCalled();
  });

  // RM-COM-003 : une composante inconnue reste inconnue jusqu'a l'ecran.
  it('conserve les composantes inconnues sans les ramener à zéro', async () => {
    mocks.apiGet.mockResolvedValue({ data: { periode: '2026-07', lignes: [ligneApi] } });

    const [report] = await bilanService.getBilansFonderie('2026-07');

    expect(report.encoursDebut).toBeNull();
    expect(report.inputTotal).toBeNull();
    expect(report.ecartKg).toBeNull();
    expect(report.encoursFinReel).toBe(90);
    expect(report.anomalies).toContain('STOCK_DEBUT_INDISPONIBLE');
    expect(report.statut).toBe('Incomplet');
  });

  it('filtre par type matière sans endpoint de calcul dédié', async () => {
    mocks.apiGet.mockResolvedValue({
      data: { periode: '2026-07', lignes: [ligneApi, { ...ligneApi, codeTypeMatiere: 'M3010L0' }] }
    });

    const resultats = await bilanService.calculerBilanFonderie('2026-07', 'M3010L0');

    expect(resultats).toHaveLength(1);
    expect(resultats[0].typeMatiere).toBe('M3010L0');
    expect(mocks.apiPost).not.toHaveBeenCalled();
  });

  it('transmet les deux dates au GET de consultation sans validation fictive', async () => {
    mocks.apiGet.mockResolvedValue({ data: {
      dateDebut: '2026-07-10', dateFin: '2026-07-20',
      bilan: { lignes: [{ ...ligneApi, statut: 'Complet' }] }
    } });
    const [report] = await bilanService.getBilans('2026-07', '2026-07-10', '2026-07-20');
    expect(mocks.apiGet).toHaveBeenCalledExactlyOnceWith('/v1/bilans/fonderie', {
      params: { dateDebut: '2026-07-10', dateFin: '2026-07-20' }
    });
    expect(report.statut).toBe('Complet');
    expect(mocks.apiPost).not.toHaveBeenCalled();
  });

  it('ne retombe pas sur le mois lorsque la date de fin manque', async () => {
    mocks.apiGet.mockRejectedValue(new Error('400'));
    await expect(bilanService.getBilans('2026-07', '2026-07-10')).rejects.toThrow('400');
    expect(mocks.apiGet).toHaveBeenCalledWith('/v1/bilans/fonderie', {
      params: { dateDebut: '2026-07-10', dateFin: undefined }
    });
  });

  it("propage l'erreur API sans repli sur les données de démonstration", async () => {
    const apiError = new Error('API indisponible');
    mocks.apiGet.mockRejectedValue(apiError);

    await expect(bilanService.getBilansFonderie('2026-07')).rejects.toBe(apiError);
    expect(mocks.calculateBilanFonderie).not.toHaveBeenCalled();
  });
});

describe('bilanService - Bilan UAP2 réel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('VITE_BILAN_UAP2_SOURCE', 'api');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('appelle la route versionnée et renvoie le contrat en pièces', async () => {
    const response = {
      periode: '2026-07',
      lignes: [{ numeroOF: 'OF1', ecartUap2Pieces: 15, tauxEcartPourcent: 1.5, sortieMpKg: 241.25 }],
      totalEcartUap2Pieces: 15,
      seuilEcartPourcent: 1.5
    };
    mocks.apiGet.mockResolvedValue({ data: response });

    await expect(bilanService.getBilanUap2Reel('2026-07')).resolves.toEqual(response);
    expect(mocks.apiGet).toHaveBeenCalledWith('/v1/bilans/uap2/2026-07');
  });

  // L'ancien contrat est tout en kg et porte un terme « entrées semi-finies » qui
  // n'existe pas dans la règle réelle : il ne peut pas être alimenté par l'API.
  it("refuse d'alimenter l'ancien contrat depuis l'API plutôt que de convertir à tort", async () => {
    await expect(bilanService.getBilansUap2('2026-07')).rejects.toThrow('getBilanUap2Reel');
    expect(mocks.apiGet).not.toHaveBeenCalled();
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

  it('refuse un intervalle en démonstration plutôt que de simuler son filtrage', async () => {
    await expect(bilanService.getBilans('2026-07', '2026-07-10', '2026-07-20')).rejects.toThrow();
    expect(mocks.apiGet).not.toHaveBeenCalled();
    expect(mocks.calculateBilanFonderie).not.toHaveBeenCalled();
  });

  it("ne contacte pas l'API pour le Bilan UAP2", async () => {
    await expect(bilanService.getBilansUap2('2026-07')).resolves.toEqual([]);
    expect(mocks.calculateBilanUap2).toHaveBeenCalled();
    expect(mocks.apiGet).not.toHaveBeenCalled();
    expect(mocks.apiPost).not.toHaveBeenCalled();
  });
});
