import { describe, expect, it, vi, beforeEach } from 'vitest';

const apiGet = vi.fn();
const apiPost = vi.fn();

vi.mock('@/services/api', () => ({
  default: {
    get: apiGet,
    post: apiPost
  }
}));

const importReferenceService = async () => {
  vi.resetModules();
  return import('./reference.service');
};

const gammeRow = (codeArticle: string, codePoste = 'P-01') => ({
  codeArticle,
  designationArticle: 'Article test',
  dateFinValidite: '2099-12-31',
  alternativeGamme: '1',
  numeroOperation: '5',
  codeAtelier: 'UAP2',
  codePoste,
  groupePosteCharge: 'GC',
  designationOperation: 'Usinage',
  cleMetier: `KEY-${codeArticle}`,
  isActive: true
});

const nomenclatureRow = (codeComposant: string) => ({
  codeArticleParent: '0101A03-1',
  alternativeNomenclature: '1',
  codeParentNiveau: '1',
  codeComposant,
  niveau: '1',
  sequence: '10',
  designationComposant: 'Composant',
  quantiteLien: 1,
  quantiteLienCumulee: 1,
  uniteQuantite: 'U',
  categorieComposant: 'C',
  typeLien: 'N',
  pourcentageRebut: 0,
  cleMetier: `KEY-${codeComposant}`,
  isActive: true
});

describe('referenceService pagination serveur', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_REFERENCES_FABRICATION_SOURCE', 'api');
    apiGet.mockReset();
    apiPost.mockReset();
  });

  // 1 + 2. Une seule requête pour la page visible : plus aucune boucle fetchAllPaged.
  it('ne déclenche qu une seule requête pour la page demandée', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [gammeRow('0101A03-1')], total: 106905 } });
    const { referenceService } = await importReferenceService();

    const result = await referenceService.getGammes({ page: 1, pageSize: 100 });

    expect(apiGet).toHaveBeenCalledTimes(1);
    expect(apiGet).toHaveBeenCalledWith('/v1/references/gammes', {
      params: { page: 1, pageSize: 100 },
      signal: undefined
    });
    expect(result.items).toHaveLength(1);
  });

  // 6. Le total affiché vient de l'API, pas du nombre de lignes reçues.
  it('remonte le total renvoyé par l API sans le recalculer', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [gammeRow('A'), gammeRow('B')], total: 106905 } });
    const { referenceService } = await importReferenceService();

    const { items, total } = await referenceService.getGammes({ page: 1, pageSize: 100 });

    expect(total).toBe(106905);
    expect(items).toHaveLength(2);
  });

  // 11 + 12. Un total énorme ne provoque AUCUN appel en cascade jusqu'à la dernière page.
  it('ne charge jamais l intégralité du référentiel malgré un total très élevé', async () => {
    apiGet.mockResolvedValue({ data: { items: [gammeRow('X')], total: 267645 } });
    const { referenceService } = await importReferenceService();

    const { items } = await referenceService.getGammes({ page: 1, pageSize: 100 });

    expect(apiGet).toHaveBeenCalledTimes(1);
    expect(items).toHaveLength(1);
  });

  // 3. Changer de page = exactement une nouvelle requête, avec la bonne page.
  it('envoie une seule requête au changement de page', async () => {
    apiGet.mockResolvedValue({ data: { items: [gammeRow('P2')], total: 106905 } });
    const { referenceService } = await importReferenceService();

    await referenceService.getGammes({ page: 2, pageSize: 100 });

    expect(apiGet).toHaveBeenCalledTimes(1);
    expect(apiGet).toHaveBeenCalledWith('/v1/references/gammes', {
      params: { page: 2, pageSize: 100 },
      signal: undefined
    });
  });

  // Les identifiants générés sont décalés par page pour rester uniques entre pages.
  it('décale les identifiants générés selon la page', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [gammeRow('P2-A')], total: 500 } });
    const { referenceService } = await importReferenceService();

    const { items } = await referenceService.getGammes({ page: 3, pageSize: 100 });

    expect(items[0].id).toBe(201);
  });

  // 4. La recherche est appliquée par le backend, jamais par filtrage local.
  it('transmet la recherche au backend comme filtre codeArticle', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [], total: 0 } });
    const { referenceService } = await importReferenceService();

    await referenceService.getGammes({ page: 1, pageSize: 50, codeArticle: '0101' });

    expect(apiGet).toHaveBeenCalledWith('/v1/references/gammes', {
      params: { codeArticle: '0101', page: 1, pageSize: 50 },
      signal: undefined
    });
  });

  it('omet les filtres vides plutôt que d envoyer des paramètres inutiles', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [], total: 0 } });
    const { referenceService } = await importReferenceService();

    await referenceService.getGammes({ page: 1, pageSize: 100, codeArticle: '', atelier: undefined });

    expect(apiGet).toHaveBeenCalledWith('/v1/references/gammes', {
      params: { page: 1, pageSize: 100 },
      signal: undefined
    });
  });

  // 9. Le signal d'annulation est bien transmis à axios.
  it('transmet le signal d annulation à la requête', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [], total: 0 } });
    const { referenceService } = await importReferenceService();
    const controller = new AbortController();

    await referenceService.getGammes({ page: 1, pageSize: 100, signal: controller.signal });

    expect(apiGet).toHaveBeenCalledWith('/v1/references/gammes', {
      params: { page: 1, pageSize: 100 },
      signal: controller.signal
    });
  });

  // 7. Une erreur API remonte à l'appelant, sans repli silencieux sur des données locales.
  it('propage l erreur API sans repli sur les données de démonstration', async () => {
    apiGet.mockRejectedValueOnce(new Error('500 Internal Server Error'));
    const { referenceService } = await importReferenceService();

    await expect(referenceService.getGammes({ page: 1, pageSize: 100 })).rejects.toThrow('500');
  });

  // 5. Les nomenclatures suivent le même contrat paginé, sur leur propre route.
  it('pagine les nomenclatures sur leur propre endpoint', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [nomenclatureRow('C-1')], total: 160740 } });
    const { referenceService } = await importReferenceService();

    const { items, total } = await referenceService.getNomenclatures({ page: 2, pageSize: 200 });

    expect(apiGet).toHaveBeenCalledTimes(1);
    expect(apiGet).toHaveBeenCalledWith('/v1/references/nomenclatures', {
      params: { page: 2, pageSize: 200 },
      signal: undefined
    });
    expect(total).toBe(160740);
    expect(items[0]).toMatchObject({ composant: 'C-1', source: 'API' });
  });

  it('transmet les filtres nomenclature acceptés par le backend', async () => {
    apiGet.mockResolvedValueOnce({ data: { items: [], total: 0 } });
    const { referenceService } = await importReferenceService();

    await referenceService.getNomenclatures({
      page: 1,
      pageSize: 100,
      codeArticleParent: '0101A03-1',
      niveau: '1'
    });

    expect(apiGet).toHaveBeenCalledWith('/v1/references/nomenclatures', {
      params: { codeArticleParent: '0101A03-1', niveau: '1', page: 1, pageSize: 100 },
      signal: undefined
    });
  });

  it('imports GAMMEFAB through the backend API', async () => {
    apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 2, updated: 1 } });
    const { referenceService } = await importReferenceService();
    const file = new File(['content'], 'GAMMEFAB.xls', { type: 'application/vnd.ms-excel' });

    const result = await referenceService.importGammes(file);

    expect(apiPost).toHaveBeenCalledWith('/v1/references/gammes/import', expect.any(FormData), {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    expect(result.importedCount).toBe(3);
  });
});
