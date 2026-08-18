import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia, type Pinia } from 'pinia';
import { createAlerteDetail, createPagedAlertes } from './alertes.test.fixtures';

const mocks = vi.hoisted(() => ({
  search: vi.fn(),
  get: vi.fn(),
  recalculer: vi.fn(),
  acquitter: vi.fn(),
  resoudre: vi.fn(),
  ignorer: vi.fn(),
  roles: ['Admin'] as string[]
}));

vi.mock('./alertes.service', () => ({
  alertesService: {
    search: mocks.search,
    get: mocks.get,
    recalculer: mocks.recalculer,
    acquitter: mocks.acquitter,
    resoudre: mocks.resoudre,
    ignorer: mocks.ignorer
  }
}));

vi.mock('@/features/auth/auth.store', () => ({ useAuthStore: () => ({ roles: mocks.roles }) }));

import AlertesView from './AlertesView.vue';

let pinia: Pinia;
const mountView = () =>
  mount(AlertesView, {
    global: {
      plugins: [pinia],
      stubs: {
        RouterLink: { props: ['to'], template: '<a :data-destination="to"><slot /></a>' }
      }
    }
  });

const findButton = (wrapper: ReturnType<typeof mountView>, label: string) =>
  wrapper.findAll('button').find((button) => button.text().includes(label));

describe('AlertesView', () => {
  beforeEach(() => {
    localStorage.clear();
    pinia = createPinia();
    setActivePinia(pinia);
    vi.clearAllMocks();
    mocks.roles.splice(0, mocks.roles.length, 'Admin');
    mocks.search.mockResolvedValue(createPagedAlertes());
  });

  it("affiche l'état de chargement", async () => {
    mocks.search.mockReturnValue(new Promise(() => undefined));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.find('[role="status"]').exists()).toBe(true);
  });

  it("affiche l'erreur API et aucune alerte fictive", async () => {
    mocks.search.mockRejectedValue(new Error('API alertes indisponible'));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('API alertes indisponible');
    expect(wrapper.text()).not.toContain('U2_QUANTITE_NON_RECEPTIONNEE');
  });

  it("affiche l'état vide explicite", async () => {
    mocks.search.mockResolvedValue(createPagedAlertes([]));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Aucune alerte pour ces filtres');
    expect(wrapper.text()).toContain('Aucune donnée fictive n’est affichée');
  });

  it('affiche la table persistante, les statuts et les unités sans recalcul métier', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('U2_QUANTITE_NON_RECEPTIONNEE');
    expect(wrapper.text()).toContain('Avertissement');
    expect(wrapper.text()).toContain('Ouverte');
    expect(wrapper.text()).not.toContain('Math.random');
  });

  it('ouvre le détail et formate les données associées', async () => {
    const detail = createAlerteDetail();
    mocks.get.mockResolvedValue(detail);
    const wrapper = mountView();
    await flushPromises();

    await findButton(wrapper, 'Détail')!.trigger('click');
    await flushPromises();

    expect(wrapper.get('[role="dialog"]').text()).toContain(detail.description);
    expect(wrapper.get('[role="dialog"]').text()).toContain('quantitePieces');
  });

  it('exige un commentaire avant Ignorer sans appeler le service', async () => {
    mocks.get.mockResolvedValue(createAlerteDetail());
    const wrapper = mountView();
    await flushPromises();
    await findButton(wrapper, 'Détail')!.trigger('click');
    await flushPromises();

    await findButton(wrapper, 'Ignorer')!.trigger('click');

    expect(wrapper.get('[role="alert"]').text()).toContain('justification est obligatoire');
    expect(mocks.ignorer).not.toHaveBeenCalled();
  });

  it('rend une alerte résolue en lecture seule', async () => {
    mocks.get.mockResolvedValue(createAlerteDetail({ statut: 'Resolue' }));
    const wrapper = mountView();
    await flushPromises();
    await findButton(wrapper, 'Détail')!.trigger('click');
    await flushPromises();

    const dialog = wrapper.get('[role="dialog"]');
    expect(dialog.text()).toContain('Résolue');
    expect(dialog.text()).not.toContain('Acquitter');
    expect(dialog.text()).not.toContain('Ignorer');
  });
});
