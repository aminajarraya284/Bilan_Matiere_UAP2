import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
  assertApiSource: vi.fn()
}));

vi.mock('@/services/api', () => ({
  default: {
    get: mocks.get,
    put: mocks.put
  }
}));

vi.mock('@/config/dataSources', () => ({
  assertApiSource: mocks.assertApiSource
}));

import { adminService } from './admin.service';

describe('adminService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('charge les paramètres uniquement depuis /api/v1/parametres', async () => {
    const parameters = [{ cle: 'SEUIL', valeur: '1', description: '', modifiable: true, typeValeur: 'Décimal' }];
    mocks.get.mockResolvedValue({ data: parameters });

    await expect(adminService.getParameters()).resolves.toEqual(parameters);

    expect(mocks.assertApiSource).toHaveBeenCalledWith('administration');
    expect(mocks.get).toHaveBeenCalledWith('/v1/parametres');
  });

  it('met à jour un paramètre sur le contrat persistant', async () => {
    const updated = { cle: 'SEUIL', valeur: '2', description: '', modifiable: true, typeValeur: 'Décimal' };
    mocks.put.mockResolvedValue({ data: updated });

    await expect(adminService.updateParameter('SEUIL ÉCART', '2')).resolves.toEqual(updated);

    expect(mocks.assertApiSource).toHaveBeenCalledWith('administration');
    expect(mocks.put).toHaveBeenCalledWith('/v1/parametres/SEUIL%20%C3%89CART', { valeur: '2' });
  });

  it('charge les types matière depuis leur API sans fallback mock', async () => {
    const types = [{ id: '1', code: 'M3010L0', libelle: 'Laiton', estActif: true }];
    mocks.get.mockResolvedValue({ data: types });

    await expect(adminService.getMaterialTypes()).resolves.toEqual(types);

    expect(mocks.assertApiSource).toHaveBeenCalledWith('typesMatiere');
    expect(mocks.get).toHaveBeenCalledWith('/v1/types-matiere');
  });
});
