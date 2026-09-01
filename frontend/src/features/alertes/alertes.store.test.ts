import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { createAlerteDetail, createPagedAlertes } from './alertes.test.fixtures';

const service = vi.hoisted(() => ({
  search: vi.fn(),
  get: vi.fn(),
  recalculer: vi.fn(),
  acquitter: vi.fn(),
  resoudre: vi.fn(),
  ignorer: vi.fn()
}));

vi.mock('./alertes.service', () => ({ alertesService: service }));

import { useAlertesStore } from './alertes.store';

describe('useAlertesStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    service.search.mockResolvedValue(createPagedAlertes());
  });

  it("n'impose aucune période par défaut afin de ne pas masquer les alertes existantes", async () => {
    const store = useAlertesStore();

    // Le mois courant n'est pas nécessairement une période portant des données :
    // filtrer d'office dessus ferait passer une page peuplée pour une page vide.
    expect(store.filters.periode).toBe('');

    await store.load();

    expect(service.search).toHaveBeenCalledWith({
      filters: expect.objectContaining({ periode: '' }),
      page: 1,
      pageSize: 50
    });
  });

  it('refuse un recalcul sans période et n’appelle pas l’API', async () => {
    const store = useAlertesStore();

    const ok = await store.recalculer();

    expect(ok).toBe(false);
    expect(service.recalculer).not.toHaveBeenCalled();
    expect(store.error).toBe('Sélectionnez une période avant de lancer un recalcul.');
  });

  it('réinitialise les filtres vers « toutes périodes »', async () => {
    const store = useAlertesStore();
    store.filters.periode = '2026-07';
    store.filters.statut = 'Ouverte';

    await store.resetFilters();

    expect(store.filters.periode).toBe('');
    expect(store.filters.statut).toBe('');
  });

  it('charge la liste, le résumé et la dernière détection', async () => {
    const store = useAlertesStore();
    store.filters.periode = '2026-01';

    await store.load();

    expect(store.items).toHaveLength(1);
    expect(store.total).toBe(1);
    expect(store.summary.ouvertes).toBe(1);
    expect(store.derniereDetectionLeUtc).toBe('2026-01-31T12:00:00Z');
    expect(service.search).toHaveBeenCalledWith({ filters: store.filters, page: 1, pageSize: 50 });
  });

  it("efface l'ancienne liste et expose l'erreur sans fallback", async () => {
    const store = useAlertesStore();
    await store.load();
    service.search.mockRejectedValueOnce(new Error('SQL indisponible'));

    await store.load();

    expect(store.items).toEqual([]);
    expect(store.total).toBe(0);
    expect(store.error).toBe('SQL indisponible');
  });

  it('applique les filtres, remet la page à un et les conserve au rafraîchissement', async () => {
    const store = useAlertesStore();
    store.page = 3;
    const next = {
      ...store.filters,
      periode: '2026-01',
      statut: 'Acquittee' as const,
      numeroOF: 'OF-22'
    };

    await store.applyFilters(next);
    await store.load();

    expect(store.page).toBe(1);
    expect(store.filters).toEqual(next);
    expect(service.search).toHaveBeenLastCalledWith({ filters: next, page: 1, pageSize: 50 });
  });

  it('charge un détail puis le ferme', async () => {
    const detail = createAlerteDetail();
    service.get.mockResolvedValue(detail);
    const store = useAlertesStore();

    await store.loadDetail(detail.id);
    expect(store.selected).toEqual(detail);

    store.closeDetail();
    expect(store.selected).toBeNull();
  });

  it('empêche un double traitement et rafraîchit après succès', async () => {
    const detail = createAlerteDetail({ statut: 'Acquittee' });
    let resolveAction!: (value: typeof detail) => void;
    service.acquitter.mockReturnValue(new Promise((resolve) => (resolveAction = resolve)));
    const store = useAlertesStore();

    const first = store.acquitter(detail.id, null);
    const second = await store.acquitter(detail.id, null);
    resolveAction(detail);

    expect(second).toBe(false);
    await expect(first).resolves.toBe(true);
    expect(service.acquitter).toHaveBeenCalledOnce();
    expect(service.search).toHaveBeenCalledOnce();
  });

  it('recalcule la période courante puis recharge sans perdre les filtres', async () => {
    const result = {
      periode: '2026-01',
      recalculeLeUtc: '2026-01-31T12:00:00Z',
      nombreDetections: 1,
      nombreCreees: 1,
      nombreMisesAJour: 0,
      nombreResoluesAutomatiquement: 0
    };
    service.recalculer.mockResolvedValue(result);
    const store = useAlertesStore();
    store.filters.periode = '2026-01';
    store.filters.source = 'Bilan Usinage 2';

    await expect(store.recalculer()).resolves.toBe(true);

    expect(store.lastRecalculation).toEqual(result);
    expect(store.filters.source).toBe('Bilan Usinage 2');
    expect(service.recalculer).toHaveBeenCalledWith('2026-01');
    expect(service.search).toHaveBeenCalledOnce();
  });
});
