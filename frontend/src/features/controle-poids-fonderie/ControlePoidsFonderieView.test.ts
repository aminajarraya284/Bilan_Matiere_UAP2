import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';

const mocks = vi.hoisted(() => ({ getAll: vi.fn(), getTypesMatiere: vi.fn(), annuler: vi.fn(), confirm: vi.fn() }));
vi.mock('./controlePoidsFonderie.service', () => ({ controlePoidsFonderieApiService: mocks }));
vi.mock('@/features/dechets-fonderie/dechets.service', () => ({ dechetsService: mocks }));
vi.mock('@/config/dataSources', () => ({ isApiSource: () => true }));
vi.mock('@/composables/useAppDialog', () => ({ useAppDialog: () => ({ confirm: mocks.confirm }) }));
import ControlePoidsFonderieView from './ControlePoidsFonderieView.vue';

describe('ControlePoidsFonderieView loading errors', () => {
  let wrapper: VueWrapper;
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(console, 'error').mockImplementation(() => {});
    mocks.getAll.mockResolvedValue([
      {
        id: 'c1',
        numeroOF: 'OF-CONTROLE',
        dateControle: '2026-07-01',
        codeArticle: 'ART1',
        poidsPhysiqueUnitaireKg: 2,
        statut: 'Valide'
      }
    ]);
    mocks.getTypesMatiere.mockResolvedValue([{ id: 'm1', code: 'M1', designation: 'Matière 1' }]);
    mocks.annuler.mockResolvedValue(undefined);
    mocks.confirm.mockResolvedValue(true);
  });
  afterEach(() => {
    wrapper?.unmount();
    vi.restoreAllMocks();
  });
  const open = async () => {
    wrapper = mount(ControlePoidsFonderieView, { global: { stubs: { PageHeader: true } } });
    await flushPromises();
  };

  it('shows a history error then reloads only the controls on retry', async () => {
    mocks.getAll.mockRejectedValueOnce(new Error('Panne API'));
    await open();
    expect(wrapper.text()).toContain('Impossible de charger les contrôles poids');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Réessayer')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('OF-CONTROLE');
    expect(wrapper.text()).not.toContain('Impossible de charger les contrôles poids');
    expect(mocks.getTypesMatiere).toHaveBeenCalledOnce();
  });

  it('shows a material loading error independently and retries that request', async () => {
    mocks.getTypesMatiere.mockRejectedValueOnce(new Error('Panne API'));
    await open();
    expect(wrapper.text()).toContain('Impossible de charger les types matière');
    expect(wrapper.text()).toContain('OF-CONTROLE');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Réessayer')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).not.toContain('Impossible de charger les types matière');
    expect(mocks.getTypesMatiere).toHaveBeenCalledTimes(2);
    expect(mocks.getAll).toHaveBeenCalledOnce();
  });

  it('clears stale history after a failed refresh', async () => {
    await open();
    mocks.getAll.mockRejectedValueOnce(new Error('Panne API'));
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Annuler')!
      .trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Impossible de charger les contrôles poids');
    expect(wrapper.text()).not.toContain('OF-CONTROLE');
  });
});
