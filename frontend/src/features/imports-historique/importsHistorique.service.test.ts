import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { importsHistoriqueService } from './importsHistorique.service';

describe('imports historique API service', () => {
  beforeEach(() => vi.clearAllMocks());

  it('loads the real imports diagnostic', async () => {
    mocks.get.mockResolvedValueOnce({
      data: { gammesActives: 1, nomenclaturesActives: 2, rapportsMachine: 3, derniersImportBatches: [] }
    });

    const result = await importsHistoriqueService.getDiagnostic();

    expect(result.gammesActives).toBe(1);
    expect(mocks.get).toHaveBeenCalledWith('/v1/diagnostics/imports-reels');
  });

  it('loads archived files for a batch', async () => {
    mocks.get.mockResolvedValueOnce({ data: [] });

    await importsHistoriqueService.getFiles('batch-1');

    expect(mocks.get).toHaveBeenCalledWith('/v1/imports/batch-1/files');
  });
});
