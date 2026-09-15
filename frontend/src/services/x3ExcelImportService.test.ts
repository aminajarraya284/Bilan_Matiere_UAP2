import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiPost = vi.fn();
const getDiagnostic = vi.fn();
const getFiles = vi.fn();
const download = vi.fn();
const getYrjmchTracking = vi.fn();
const getMvtTracking = vi.fn();

vi.mock('@/services/api', () => ({
  default: {
    post: apiPost
  }
}));

vi.mock('@/features/imports-historique/importsHistorique.service', () => ({
  importsHistoriqueService: {
    getDiagnostic,
    getFiles,
    download
  }
}));

// Le statut des cartes Etats X3 provient des endpoints de suivi DEDIES, filtres par
// type d'import — et non du diagnostic global, tronque aux 10 derniers lots.
vi.mock('@/features/rapport-machine/rapportMachine.service', () => ({
  rapportMachineService: { getImportTracking: getYrjmchTracking }
}));

vi.mock('@/features/mouvements-stock/mouvementsStock.service', () => ({
  mouvementsStockService: { getImportTracking: getMvtTracking }
}));

const emptyTracking = { derniereTentative: null, dernierImportReussi: null };

const trackingBatch = (overrides: Record<string, unknown> = {}) => ({
  importBatchId: 'batch-id',
  statut: 'Termine',
  message: null,
  nomFichier: 'YRJMCH.xlsx',
  dateCreationUtc: '2026-07-27T12:11:48Z',
  dateFinUtc: '2026-07-27T12:11:48Z',
  lignesLues: 0,
  lignesValides: 0,
  lignesRejetees: 0,
  lignesInserees: 0,
  lignesMisesAJour: 0,
  lignesInchangees: 0,
  ...overrides
});

const importService = async () => {
  vi.resetModules();
  return import('./x3ExcelImportService');
};

const makeFile = (name: string) => new File(['excel-content'], name, { type: 'application/vnd.ms-excel' });

describe('x3ExcelImportService real API mode', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ETATS_X3_SOURCE', 'api');
    localStorage.clear();
    apiPost.mockReset();
    getDiagnostic.mockReset();
    getFiles.mockReset();
    download.mockReset();
    getYrjmchTracking.mockReset();
    getMvtTracking.mockReset();
    getDiagnostic.mockResolvedValue({ derniersImportBatches: [] });
    getFiles.mockResolvedValue([]);
    getYrjmchTracking.mockResolvedValue(emptyTracking);
    getMvtTracking.mockResolvedValue(emptyTracking);
  });

  it('imports MVTSTOTRSVCR through the backend API', async () => {
    apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 2, updated: 1, rowsRead: 5, rowsValid: 3 } });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.importMVTSTOTRSVCR(makeFile('MVTSTOTRSVCR.xls'), 'USINAGE_2');

    expect(apiPost).toHaveBeenCalledWith(
      '/v1/mouvements-entree-of/import',
      expect.any(FormData),
      expect.objectContaining({ headers: { 'Content-Type': 'multipart/form-data' } })
    );
  });

  it('recalcule la classification YRJMCH via le backend', async () => {
    const result = {
      lignesAnalysees: 100,
      lignesModifiees: 4,
      lignesInchangees: 96,
      lignesSci: 20,
      lignesEmr: 50,
      lignesTrn: 25,
      lignesInconnues: 5
    };
    apiPost.mockResolvedValue({ data: result });
    const { x3ExcelImportService } = await importService();

    await expect(x3ExcelImportService.recalculerClassificationYRJMCH()).resolves.toEqual(result);
    expect(apiPost).toHaveBeenCalledWith('/v1/rapports-machine/recalculer-classification');
  });

  it('does not write MVTSTOTRSVCR business state to localStorage in real mode', async () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 1, updated: 0, rowsRead: 1, rowsValid: 1 } });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.importMVTSTOTRSVCR(makeFile('MVTSTOTRSVCR.xls'), 'UAP2');

    expect(setItem).not.toHaveBeenCalledWith(expect.stringContaining('sopal_x3_excel_import'), expect.any(String));
    setItem.mockRestore();
  });

  // Regression : un re-import du meme fichier cree un lot `Duplicate` a 0 ligne.
  // La carte Etats X3 doit continuer d'afficher le dernier import EFFECTIF, sinon
  // elle annonce « 0 ligne lue » alors que les donnees sont bien en base.
  // Le backend exclut deja les lots `Duplicate` de `dernierImportReussi`.
  it('ignores a Duplicate batch and keeps the last effective import counters', async () => {
    getYrjmchTracking.mockResolvedValue({
      derniereTentative: trackingBatch({ importBatchId: 'dup-1', statut: 'Duplicate' }),
      dernierImportReussi: trackingBatch({
        importBatchId: 'ok-1',
        lignesLues: 81997,
        lignesValides: 81996
      })
    });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.refreshStatuses();
    const status = x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH');

    expect(status.importBatchId).toBe('ok-1');
    expect(status.rowsRead).toBe(81997);
    expect(status.rowCount).toBe(81996);
  });

  // Un fichier deja importe n'est pas une erreur : la carte reste « Importé ».
  it('falls back to the Duplicate batch when no effective import exists', async () => {
    getYrjmchTracking.mockResolvedValue({
      derniereTentative: trackingBatch({ importBatchId: 'dup-only', statut: 'Duplicate' }),
      dernierImportReussi: null
    });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.refreshStatuses();
    const status = x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH');

    expect(status.importBatchId).toBe('dup-only');
    expect(status.status).toBe('Importé');
  });

  // Cause du defaut « l'import n'est pas memorise » : le diagnostic global ne renvoie
  // que les 10 derniers lots, tous types confondus. Des qu'assez d'imports plus
  // recents existaient, la carte retombait a « Non importé ».
  it('lit le statut depuis les endpoints dédiés, jamais depuis le diagnostic global', async () => {
    getYrjmchTracking.mockResolvedValue({
      derniereTentative: null,
      dernierImportReussi: trackingBatch({ importBatchId: 'ancien-lot', lignesValides: 42 })
    });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.refreshStatuses();

    expect(getYrjmchTracking).toHaveBeenCalled();
    expect(getDiagnostic).not.toHaveBeenCalled();
    expect(x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH')).toMatchObject({
      importBatchId: 'ancien-lot',
      status: 'Importé',
      rowCount: 42
    });
  });

  // Un suivi indisponible ne doit pas effacer le statut deja affiche.
  it('conserve le statut courant si le suivi est indisponible', async () => {
    getYrjmchTracking.mockRejectedValue(new Error('API indisponible'));
    const { x3ExcelImportService } = await importService();

    await expect(x3ExcelImportService.refreshStatuses()).resolves.toBeUndefined();
    expect(x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH').status).toBe('Non importé');
  });

  it.each(['Date crea', 'Heure crea', 'Utilisateur', 'Lot', 'Emplacement', 'Statut'])(
    'sends MVTSTOTRSVCR to the backend without requiring %s',
    async () => {
      apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 1, updated: 0, rowsRead: 1, rowsValid: 1 } });
      const { x3ExcelImportService } = await importService();

      await expect(x3ExcelImportService.importMVTSTOTRSVCR(makeFile('MVTSTOTRSVCR.xls'), 'USINAGE_2')).resolves.toBeTruthy();

      expect(apiPost).toHaveBeenCalledTimes(1);
    }
  );

  it('imports YRJMCH only through its backend endpoint', async () => {
    apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 3, updated: 0, rowsRead: 3, rowsValid: 3 } });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.importYRJMCH(makeFile('YRJMCH.xls'), 'USINAGE_2');

    expect(apiPost).toHaveBeenCalledWith(
      '/v1/rapports-machine/import',
      expect.any(FormData),
      expect.objectContaining({
        params: { profilImport: 'YRJMCH_V1', sourceSystem: 'X3' },
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    );
  });

  it('keeps API errors visible without mock fallback', async () => {
    apiPost.mockRejectedValue(new Error('API indisponible'));
    const { x3ExcelImportService } = await importService();

    await expect(x3ExcelImportService.importYRJMCH(makeFile('YRJMCH.xls'), 'UAP2')).rejects.toThrow('API indisponible');

    expect(localStorage.getItem('sopal_x3_excel_import_UAP2_YRJMCH')).toBeNull();
    expect(x3ExcelImportService.getStatus('UAP2', 'YRJMCH')).toMatchObject({
      status: 'Erreur',
      error: 'API indisponible'
    });
  });

  it('shows detected and expected profiles when the selected file does not match the card', async () => {
    const { x3ExcelImportService } = await importService();

    await expect(x3ExcelImportService.importMVTSTOTRSVCR(makeFile('YRJMCH.xls'), 'USINAGE_2')).rejects.toThrow(
      'Profil détecté : YRJMCH. Profil attendu : MVTSTOTRSVCR.'
    );

    expect(apiPost).not.toHaveBeenCalled();
  });

  it('refreshes import history after a successful backend import', async () => {
    apiPost.mockResolvedValue({ data: { duplicate: false, inserted: 1, updated: 0, rowsRead: 1, rowsValid: 1 } });
    getMvtTracking.mockResolvedValue({
      // Une tentative `EnCours` plus recente ne doit pas masquer le dernier import reussi.
      derniereTentative: trackingBatch({ importBatchId: 'stale-batch-id', statut: 'EnCours' }),
      dernierImportReussi: trackingBatch({
        importBatchId: 'batch-id',
        nomFichier: 'MVTSTOTRSVCR.xls',
        lignesLues: 1,
        lignesValides: 1,
        lignesInserees: 1
      })
    });
    getFiles.mockResolvedValue([
      {
        id: 'file-id',
        importBatchId: 'batch-id',
        typeImport: 'MvtEntreeOF',
        profilImport: 'MVT_ENTREE_OF_V1',
        nomFichierOriginal: 'MVTSTOTRSVCR.xls',
        extension: '.xls',
        tailleOctets: 12,
        hashSha256: 'hash',
        dateUploadUtc: '2026-07-27T08:01:00Z',
        uploadeParNom: 'admin'
      }
    ]);
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.importMVTSTOTRSVCR(makeFile('MVTSTOTRSVCR.xls'), 'USINAGE_2');

    expect(getMvtTracking).toHaveBeenCalled();
    expect(x3ExcelImportService.getStatus('USINAGE_2', 'MVTSTOTRSVCR')).toMatchObject({
      importBatchId: 'batch-id',
      archivedFileId: 'file-id',
      hasArchivedFile: true,
      user: 'admin',
      rowsRejected: 0
    });
  });
});

describe('x3ExcelImportService mock mode', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ETATS_X3_SOURCE', 'mock');
    localStorage.clear();
    apiPost.mockReset();
  });

  it('keeps the local demo import available outside real API mode', async () => {
    const xlsx = await import('xlsx');
    const worksheet = xlsx.utils.json_to_sheet([
      {
        'Date imput': '27/07/2026',
        Code: 'A1',
        Désignation: 'Article',
        Quantité: '1',
        Unité: 'UN',
        Transaction: 'Entrée OF',
        'Type Pce': 'OF',
        'Pièce Origin': 'OF123'
      }
    ]);
    const workbook = xlsx.utils.book_new();
    xlsx.utils.book_append_sheet(workbook, worksheet);
    const buffer = xlsx.write(workbook, { type: 'array', bookType: 'xlsx' }) as ArrayBuffer;
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.importMVTSTOTRSVCR(
      new File([buffer], 'MVTSTOTRSVCR.xlsx', {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      }),
      'USINAGE_2'
    );

    expect(apiPost).not.toHaveBeenCalled();
    expect(x3ExcelImportService.getStatus('USINAGE_2', 'MVTSTOTRSVCR').status).toBe('Importé');
  });
});
