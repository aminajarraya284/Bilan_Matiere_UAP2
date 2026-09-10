import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';

const mocks = vi.hoisted(() => ({
  getReceptions: vi.fn(),
  annulerReception: vi.fn(),
  confirm: vi.fn()
}));
vi.mock('./receptions.service', () => ({ receptionsService: mocks }));
vi.mock('./bilanUsinage2.service', () => ({
  bilanUsinage2Service: {
    isAvailable: () => true,
    getBilan: async () => null
  }
}));
vi.mock('./alerteNonReception.service', () => ({ alerteNonReceptionService: { isAvailable: () => false } }));
vi.mock('@/features/auth/auth.store', () => ({ useAuthStore: () => ({ userRole: 'Admin' }) }));
vi.mock('@/composables/useAppDialog', () => ({ useAppDialog: () => ({ confirm: mocks.confirm }) }));
import ReceptionsUsinage2View from './ReceptionsUsinage2View.vue';

const reception = {
  id: 1,
  apiId: 'r1',
  numeroOF: 'OF-TEST',
  dateReception: '2026-07-01',
  codeArticle: 'ART1',
  designationArticle: 'Article test',
  codeTypeMatiere: 'M1',
  poidsTotalReceptionneKg: null,
  poidsPhysiqueUnitaireKg: null,
  nombrePiecesReceptionnees: 10,
  statut: 'Valide',
  atelierOrigine: '16',
  avertissements: ['Poids physique manquant']
};

describe('ReceptionsUsinage2View loading and missing weights', () => {
  let wrapper: VueWrapper;
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(console, 'error').mockImplementation(() => {});
    mocks.getReceptions.mockResolvedValue([reception]);
    mocks.annulerReception.mockResolvedValue(undefined);
    mocks.confirm.mockResolvedValue(true);
  });
  afterEach(() => {
    wrapper?.unmount();
    vi.restoreAllMocks();
  });
  const open = async () => {
    wrapper = mount(ReceptionsUsinage2View, { global: { stubs: { PageHeader: true } } });
    await flushPromises();
  };

  it('shows dashes for absent weights and displays the backend warning', async () => {
    await open();
    const cells = wrapper
      .findAll('tbody tr')
      .find((row) => row.text().includes('OF-TEST'))!
      .findAll('td');
    expect(cells[4].text()).toBe('—');
    expect(cells[5].text()).toBe('—');
    expect(wrapper.text()).toContain('Poids physique manquant');
  });

  it('shows a list loading error and retries successfully', async () => {
    mocks.getReceptions.mockRejectedValueOnce(new Error('Panne API'));
    await open();
    expect(wrapper.text()).toContain('Impossible de charger les réceptions');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Réessayer')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('OF-TEST');
    expect(wrapper.text()).not.toContain('Impossible de charger les réceptions');
    expect(mocks.getReceptions).toHaveBeenCalledTimes(2);
  });

  it('removes stale receptions when refreshing after cancellation fails', async () => {
    await open();
    mocks.getReceptions.mockRejectedValueOnce(new Error('Panne API'));
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Annuler')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Impossible de charger les réceptions');
    expect(wrapper.text()).not.toContain('OF-TEST');
  });
});
