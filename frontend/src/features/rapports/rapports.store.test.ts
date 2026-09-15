import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { reportCatalogue, reportPreview } from './rapports.test.fixtures';

const mocks = vi.hoisted(() => ({
  getCatalogue: vi.fn(),
  preview: vi.fn(),
  exportCsv: vi.fn(),
  download: vi.fn()
}));

vi.mock('./rapports.service', () => ({
  rapportsService: {
    getCatalogue: mocks.getCatalogue,
    preview: mocks.preview,
    exportCsv: mocks.exportCsv
  }
}));
vi.mock('./exporter', () => ({ reportExporter: { download: mocks.download } }));

import { useRapportsStore } from './rapports.store';

describe('useRapportsStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mocks.getCatalogue.mockResolvedValue(reportCatalogue);
    mocks.preview.mockResolvedValue(reportPreview);
  });

  it('charge le catalogue et sélectionne le premier rapport autorisé', async () => {
    const store = useRapportsStore();

    await store.loadCatalogue();

    expect(store.catalogue).toEqual(reportCatalogue);
    expect(store.selectedCode).toBe('RPT-001');
  });

  it('charge un aperçu réel et conserve les filtres', async () => {
    const store = useRapportsStore();
    await store.loadCatalogue();
    store.filters.periode = '2026-01';

    await expect(store.loadPreview()).resolves.toBe(true);

    expect(store.preview).toEqual(reportPreview);
    expect(mocks.preview).toHaveBeenCalledWith({
      code: 'RPT-001',
      filters: store.filters,
      page: 1,
      pageSize: 50
    });
  });

  it("efface l'aperçu et affiche l'erreur sans fallback", async () => {
    const store = useRapportsStore();
    await store.loadCatalogue();
    await store.loadPreview();
    mocks.preview.mockRejectedValueOnce(new Error('SQL indisponible'));

    await expect(store.loadPreview()).resolves.toBe(false);

    expect(store.preview).toBeNull();
    expect(store.error).toBe('SQL indisponible');
  });

  it('empêche deux chargements simultanés', async () => {
    let resolvePreview!: (value: typeof reportPreview) => void;
    mocks.preview.mockReturnValueOnce(new Promise((resolve) => (resolvePreview = resolve)));
    const store = useRapportsStore();
    await store.loadCatalogue();

    const first = store.loadPreview();
    const second = await store.loadPreview();
    resolvePreview(reportPreview);

    expect(second).toBe(false);
    await expect(first).resolves.toBe(true);
    expect(mocks.preview).toHaveBeenCalledOnce();
  });

  it('exporte le fichier backend et empêche un double clic', async () => {
    const download = { blob: new Blob(['csv']), fileName: 'rapport.csv' };
    let resolveExport!: (value: typeof download) => void;
    mocks.exportCsv.mockReturnValueOnce(new Promise((resolve) => (resolveExport = resolve)));
    const store = useRapportsStore();
    await store.loadCatalogue();

    const first = store.exportCsv();
    const second = await store.exportCsv();
    resolveExport(download);

    expect(second).toBe(false);
    await expect(first).resolves.toBe(true);
    expect(mocks.download).toHaveBeenCalledWith(download);
  });

  it('change de page sans perdre les filtres', async () => {
    const store = useRapportsStore();
    await store.loadCatalogue();
    store.filters.periode = '2026-01';
    store.preview = { ...reportPreview, total: 120 };

    await store.goToPage(2);

    expect(store.page).toBe(2);
    expect(store.filters.periode).toBe('2026-01');
    expect(mocks.preview).toHaveBeenCalledWith(expect.objectContaining({ page: 2 }));
  });
});
