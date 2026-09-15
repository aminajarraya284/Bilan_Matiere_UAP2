import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { auditListItem, createAuditDetail, createPagedAudit } from './audit.test.fixtures';

const service = vi.hoisted(() => ({ search: vi.fn(), getDetail: vi.fn() }));
vi.mock('./audit.service', () => ({ auditService: service }));

import { useAuditStore } from './audit.store';

describe('useAuditStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    service.search.mockResolvedValue(createPagedAudit());
  });

  it('charge la liste, le total et la dernière activité', async () => {
    const store = useAuditStore();

    await store.load();

    expect(store.items).toEqual([auditListItem]);
    expect(store.total).toBe(1);
    expect(store.derniereActiviteUtc).toBe(auditListItem.dateUtc);
    expect(service.search).toHaveBeenCalledWith({ filters: store.filters, page: 1, pageSize: 50 });
  });

  it("efface l'ancienne liste et expose l'erreur sans fallback", async () => {
    const store = useAuditStore();
    await store.load();
    service.search.mockRejectedValueOnce(new Error('SQL audit indisponible'));

    await store.load();

    expect(store.items).toEqual([]);
    expect(store.total).toBe(0);
    expect(store.derniereActiviteUtc).toBeNull();
    expect(store.error).toBe('SQL audit indisponible');
  });

  it('applique les filtres, remet la page à un et les conserve', async () => {
    const store = useAuditStore();
    store.page = 3;
    const next = { ...store.filters, action: 'Validation' as const, utilisateur: 'admin' };

    await store.setFilters(next);

    expect(store.page).toBe(1);
    expect(store.filters).toEqual(next);
    expect(service.search).toHaveBeenCalledWith({ filters: next, page: 1, pageSize: 50 });
  });

  it('change de page dans les bornes disponibles', async () => {
    service.search.mockResolvedValue({ ...createPagedAudit(), total: 120 });
    const store = useAuditStore();
    await store.load();

    await store.changePage(2);

    expect(store.page).toBe(2);
    expect(service.search).toHaveBeenLastCalledWith({ filters: store.filters, page: 2, pageSize: 50 });
  });

  it('charge un détail puis le ferme', async () => {
    const detail = createAuditDetail();
    service.getDetail.mockResolvedValue(detail);
    const store = useAuditStore();

    await store.loadDetail(detail.id);
    expect(store.selected).toEqual(detail);

    store.closeDetail();
    expect(store.selected).toBeNull();
  });

  it('réinitialise tous les filtres', async () => {
    const store = useAuditStore();
    store.filters.action = 'Validation';
    store.filters.recherche = 'OF-1';

    await store.resetFilters();

    expect(store.filters.action).toBe('');
    expect(store.filters.recherche).toBe('');
    expect(store.page).toBe(1);
  });
});
