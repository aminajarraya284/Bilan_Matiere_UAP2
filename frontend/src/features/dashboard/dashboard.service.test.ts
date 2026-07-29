import { afterEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { dashboardService } from './dashboard.service';

describe('dashboardService en mode mock', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    mocks.get.mockReset();
  });

  it("retourne les données de démonstration sans appel API", async () => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');

    const result = await dashboardService.getDashboardData('2026-06');

    expect(result.stats).toBeDefined();
    expect(mocks.get).not.toHaveBeenCalled();
  });
});
