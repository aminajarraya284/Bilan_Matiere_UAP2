import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { createDashboardResponse } from './dashboard.test.fixtures';

const mocks = vi.hoisted(() => ({ getDashboard: vi.fn() }));

vi.mock('./dashboard.service', async (importOriginal) => {
  const original = await importOriginal<typeof import('./dashboard.service')>();
  return {
    ...original,
    dashboardService: { getDashboard: mocks.getDashboard }
  };
});

import { useDashboardStore } from './dashboard.store';

describe('useDashboardStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mocks.getDashboard.mockReset();
  });

  it("expose un état initial sans ancienne donnée ni erreur", () => {
    const store = useDashboardStore();

    expect(store.data).toBeNull();
    expect(store.loading).toBe(false);
    expect(store.error).toBeNull();
    expect(store.lastLoadedAt).toBeNull();
  });

  it('charge les données et mémorise la période', async () => {
    const payload = createDashboardResponse();
    mocks.getDashboard.mockResolvedValue(payload);
    const store = useDashboardStore();

    await store.load('2026-01');

    expect(mocks.getDashboard).toHaveBeenCalledWith('2026-01');
    expect(store.data).toEqual(payload);
    expect(store.periode).toBe('2026-01');
    expect(store.loading).toBe(false);
    expect(store.lastLoadedAt).not.toBeNull();
  });

  it("vide l'ancienne donnée et expose l'erreur après un échec", async () => {
    const store = useDashboardStore();
    mocks.getDashboard.mockResolvedValueOnce(createDashboardResponse()).mockRejectedValueOnce(new Error('API en erreur'));
    await store.load('2026-01');

    await store.load('2026-02');

    expect(store.data).toBeNull();
    expect(store.error).toBe('API en erreur');
    expect(store.loading).toBe(false);
    expect(store.lastLoadedAt).toBeNull();
  });

  it("refuse une période vide dans l'état d'erreur sans appeler l'API", async () => {
    const store = useDashboardStore();

    await store.load('');

    expect(store.data).toBeNull();
    expect(store.error).toContain('AAAA-MM');
    expect(store.loading).toBe(false);
    expect(mocks.getDashboard).not.toHaveBeenCalled();
  });

  it('conserve une réponse partielle sans la recalculer', async () => {
    const partial = createDashboardResponse({
      statutGlobal: 'Partiel',
      fonderie: {
        ...createDashboardResponse().fonderie,
        ecart: null,
        statut: 'Partiel',
        nombreDonneesManquantes: 1
      }
    });
    mocks.getDashboard.mockResolvedValue(partial);
    const store = useDashboardStore();

    await store.load('2026-01');

    expect(store.data?.fonderie.ecart).toBeNull();
    expect(store.data?.fonderie.statut).toBe('Partiel');
  });

  it('ignore une réponse obsolète lorsque la période change rapidement', async () => {
    let resolveFirst!: (value: ReturnType<typeof createDashboardResponse>) => void;
    let resolveSecond!: (value: ReturnType<typeof createDashboardResponse>) => void;
    mocks.getDashboard
      .mockImplementationOnce(
        () => new Promise<ReturnType<typeof createDashboardResponse>>((resolve) => (resolveFirst = resolve))
      )
      .mockImplementationOnce(
        () => new Promise<ReturnType<typeof createDashboardResponse>>((resolve) => (resolveSecond = resolve))
      );
    const store = useDashboardStore();

    const first = store.load('2026-01');
    const second = store.load('2026-02');
    resolveSecond(createDashboardResponse({ periode: '2026-02' }));
    await second;
    resolveFirst(createDashboardResponse({ periode: '2026-01' }));
    await first;

    expect(store.data?.periode).toBe('2026-02');
    expect(store.periode).toBe('2026-02');
  });

  it('refresh recharge la période courante', async () => {
    mocks.getDashboard.mockResolvedValue(createDashboardResponse());
    const store = useDashboardStore();
    store.setPeriode('2026-01');

    await store.refresh();

    expect(mocks.getDashboard).toHaveBeenCalledWith('2026-01');
  });

  it('reset annule logiquement la requête et efface tout état', async () => {
    mocks.getDashboard.mockResolvedValue(createDashboardResponse());
    const store = useDashboardStore();
    await store.load('2026-01');

    store.reset();

    expect(store.data).toBeNull();
    expect(store.error).toBeNull();
    expect(store.loading).toBe(false);
    expect(store.lastLoadedAt).toBeNull();
  });
});
