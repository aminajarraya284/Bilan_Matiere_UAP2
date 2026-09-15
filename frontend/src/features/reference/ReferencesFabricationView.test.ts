import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ReferencesFabricationView from './ReferencesFabricationView.vue';
import { useAuthStore } from '@/features/auth/auth.store';

const mocks = vi.hoisted(() => ({
  getGammes: vi.fn(),
  getNomenclatures: vi.fn(),
  importGammes: vi.fn(),
  importNomenclatures: vi.fn(),
  alert: vi.fn()
}));

vi.mock('./reference.service', async () => {
  const actual = await vi.importActual<typeof import('./reference.service')>('./reference.service');
  return {
    ...actual,
    referenceService: {
      getGammes: mocks.getGammes,
      getNomenclatures: mocks.getNomenclatures,
      importGammes: mocks.importGammes,
      importNomenclatures: mocks.importNomenclatures
    }
  };
});

vi.mock('@/composables/useAppDialog', () => ({
  useAppDialog: () => ({ alert: mocks.alert })
}));

const gammeRow = (codeArticle: string, id = 1) => ({
  id,
  codeArticle,
  designationArticle: `Article ${codeArticle}`,
  atelier: 'UAP2',
  poste: 'P1',
  groupePosteCharge: 'G1',
  numeroOperation: '5',
  designationOperation: 'Usinage',
  alternative: '1',
  dateFinValidite: '2099-12-31',
  source: 'API',
  statut: 'Actif'
});

const page = <T,>(items: T[], total: number) => ({ items, total });

const mountView = async () => {
  setActivePinia(createPinia());
  const authStore = useAuthStore();
  authStore.user = {
    id: '1',
    displayName: 'Administrateur SOPAL',
    fullName: 'Administrateur SOPAL',
    username: 'admin',
    email: 'admin@sopal.com',
    roles: ['Admin'],
    role: 'Admin',
    authenticationMode: 'development',
    status: 'Actif'
  };
  authStore.token = 'token';

  const wrapper = mount(ReferencesFabricationView, {
    global: { stubs: { PageHeader: true } }
  });
  await vi.dynamicImportSettled();
  await flush();
  return wrapper;
};

const flush = async () => {
  await Promise.resolve();
  await Promise.resolve();
  await new Promise((resolve) => setTimeout(resolve, 0));
};

// Le champ de recherche est temporise de 300 ms cote vue.
const waitForDebounce = async () => {
  await new Promise((resolve) => setTimeout(resolve, 360));
  await flush();
};

const openNomenclatureTab = async (wrapper: Awaited<ReturnType<typeof mountView>>) => {
  const tab = wrapper.findAll('button').find((b) => b.text() === 'Nomenclature');
  await tab!.trigger('click');
  await flush();
};

describe('ReferencesFabricationView — pagination serveur', () => {
  beforeEach(() => {
    vi.useRealTimers();
    mocks.getGammes.mockReset();
    mocks.getNomenclatures.mockReset();
    mocks.importGammes.mockReset();
    mocks.importNomenclatures.mockReset();
    mocks.alert.mockReset();
    mocks.getGammes.mockResolvedValue(page([], 0));
    mocks.getNomenclatures.mockResolvedValue(page([], 0));
  });

  it('does not render test data buttons and keeps GAMMEFAB import visible', async () => {
    const wrapper = await mountView();
    const text = wrapper.text();

    expect(text).not.toContain('Utiliser data test gamme');
    expect(text).toContain('Importer GAMMEFAB');
  });

  // 1 + 5. Au montage, seul l'onglet visible est chargé : une requête Gammes, zéro Nomenclature.
  it('ne charge que l onglet actif au montage', async () => {
    await mountView();

    expect(mocks.getGammes).toHaveBeenCalledTimes(1);
    expect(mocks.getGammes).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 100 }));
    expect(mocks.getNomenclatures).not.toHaveBeenCalled();
  });

  // 5. Le changement d'onglet ne charge que l'onglet nouvellement actif.
  it('charge les nomenclatures uniquement au passage sur leur onglet', async () => {
    const wrapper = await mountView();
    expect(mocks.getNomenclatures).not.toHaveBeenCalled();

    await openNomenclatureTab(wrapper);

    expect(mocks.getNomenclatures).toHaveBeenCalledTimes(1);
    expect(mocks.getGammes).toHaveBeenCalledTimes(1); // aucun rechargement des gammes
  });

  // 5 bis. Revenir sur un onglet déjà chargé ne relance aucune requête.
  it('ne recharge pas un onglet déjà chargé', async () => {
    const wrapper = await mountView();
    await openNomenclatureTab(wrapper);
    const gammeTab = wrapper.findAll('button').find((b) => b.text() === 'Gamme de fabrication');

    await gammeTab!.trigger('click');
    await flush();

    expect(mocks.getGammes).toHaveBeenCalledTimes(1);
    expect(mocks.getNomenclatures).toHaveBeenCalledTimes(1);
  });

  // 3. Passer à la page suivante déclenche exactement une requête, sur la bonne page.
  it('envoie une seule requête au passage à la page suivante', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('A')], 500));
    const wrapper = await mountView();
    mocks.getGammes.mockClear();

    const next = wrapper.findAll('button').find((b) => b.text() === 'Suivant');
    await next!.trigger('click');
    await flush();

    expect(mocks.getGammes).toHaveBeenCalledTimes(1);
    expect(mocks.getGammes).toHaveBeenCalledWith(expect.objectContaining({ page: 2, pageSize: 100 }));
  });

  // 4. Changer la taille de page revient page 1.
  it('revient page 1 lors d un changement de taille de page', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('A')], 500));
    const wrapper = await mountView();
    const next = wrapper.findAll('button').find((b) => b.text() === 'Suivant');
    await next!.trigger('click');
    await flush();
    mocks.getGammes.mockClear();

    await wrapper.get('#gamme-page-size').setValue('200');
    await flush();

    expect(mocks.getGammes).toHaveBeenCalledTimes(1);
    expect(mocks.getGammes).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 200 }));
  });

  // 6. Le total affiché provient de l'API.
  it('affiche le total renvoyé par l API', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('0101A03-1')], 106905));
    const wrapper = await mountView();

    expect(wrapper.text()).toContain('106');
    expect(wrapper.text()).toContain('référence(s)');
    expect(wrapper.text()).toContain('0101A03-1');
  });

  // 7 + 8. Une erreur est affichée, et « Réessayer » relance la page courante.
  it('affiche l erreur API puis relance la bonne page via Réessayer', async () => {
    mocks.getGammes.mockRejectedValueOnce(new Error('Service indisponible'));
    const wrapper = await mountView();

    expect(wrapper.text()).toContain('Service indisponible');

    mocks.getGammes.mockResolvedValue(page([gammeRow('OK')], 1));
    const retryButton = wrapper.findAll('button').find((b) => b.text() === 'Réessayer');
    await retryButton!.trigger('click');
    await flush();

    expect(mocks.getGammes).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    expect(wrapper.text()).not.toContain('Service indisponible');
    expect(wrapper.text()).toContain('0101A03-1'.slice(0, 0) + 'Article OK');
  });

  // 9. Une requête encore EN VOL est annulée par la suivante.
  //    Les boutons de pagination sont désactivés pendant un chargement : le seul
  //    chemin de navigation rapide réellement possible est la saisie de recherche.
  it('annule la requête de recherche précédente encore en vol', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('A')], 500));
    const wrapper = await mountView();

    // Première frappe : la requête reste en attente.
    let releaseFirst: ((value: unknown) => void) | undefined;
    mocks.getGammes.mockImplementationOnce(
      () => new Promise((resolve) => { releaseFirst = resolve; })
    );
    await wrapper.get('#gamme-code-search').setValue('010');
    await waitForDebounce();

    const inFlight = mocks.getGammes.mock.calls.at(-1)![0].signal as AbortSignal;
    expect(inFlight.aborted).toBe(false);

    // Deuxième frappe : la requête précédente doit être annulée.
    mocks.getGammes.mockResolvedValue(page([gammeRow('B')], 1));
    await wrapper.get('#gamme-code-search').setValue('0101');
    await waitForDebounce();

    expect(inFlight.aborted).toBe(true);
    releaseFirst?.(page([gammeRow('PERIME')], 999));
  });

  // 10. Une réponse tardive n'écrase jamais un résultat plus récent.
  it('ignore une réponse arrivée après annulation', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('A')], 500));
    const wrapper = await mountView();

    // Recherche lente, laissée en attente.
    let releaseStale: ((value: unknown) => void) | undefined;
    mocks.getGammes.mockImplementationOnce(
      () => new Promise((resolve) => { releaseStale = resolve; })
    );
    await wrapper.get('#gamme-code-search').setValue('010');
    await waitForDebounce();

    // Recherche suivante, plus récente, qui aboutit.
    mocks.getGammes.mockResolvedValue(page([gammeRow('RECENT')], 1));
    await wrapper.get('#gamme-code-search').setValue('0101');
    await waitForDebounce();

    // La requête lente répond enfin, avec des données périmées.
    releaseStale?.(page([gammeRow('PERIME')], 999));
    await flush();

    expect(wrapper.text()).toContain('Article RECENT');
    expect(wrapper.text()).not.toContain('Article PERIME');
  });

  // 11 + 12. Aucun appel en cascade : un total énorme ne provoque qu'une requête.
  it('ne déclenche aucun chargement en cascade malgré un total énorme', async () => {
    mocks.getGammes.mockResolvedValue(page([gammeRow('A')], 267645));
    await mountView();

    expect(mocks.getGammes).toHaveBeenCalledTimes(1);
  });

  it('recherche une nomenclature par article parent et revient page 1', async () => {
    mocks.getNomenclatures.mockResolvedValue(page([], 500));
    const wrapper = await mountView();
    await openNomenclatureTab(wrapper);

    const next = wrapper.findAll('button').find((button) => button.text() === 'Suivant');
    await next!.trigger('click');
    await flush();
    mocks.getNomenclatures.mockClear();

    await wrapper.get('#nomenclature-code-search').setValue(' 0101a03-1 ');
    await waitForDebounce();

    expect(mocks.getNomenclatures).toHaveBeenCalledTimes(1);
    expect(mocks.getNomenclatures).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        codeArticleParent: '0101A03-1'
      })
    );
  });
});
