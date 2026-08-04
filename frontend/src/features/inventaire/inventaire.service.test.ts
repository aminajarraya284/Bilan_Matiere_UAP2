import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn()
}));

vi.mock('@/config/dataSources', () => ({
  assertApiSource: vi.fn(),
  isApiSource: () => true
}));

vi.mock('@/services/api', () => ({ default: mocks }));

import { inventaireService } from './inventaire.service';

const TYPE_ID = '30000000-0000-0000-0000-000000000001';

const periodeResponse = {
  periode: '2026-07',
  conteneurs: [
    {
      id: 'c-1',
      periode: '2026-07',
      dateInventaire: '2026-07-15',
      numeroConteneur: 'C-100',
      numeroOF: null,
      codeArticle: null,
      designationArticle: null,
      typeMatiereId: TYPE_ID,
      codeTypeMatiere: 'M3000L0',
      quantiteKg: 12.5,
      observation: null,
      statut: 'Brouillon'
    }
  ],
  categories: [],
  fours: [],
  totaux: [
    {
      typeMatiereId: TYPE_ID,
      codeTypeMatiere: 'M3000L0',
      totalPartie1: 12.5,
      totalPartie2: 3,
      totalPartie3: 0,
      totalInventaireRapport: 15.5,
      totalUtiliseBilan: 15.5
    }
  ],
  totauxParTypeMatiere: [
    {
      typeMatiereId: TYPE_ID,
      codeTypeMatiere: 'M3000L0',
      stockDebutKg: null,
      stockFinKg: 15.5,
      totalFoursKg: 0,
      totalUtiliseBilanKg: 15.5,
      totalInventaireRapportKg: 15.5,
      anomalies: ['STOCK_DEBUT_INTROUVABLE']
    }
  ]
};

describe('inventaire service', () => {
  beforeEach(() => vi.clearAllMocks());

  it('charge une période sur la route de comptage', async () => {
    mocks.get.mockResolvedValueOnce({ data: periodeResponse });

    const result = await inventaireService.getPeriode('2026-07');

    expect(mocks.get).toHaveBeenCalledWith('/v1/inventaires-fonderie/comptage/2026-07');
    expect(result.conteneurs[0].numeroConteneur).toBe('C-100');
    expect(result.conteneurs[0].numeroOF).toBeUndefined();
  });

  it('rejette une période mal formée sans appeler le réseau', async () => {
    await expect(inventaireService.getPeriode('07-2026')).rejects.toThrow('AAAA-MM');
    expect(mocks.get).not.toHaveBeenCalled();
  });

  // Les totaux affichés viennent de la projection persistée, pas du recalcul local :
  // stock de début/fin et anomalies ne sont disponibles que par elle.
  it('fusionne la projection persistée avec les totaux par partie', async () => {
    mocks.get.mockResolvedValueOnce({ data: periodeResponse });

    const { totaux } = await inventaireService.getPeriode('2026-07');

    expect(totaux[0]).toMatchObject({
      codeTypeMatiere: 'M3000L0',
      stockDebutKg: null,
      stockFinKg: 15.5,
      totalPartie1: 12.5,
      totalPartie2: 3,
      anomalies: ['STOCK_DEBUT_INTROUVABLE']
    });
  });

  it('expose les types matière actifs pour les listes déroulantes', async () => {
    mocks.get.mockResolvedValueOnce({ data: periodeResponse });

    const { typesMatiere } = await inventaireService.getPeriode('2026-07');

    expect(typesMatiere).toEqual([{ id: TYPE_ID, code: 'M3000L0' }]);
  });

  it("envoie la période au format attendu par l'API lors d'une création par catégorie", async () => {
    mocks.post.mockResolvedValueOnce({ data: { ...periodeResponse.conteneurs[0], categorie: 'REBUT' } });

    await inventaireService.createCategorie({
      periode: '2026-07',
      categorie: 'REBUT',
      typeMatiereId: TYPE_ID,
      quantiteKg: 3
    });

    expect(mocks.post).toHaveBeenCalledWith(
      '/v1/inventaires-fonderie/comptage/categories',
      expect.objectContaining({ periode: '2026-07-01', categorie: 'REBUT' })
    );
  });

  // Aucune suppression physique : une ligne retirée du stock reste consultable.
  it('annule une ligne au lieu de la supprimer', async () => {
    mocks.post.mockResolvedValueOnce({ data: { ...periodeResponse.conteneurs[0], statut: 'Annule' } });

    const result = await inventaireService.annulerConteneur('c-1');

    expect(mocks.post).toHaveBeenCalledWith('/v1/inventaires-fonderie/comptage/conteneurs/c-1/annuler');
    expect(mocks.delete).not.toHaveBeenCalled();
    expect(result.statut).toBe('Annule');
  });
});
