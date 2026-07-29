import { beforeEach, describe, expect, it, vi } from 'vitest';
import { sortiesX3Service } from './sortiesX3.service';

const apiGet = vi.hoisted(() => vi.fn());

vi.mock('@/services/api', () => ({
  default: {
    get: apiGet
  }
}));

describe('consommations composants par OF', () => {
  beforeEach(() => {
    apiGet.mockReset();
    apiGet.mockResolvedValue({ data: { items: [], total: 0, page: 1, pageSize: 100 } });
  });

  it('laisse le rapprochement X3 au backend', () => {
    expect('/v1/consommations-composants/imports').toContain('consommations-composants');
  });

  it('transmet la recherche OF au backend', async () => {
    await sortiesX3Service.getMouvements({ numeroOF: 'OF12345' });

    expect(apiGet).toHaveBeenCalledWith('/v1/consommations-composants', {
      params: { page: 1, pageSize: 100, numeroOF: 'OF12345' },
      signal: undefined
    });
  });

  it('omet le filtre OF lorsque la recherche est vide', async () => {
    await sortiesX3Service.getMouvements();

    expect(apiGet).toHaveBeenCalledWith('/v1/consommations-composants', {
      params: { page: 1, pageSize: 100 },
      signal: undefined
    });
  });
});
