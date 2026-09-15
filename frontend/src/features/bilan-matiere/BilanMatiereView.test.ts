import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';

const mocks = vi.hoisted(() => ({ getBilans: vi.fn(), enregistrer: vi.fn(), confirm: vi.fn() }));
vi.mock('./bilan.service', () => ({ bilanService: { getBilans: mocks.getBilans } }));
vi.mock('./rebutsUsinage2.service', () => ({ rebutsUsinage2Service: { isAvailable: () => false } }));
vi.mock('@/features/archivage/archivage.service', () => ({ archivageService: { enregistrer: mocks.enregistrer } }));
vi.mock('@/features/auth/auth.store', () => ({ useAuthStore: () => ({ userRole: 'Admin', roles: ['Admin'] }) }));
vi.mock('@/composables/useAppDialog', () => ({ useAppDialog: () => ({ confirm: mocks.confirm }) }));
vi.mock('vue-router', async (importOriginal) => ({
  ...await importOriginal<typeof import('vue-router')>(), useRoute: () => ({ query: { tab: 'fonderie' } })
}));
import BilanMatiereView from './BilanMatiereView.vue';

const report = {
  bilanKey: 'range|M-TEST', periode: 'range', typeMatiere: 'M-TEST',
  encoursDebut: null, sortiesX3: 45, inputTotal: null, encoursFinReel: null,
  dechetsFonderie: 45, receptionsUsinage2: 45, outputTotal: null, ecartKg: null, tauxEcart: null,
  statut: 'Incomplet', anomalies: ['STOCK_DEBUT_INDISPONIBLE', 'STOCK_FIN_INDISPONIBLE']
};

describe('consultation Fonderie par intervalle', () => {
  let wrapper: VueWrapper;
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getBilans.mockResolvedValue([report]);
    mocks.confirm.mockResolvedValue(true);
    mocks.enregistrer.mockResolvedValue(null);
  });
  afterEach(() => wrapper?.unmount());
  const open = async () => {
    wrapper = mount(BilanMatiereView, { global: { stubs: {
      PageHeader: true, DataSourceBadge: true, LoadingState: true, BilanMatiereUap2View: true,
      RouterLink: { template: '<a><slot /></a>' }
    } } });
    await flushPromises();
  };
  const click = async (label: string) => {
    await wrapper.findAll('button').find((button) => button.text() === label)!.trigger('click');
    await flushPromises();
  };

  it('garde les deux dates visibles, transmet leur valeur et actualise une seule fois', async () => {
    await open();
    expect(wrapper.findAll('input[type="date"]')).toHaveLength(2);
    expect(wrapper.text()).not.toContain('Valider tout');
    expect(wrapper.text()).not.toContain('Calculer le Bilan Fonderie');
    await wrapper.get('input[aria-label="Date début"]').setValue('2026-07-10');
    await wrapper.get('input[aria-label="Date fin"]').setValue('2026-07-20');
    await flushPromises();
    expect(mocks.getBilans.mock.lastCall?.slice(1)).toEqual(['2026-07-10', '2026-07-20']);
    mocks.getBilans.mockClear();
    await click('Actualiser le bilan');
    expect(mocks.getBilans).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain('STOCK_DEBUT_INDISPONIBLE');
    expect(wrapper.find('tbody tr').text()).toContain('—');
    expect(wrapper.text()).toContain('Incomplet');
  });

  it('efface les anciennes lignes en erreur puis permet de réessayer', async () => {
    await open();
    expect(wrapper.text()).toContain('M-TEST');
    mocks.getBilans.mockRejectedValueOnce(new Error('API hors ligne'));
    await click('Actualiser le bilan');
    expect(wrapper.get('[role="alert"]').text()).toContain("n'a pas pu être chargé");
    expect(wrapper.text()).not.toContain('M-TEST');
    expect(wrapper.find('table').exists()).toBe(false);
    await click('Réessayer');
    expect(wrapper.text()).toContain('M-TEST');
    expect(wrapper.find('[role="alert"]').exists()).toBe(false);
  });

  it('refuse les bornes manquantes ou inversées sans conserver le résultat précédent', async () => {
    await open();
    mocks.getBilans.mockClear();
    await wrapper.get('input[aria-label="Date fin"]').setValue('');
    await flushPromises();
    expect(mocks.getBilans).not.toHaveBeenCalled();
    expect(wrapper.text()).not.toContain('M-TEST');
    await wrapper.get('input[aria-label="Date début"]').setValue('2026-07-21');
    await wrapper.get('input[aria-label="Date fin"]').setValue('2026-07-20');
    await flushPromises();
    expect(mocks.getBilans).not.toHaveBeenCalled();
    expect(wrapper.get('[role="alert"]').text()).toContain('antérieure ou égale');
  });

  it('ignore une réponse ancienne arrivée après la nouvelle sélection', async () => {
    await open();
    let terminer!: (value: typeof report[]) => void;
    mocks.getBilans.mockImplementationOnce(() => new Promise((resolve) => { terminer = resolve; }));
    await wrapper.get('input[aria-label="Date début"]').setValue('2026-07-10');
    await wrapper.get('input[aria-label="Date fin"]').setValue('2026-07-20');
    await flushPromises();
    terminer([{ ...report, typeMatiere: 'ANCIEN-RESULTAT' }]);
    await flushPromises();
    expect(wrapper.text()).toContain('M-TEST');
    expect(wrapper.text()).not.toContain('ANCIEN-RESULTAT');
  });

  it('archive uniquement le mois explicitement choisi indépendamment des dates live', async () => {
    await open();
    await wrapper.get('input[aria-label="Mois à archiver"]').setValue('2026-08');
    await wrapper.get('input[aria-label="Date début"]').setValue('2026-07-10');
    await wrapper.get('input[aria-label="Date fin"]').setValue('2026-07-20');
    await flushPromises();
    await click('Enregistrer la période');
    expect(mocks.enregistrer).toHaveBeenCalledExactlyOnceWith('2026-08');
    expect(mocks.confirm.mock.lastCall?.[0]).toContain('2026-08');
  });
});
