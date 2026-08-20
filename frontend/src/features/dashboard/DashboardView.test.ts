import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia, type Pinia } from 'pinia';
import { createDashboardResponse } from './dashboard.test.fixtures';
import dashboardViewSource from './DashboardView.vue?raw';

const mocks = vi.hoisted(() => ({ getDashboard: vi.fn(), roles: [] as string[] }));

vi.mock('./dashboard.service', async (importOriginal) => {
  const original = await importOriginal<typeof import('./dashboard.service')>();
  return {
    ...original,
    dashboardService: { getDashboard: mocks.getDashboard }
  };
});

vi.mock('@/features/auth/auth.store', () => ({
  useAuthStore: () => ({ roles: mocks.roles })
}));

import DashboardView from './DashboardView.vue';

const setRole = (role: 'Admin' | 'ResponsableFonderie' | 'ResponsableUsinage2') => {
  mocks.roles.splice(0, mocks.roles.length, role);
};

let pinia: Pinia;
const RouterLinkTestStub = {
  props: ['to'],
  template: '<a :data-destination="to"><slot /></a>'
};

const mountView = () =>
  mount(DashboardView, {
    global: {
      plugins: [pinia],
      stubs: { RouterLink: RouterLinkTestStub }
    }
  });

describe('DashboardView', () => {
  beforeEach(() => {
    localStorage.clear();
    pinia = createPinia();
    setActivePinia(pinia);
    mocks.getDashboard.mockReset();
    mocks.roles.splice(0);
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      value: vi.fn().mockReturnValue({ matches: true })
    });
  });

  it("affiche l'état de chargement", async () => {
    mocks.getDashboard.mockReturnValue(new Promise(() => undefined));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.find('[aria-label="Chargement du tableau de bord"]').exists()).toBe(true);
  });

  it("affiche l'erreur et aucune ancienne donnée", async () => {
    mocks.getDashboard.mockRejectedValue(new Error('SQL indisponible'));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.get('[role="alert"]').text()).toContain('SQL indisponible');
    expect(wrapper.text()).toContain('Aucune ancienne donnée');
    expect(wrapper.find('[data-testid="dashboard-bilan-cards"]').exists()).toBe(false);
  });

  it("affiche l'état vide sans convertir les écarts absents en zéro", async () => {
    const emptySummary = {
      ...createDashboardResponse().fonderie,
      ecart: null,
      nombreLignes: 0,
      nombreOf: null,
      statut: 'NonDisponible' as const
    };
    mocks.getDashboard.mockResolvedValue(
      createDashboardResponse({
        statutGlobal: 'NonDisponible',
        fonderie: { ...emptySummary, code: 'FONDERIE', unite: 'kg' },
        usinage2: { ...emptySummary, code: 'USINAGE2', unite: 'pieces' },
        uap2: { ...emptySummary, code: 'UAP2', unite: 'pieces' }
      })
    );

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Aucune donnée disponible pour cette période.');
    expect(wrapper.text()).toContain('Non disponible');
  });

  it('affiche les trois cartes, les unités, les opérations et les avertissements réels', async () => {
    mocks.getDashboard.mockResolvedValue(createDashboardResponse());
    setRole('Admin');

    const wrapper = mountView();
    await flushPromises();

    const cards = wrapper.get('[data-testid="dashboard-bilan-cards"]');
    expect(cards.findAll('.dashboard-kpi')).toHaveLength(3);
    expect(cards.text()).toContain('Écart Fonderie');
    expect(cards.text()).toContain('kg');
    expect(cards.text()).toContain('pièces');
    expect(cards.text()).toContain('0');
    expect(wrapper.text()).toContain('Contrôles poids ambigus');
    expect(wrapper.text()).toContain('Non disponible');
    expect(wrapper.text()).toContain('Une référence OF est manquante.');
  });

  it('affiche une valeur null comme donnée manquante et conserve le statut partiel', async () => {
    const base = createDashboardResponse();
    mocks.getDashboard.mockResolvedValue(
      createDashboardResponse({
        statutGlobal: 'Partiel',
        fonderie: {
          ...base.fonderie,
          ecart: null,
          statut: 'Partiel',
          nombreDonneesManquantes: 1
        }
      })
    );

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Donnée manquante');
    expect(wrapper.text()).toContain('Calcul partiel');
  });

  it('ne montre au Responsable Fonderie que le lien de détail autorisé', async () => {
    mocks.getDashboard.mockResolvedValue(createDashboardResponse());
    setRole('ResponsableFonderie');

    const wrapper = mountView();
    await flushPromises();

    const cards = wrapper.get('[data-testid="dashboard-bilan-cards"]').findAll('.dashboard-kpi');
    expect(cards[0]!.text()).not.toContain('Détail non accessible');
    expect(cards[1]!.text()).toContain('Détail non accessible');
    expect(cards[2]!.text()).toContain('Détail non accessible');
    expect(dashboardViewSource).toContain("to: '/bilan-matiere?tab=fonderie'");
  });

  it('ne contient plus les anciens KPI ou routes Dashboard fictives', async () => {
    mocks.getDashboard.mockResolvedValue(createDashboardResponse());

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).not.toContain('Stock début');
    expect(wrapper.text()).not.toContain('Sorties magasin X3');
    expect(wrapper.html()).not.toContain('/dashboard/summary');
    expect(wrapper.html()).not.toContain('/dashboard/bilan-par-matiere');
  });

  it('aggregates real line counters in the status distribution', async () => {
    const base = createDashboardResponse();
    mocks.getDashboard.mockResolvedValue(createDashboardResponse({
      fonderie: { ...base.fonderie, nombreOk: 4, nombreAVerifier: 1, nombreAnomalies: 2 },
      usinage2: { ...base.usinage2, nombreOk: 3, nombreAVerifier: 2, nombreAnomalies: 0 },
      uap2: { ...base.uap2, nombreOk: 1, nombreAVerifier: 0, nombreAnomalies: 1 }
    }));

    const wrapper = mountView();
    await flushPromises();

    const chart = wrapper.get('.status-chart');
    expect(chart.text()).toContain('8');
    expect(chart.text()).toContain('3');
    expect(chart.text()).toContain('3');
  });
});
