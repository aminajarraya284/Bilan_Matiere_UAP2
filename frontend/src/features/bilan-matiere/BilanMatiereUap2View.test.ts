import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import type { BilanUap2ApiResponse } from '@/types/api.types';

const mocks = vi.hoisted(() => ({
  getBilanUap2Reel: vi.fn(),
  getPeriodesUap2Disponibles: vi.fn()
}));

vi.mock('./bilan.service', () => ({
  bilanService: {
    getBilanUap2Reel: mocks.getBilanUap2Reel,
    getPeriodesUap2Disponibles: mocks.getPeriodesUap2Disponibles
  }
}));

import BilanMatiereUap2View from './BilanMatiereUap2View.vue';

const response: BilanUap2ApiResponse = {
  periode: '2026-07',
  lignes: [
    {
      numeroOF: 'OF-REAL-001',
      codeArticle: 'ARTICLE-X3',
      designationArticle: 'Article API',
      codeTypeMatiere: 'M3010L0',
      periode: '2026-07',
      quantiteCtrlPieces: 100,
      rebutSciPieces: 1,
      rebutEmrPieces: 2,
      rebutTrnPieces: 3,
      quantiteSortieMpPieces: 94,
      poidsPhysiqueUnitaireKg: null,
      sortieMpKg: null,
      entreeOfPieces: 50,
      rebutUsinage2Pieces: 4,
      // Valeur volontairement différente d'un recalcul local : l'écran doit l'afficher telle quelle.
      ecartUap2Pieces: 41,
      tauxEcartPourcent: null,
      anomalies: ['POIDS_PHYSIQUE_NON_RESOLU', 'ECART_HORS_SEUIL'],
      statut: 'DonneesManquantes'
    }
  ],
  totalQuantiteCtrlPieces: 100,
  totalEcartUap2Pieces: 41,
  totalSortieMpKg: null,
  seuilEcartPourcent: 1.5,
  nombreLignes: 1,
  nombreLignesHorsSeuil: 1,
  nombreLignesDonneesManquantes: 1,
  nombreAnomalies: 2,
  total: 1,
  page: 1,
  pageSize: 1
};

const mountView = () =>
  mount(BilanMatiereUap2View, {
    global: {
      stubs: {
        PageHeader: {
          props: ['title', 'description'],
          template: '<header>{{ title }} {{ description }}</header>'
        },
        FormField: { template: '<label><slot /></label>' },
        LoadingState: { template: '<div data-testid="loading">Chargement du bilan UAP2...</div>' },
        BadgeStatus: {
          props: ['status'],
          template: '<span data-testid="status">{{ status }}</span>'
        }
      }
    }
  });

describe('BilanMatiereUap2View', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getPeriodesUap2Disponibles.mockResolvedValue(['2026-07']);
    mocks.getBilanUap2Reel.mockResolvedValue(response);
  });

  it('charge la dernière période via le contrat UAP2 réel', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(mocks.getPeriodesUap2Disponibles).toHaveBeenCalledOnce();
    expect(mocks.getBilanUap2Reel).toHaveBeenCalledWith('2026-07');
    expect((wrapper.get('input[type="month"]').element as HTMLInputElement).value).toBe('2026-07');
  });

  it('affiche les lignes et les KPI fournis par l’API', async () => {
    const wrapper = mountView();
    await flushPromises();

    const text = wrapper.text();
    expect(text).toContain('OF-REAL-001');
    expect(text).toContain('ARTICLE-X3');
    expect(text).toContain('Total CTRL');
    expect(text).toContain('Écart UAP2 global');
    expect(text).toContain('41');
  });

  it('conserve les unités pièces, pourcentage et kilogrammes séparées', async () => {
    const wrapper = mountView();
    await flushPromises();

    const text = wrapper.text();
    expect(text).toContain('CTRL (pcs)');
    expect(text).toContain('Sortie MP (pcs)');
    expect(text).toContain('Entrée OF (pcs)');
    expect(text).toContain('Rebut U2 (pcs)');
    expect(text).toContain('Écart (pcs)');
    expect(text).toContain('Sortie MP (kg)');
    expect(text).not.toContain('Écart en kg');
  });

  it('affiche un tiret pour les valeurs null sans les convertir en zéro', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Indisponible');
    expect(wrapper.text()).toContain('—');
  });

  it('affiche le statut et les anomalies du backend', async () => {
    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.get('[data-testid="status"]').text()).toBe('DonneesManquantes');
    expect(wrapper.text()).toContain('POIDS_PHYSIQUE_NON_RESOLU');
    expect(wrapper.text()).toContain('ECART_HORS_SEUIL');
  });

  it('n’affiche ni l’ancien diagnostic ni l’ancien contrat tout en kg', async () => {
    const wrapper = mountView();
    await flushPromises();

    const text = wrapper.text();
    expect(text).not.toContain("L'ancien contrat du Bilan UAP2");
    expect(text).not.toContain('entrées semi-finies');
    expect(text).not.toContain('Recalculer UAP2');
  });

  it('affiche un état de chargement avant la réponse des périodes', () => {
    mocks.getPeriodesUap2Disponibles.mockReturnValue(new Promise(() => {}));

    const wrapper = mountView();

    expect(wrapper.get('[data-testid="loading"]').text()).toContain('Chargement');
    expect(wrapper.text()).not.toContain('OF-REAL-001');
  });

  it('affiche une erreur utilisateur lorsque le bilan ne charge pas', async () => {
    mocks.getBilanUap2Reel.mockRejectedValue(new Error('détail technique'));

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Impossible de charger le bilan UAP2.');
    expect(wrapper.text()).not.toContain('détail technique');
  });

  it('affiche un vrai état vide sans KPI à zéro', async () => {
    mocks.getBilanUap2Reel.mockResolvedValue({ ...response, lignes: [], nombreLignes: 0 });

    const wrapper = mountView();
    await flushPromises();

    expect(wrapper.text()).toContain('Aucune donnée UAP2 pour la période sélectionnée.');
    expect(wrapper.text()).not.toContain('Total CTRL');
  });

  it('recharge le contrat réel lors d’un changement de période', async () => {
    const wrapper = mountView();
    await flushPromises();
    mocks.getBilanUap2Reel.mockClear();

    await wrapper.get('input[type="month"]').setValue('2026-06');
    await flushPromises();

    expect(mocks.getBilanUap2Reel).toHaveBeenCalledWith('2026-06');
  });

  it('nomme Actualiser le bouton qui relance uniquement le GET', async () => {
    const wrapper = mountView();
    await flushPromises();
    mocks.getBilanUap2Reel.mockClear();

    const button = wrapper.findAll('button').find((item) => item.text().includes('Actualiser'));
    expect(button).toBeDefined();
    await button!.trigger('click');
    await flushPromises();

    expect(mocks.getBilanUap2Reel).toHaveBeenCalledWith('2026-07');
  });

  it('affiche les résultats calculés par le backend sans recalculer la formule', async () => {
    const wrapper = mountView();
    await flushPromises();

    const row = wrapper.findAll('tbody tr')[0];
    expect(row.text()).toContain('41');
    expect(row.text()).not.toContain('40');
  });
});
