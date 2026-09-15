import { afterEach, describe, expect, it, vi } from 'vitest';
import { createDashboardResponse } from './dashboard.test.fixtures';

const mocks = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock('@/services/api', () => ({ default: mocks }));

import { dashboardService } from './dashboard.service';

describe('dashboardService', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    mocks.get.mockReset();
  });

  it('appelle le seul endpoint réel avec la période', async () => {
    const payload = createDashboardResponse();
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'api');
    mocks.get.mockResolvedValue({ data: payload });

    await expect(dashboardService.getDashboard('2026-01')).resolves.toEqual(payload);
    expect(mocks.get).toHaveBeenCalledOnce();
    expect(mocks.get).toHaveBeenCalledWith('/v1/dashboard', { params: { periode: '2026-01' } });
  });

  it("propage l'erreur API sans basculer vers le mock", async () => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'api');
    mocks.get.mockRejectedValue(new Error('SQL indisponible'));

    await expect(dashboardService.getDashboard('2026-01')).rejects.toThrow('SQL indisponible');
    expect(mocks.get).toHaveBeenCalledOnce();
  });

  it('utilise le mock vide uniquement lorsque le mode mock est explicite en développement', async () => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');

    const result = await dashboardService.getDashboard('2026-01');

    expect(result.statutGlobal).toBe('NonDisponible');
    expect(result.fonderie.ecart).toBeNull();
    expect(mocks.get).not.toHaveBeenCalled();
  });

  it.each(['', '2026', '01-2026', '2026-13'])('refuse la période invalide %j', async (periode) => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'api');

    await expect(dashboardService.getDashboard(periode)).rejects.toThrow('AAAA-MM');
    expect(mocks.get).not.toHaveBeenCalled();
  });
});
