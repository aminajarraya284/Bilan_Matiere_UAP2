import { afterEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { synchronisationService } from './synchronisation.service';

describe('synchronisationService désactivé', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it("n'appelle aucune route de synchronisation", async () => {
    vi.stubEnv('VITE_SYNCHRONISATION_X3_SOURCE', 'disabled');

    await expect(synchronisationService.syncAll()).rejects.toThrow('Fonctionnalité non disponible');
    expect(mocks.get).not.toHaveBeenCalled();
    expect(mocks.post).not.toHaveBeenCalled();
  });
});
