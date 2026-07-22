import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

const mocks = vi.hoisted(() => ({
  getTypesMatiere: vi.fn(),
  getDechetsFonderie: vi.fn(),
  getResumeDechetsFonderie: vi.fn(),
  createDechetFonderie: vi.fn(),
  updateDechetFonderie: vi.fn(),
  deleteDechetFonderie: vi.fn(),
  confirm: vi.fn(),
  alert: vi.fn()
}));

vi.mock('./dechets.service', () => ({
  dechetsService: {
    getTypesMatiere: mocks.getTypesMatiere,
    getDechetsFonderie: mocks.getDechetsFonderie,
    getResumeDechetsFonderie: mocks.getResumeDechetsFonderie,
    createDechetFonderie: mocks.createDechetFonderie,
    updateDechetFonderie: mocks.updateDechetFonderie,
    deleteDechetFonderie: mocks.deleteDechetFonderie
  }
}));

vi.mock('@/composables/useAppDialog', () => ({
  useAppDialog: () => ({ confirm: mocks.confirm, alert: mocks.alert })
}));

import DechetsFonderieView from './DechetsFonderieView.vue';

const material = {
  id: '30000000-0000-0000-0000-000000000001',
  code: 'M3000L0',
  libelle: 'Matière M3000L0',
  estActif: true,
  dateCreationUtc: '2026-01-01T00:00:00Z',
  dateModificationUtc: null
};

const waste = {
  id: 'f0000000-0000-0000-0000-000000000001',
  dateDeclaration: '2026-07-22',
  semaineIso: 30,
  typeMatiereId: material.id,
  typeMatiereCode: material.code,
  typeMatiereLibelle: material.libelle,
  typeDechet: 'Bavure' as const,
  poidsKg: 125.5,
  observation: 'Bavures triées',
  statut: 'Valide' as const,
  declarant: 'Responsable Fonderie',
  dateCreationUtc: '2026-07-22T08:00:00Z',
  dateModificationUtc: null
};

const summary = {
  periode: '2026-07',
  totauxParTypeMatiere: [
    {
      typeMatiereId: material.id,
      typeMatiereCode: material.code,
      totalBavureKg: 125.5,
      totalPoudreKg: 0,
      totalKg: 125.5
    }
  ],
  totalGeneralKg: 125.5
};

const mountView = async () => {
  const wrapper = mount(DechetsFonderieView, {
    global: {
      stubs: {
        PageHeader: true,
        BadgeStatus: { template: '<span><slot />{{ status }}</span>', props: ['status'] },
        LoadingState: { template: '<div>Chargement</div>' }
      }
    }
  });
  await flushPromises();
  return wrapper;
};

describe('DechetsFonderieView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem('sopal_auth_token', 'test-token');
    localStorage.setItem(
      'sopal_auth_user',
      JSON.stringify({
        id: 1,
        fullName: 'Administrateur SOPAL',
        username: 'admin',
        email: 'admin@sopal.com',
        role: 'Admin',
        status: 'Actif'
      })
    );
    setActivePinia(createPinia());
    mocks.getTypesMatiere.mockResolvedValue([material]);
    mocks.getDechetsFonderie.mockResolvedValue({ items: [waste], total: 1, page: 1, pageSize: 20 });
    mocks.getResumeDechetsFonderie.mockResolvedValue(summary);
    mocks.createDechetFonderie.mockResolvedValue(waste);
    mocks.deleteDechetFonderie.mockResolvedValue(undefined);
    mocks.confirm.mockResolvedValue(true);
  });

  it('loads material types and displays the API waste list', async () => {
    const wrapper = await mountView();

    expect(mocks.getTypesMatiere).toHaveBeenCalledOnce();
    expect(wrapper.get('[data-testid="material-select"]').text()).toContain('M3000L0');
    expect(wrapper.get('[data-testid="waste-row"]').text()).toContain('125,5 kg');
  });

  it('keeps a material loading error visible and retries only that request', async () => {
    mocks.getTypesMatiere.mockRejectedValueOnce(new Error('Service indisponible')).mockResolvedValueOnce([material]);
    const wrapper = await mountView();

    expect(wrapper.get('[data-testid="types-error"]').text()).toContain(
      'Impossible de charger les types matière. Service indisponible'
    );
    expect(wrapper.find('[data-testid="api-error"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="submit-waste"]').attributes('disabled')).toBeDefined();

    await wrapper.get('[data-testid="retry-types"]').trigger('click');
    await flushPromises();

    expect(mocks.getTypesMatiere).toHaveBeenCalledTimes(2);
    expect(wrapper.find('[data-testid="types-error"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="material-select"]').text()).toContain('M3000L0');
  });

  it('shows an explicit message when the API returns no active material type', async () => {
    mocks.getTypesMatiere.mockResolvedValueOnce([{ ...material, estActif: false }]);
    const wrapper = await mountView();

    expect(wrapper.get('[data-testid="types-empty"]').text()).toBe('Aucun type matière actif disponible');
    expect(wrapper.get('[data-testid="material-select"]').findAll('option')).toHaveLength(1);
    expect(wrapper.get('[data-testid="submit-waste"]').attributes('disabled')).toBeDefined();
  });

  it('clears the previous list, total and summary when a new data request fails', async () => {
    const wrapper = await mountView();
    expect(wrapper.findAll('[data-testid="waste-row"]')).toHaveLength(1);
    expect(wrapper.get('[data-testid="waste-total"]').text()).toContain('1 déclaration(s)');
    expect(wrapper.get('[data-testid="summary-total"]').text()).toContain('125,5 kg');

    mocks.getResumeDechetsFonderie.mockRejectedValueOnce(new Error('Résumé indisponible'));
    await wrapper.get('[data-testid="period-filter"]').setValue('2026-06');
    await new Promise((resolve) => setTimeout(resolve, 300));
    await flushPromises();

    expect(wrapper.get('[data-testid="api-error"]').text()).toContain('Résumé indisponible');
    expect(wrapper.find('[data-testid="waste-row"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="waste-total"]').text()).toContain('0 déclaration(s)');
    expect(wrapper.find('[data-testid="summary-total"]').exists()).toBe(false);
  });

  it('creates a waste with the allowed payload only', async () => {
    const wrapper = await mountView();
    await wrapper.get('[data-testid="date-input"]').setValue('2026-07-22');
    await wrapper.get('[data-testid="weight-input"]').setValue('125.5');
    await wrapper.get('[data-testid="observation-input"]').setValue('Bavures triées');
    await wrapper.get('[data-testid="waste-form"]').trigger('submit');
    await flushPromises();

    expect(mocks.createDechetFonderie).toHaveBeenCalledWith({
      dateDeclaration: '2026-07-22',
      typeMatiereId: material.id,
      typeDechet: 'Bavure',
      poidsKg: 125.5,
      observation: 'Bavures triées'
    });
    expect(wrapper.get('[data-testid="success-message"]').text()).toContain('enregistrée');
  });

  it('refuses a zero weight before calling the API', async () => {
    const wrapper = await mountView();
    await wrapper.get('[data-testid="weight-input"]').setValue('0');
    await wrapper.get('[data-testid="waste-form"]').trigger('submit');
    await flushPromises();

    expect(mocks.createDechetFonderie).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('strictement supérieur à zéro');
  });

  it('updates the ISO week automatically when the date changes', async () => {
    const wrapper = await mountView();

    await wrapper.get('[data-testid="date-input"]').setValue('2026-07-06');

    expect((wrapper.get('[data-testid="week-input"]').element as HTMLInputElement).value).toBe('S-28');
  });

  it('shows a normalized API validation error', async () => {
    mocks.createDechetFonderie.mockRejectedValueOnce({
      isAxiosError: true,
      response: { data: { title: 'Erreur de validation', errors: { poidsKg: ['Poids invalide.'] } } }
    });
    const wrapper = await mountView();
    await wrapper.get('[data-testid="weight-input"]').setValue('10');
    await wrapper.get('[data-testid="waste-form"]').trigger('submit');
    await flushPromises();

    expect(wrapper.get('[data-testid="api-error"]').text()).toContain('Erreur de validation');
    expect(wrapper.text()).toContain('Poids invalide');
  });

  it('deletes only after confirmation and reloads the list and summary', async () => {
    const wrapper = await mountView();
    const callsBeforeDelete = mocks.getDechetsFonderie.mock.calls.length;

    await wrapper.get('[data-testid="delete-waste"]').trigger('click');
    await flushPromises();

    expect(mocks.confirm).toHaveBeenCalledOnce();
    expect(mocks.deleteDechetFonderie).toHaveBeenCalledWith(waste.id);
    expect(mocks.getDechetsFonderie.mock.calls.length).toBeGreaterThan(callsBeforeDelete);
    expect(mocks.getResumeDechetsFonderie.mock.calls.length).toBeGreaterThan(1);
  });

  it('reloads the API list when the period filter changes', async () => {
    const wrapper = await mountView();
    await wrapper.get('[data-testid="period-filter"]').setValue('2026-06');
    await new Promise((resolve) => setTimeout(resolve, 300));
    await flushPromises();

    expect(mocks.getDechetsFonderie).toHaveBeenLastCalledWith(expect.objectContaining({ periode: '2026-06' }));
  });
});
