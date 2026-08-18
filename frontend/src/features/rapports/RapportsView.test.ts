import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia, type Pinia } from 'pinia';
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

import RapportsView from './RapportsView.vue';

let pinia: Pinia;
const mountView = () => mount(RapportsView, { global: { plugins: [pinia] } });
const findButton = (wrapper: ReturnType<typeof mountView>, text: string) =>
  wrapper.findAll('button').find((button) => button.text().includes(text));

describe('RapportsView', () => {
  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);
    vi.clearAllMocks();
    mocks.getCatalogue.mockResolvedValue(reportCatalogue);
    mocks.preview.mockResolvedValue(reportPreview);
    mocks.exportCsv.mockResolvedValue({ blob: new Blob(['csv']), fileName: 'rapport.csv' });
  });

  it('affiche le catalogue backend et un aperçu avec les unités explicites', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('RPT-001');
    expect(wrapper.text()).toContain('Bilan Fonderie mensuel');
    expect(wrapper.text()).toContain('Stock début (kg)');
    expect(wrapper.text()).toContain('M3000');
    expect(wrapper.text()).toContain('Non renseigné');
    expect(wrapper.text()).toContain('Données partielles');
  });

  it("affiche l'erreur API et aucune donnée fictive", async () => {
    mocks.preview.mockRejectedValue(new Error('API rapports indisponible'));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.get('[role="alert"]').text()).toContain('API rapports indisponible');
    expect(wrapper.text()).not.toContain('M3000');
  });

  it('affiche un état vide explicite', async () => {
    mocks.preview.mockResolvedValue({ ...reportPreview, rows: [], total: 0 });

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Aucune donnée réelle ne correspond aux filtres sélectionnés');
  });

  it("exige le numéro d'OF pour le rapport détaillé", async () => {
    const wrapper = mountView();
    await flushPromises();

    await findButton(wrapper, 'RPT-004')!.trigger('click');
    await flushPromises();

    expect(wrapper.text()).toContain('Numéro OF » est obligatoire');
    expect(findButton(wrapper, 'Afficher')!.attributes('disabled')).toBeDefined();
  });

  it('déclenche uniquement le téléchargement CSV produit par le backend', async () => {
    const wrapper = mountView();
    await flushPromises();

    await findButton(wrapper, 'Exporter CSV')!.trigger('click');
    await flushPromises();

    expect(mocks.exportCsv).toHaveBeenCalledOnce();
    expect(mocks.download).toHaveBeenCalledWith(expect.objectContaining({ fileName: 'rapport.csv' }));
    expect(wrapper.text()).not.toContain('PDF');
    expect(wrapper.text()).not.toContain('Excel');
  });
});
