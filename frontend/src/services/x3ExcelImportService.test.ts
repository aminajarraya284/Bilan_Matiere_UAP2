import { beforeEach, describe, expect, it, vi } from 'vitest';

const apiPost = vi.fn();
const getDiagnostic = vi.fn();
const getFiles = vi.fn();
const download = vi.fn();

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
    getDiagnostic.mockResolvedValue({ derniersImportBatches: [] });
    getFiles.mockResolvedValue([]);
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
  it('ignores a Duplicate batch and keeps the last effective import counters', async () => {
    getDiagnostic.mockResolvedValue({
      derniersImportBatches: [
        {
          id: 'dup-1',
          typeImport: 'RapportMachine',
          profilImport: 'YRJMCH_V1',
          statut: 'Duplicate',
          nombreLignesLues: 0,
          nombreLignesValides: 0,
          readCount: 0,
          validCount: 0,
          dateCreationUtc: '2026-07-27T12:16:01',
          dateFinUtc: '2026-07-27T12:16:01'
        },
        {
          id: 'ok-1',
          typeImport: 'RapportMachine',
          profilImport: 'YRJMCH_V1',
          statut: 'Termine',
          nombreLignesLues: 81997,
          nombreLignesValides: 81996,
          readCount: 81997,
          validCount: 81996,
          dateCreationUtc: '2026-07-27T12:11:48',
          dateFinUtc: '2026-07-27T12:11:48'
        }
      ]
    });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.refreshStatuses();
    const status = x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH');

    expect(status.importBatchId).toBe('ok-1');
    expect(status.rowsRead).toBe(81997);
    expect(status.rowCount).toBe(81996);
  });

  it('falls back to the Duplicate batch when no effective import exists', async () => {
    getDiagnostic.mockResolvedValue({
      derniersImportBatches: [
        {
          id: 'dup-only',
          typeImport: 'RapportMachine',
          profilImport: 'YRJMCH_V1',
          statut: 'Duplicate',
          nombreLignesLues: 0,
          nombreLignesValides: 0,
          readCount: 0,
          validCount: 0,
          dateCreationUtc: '2026-07-27T12:16:01',
          dateFinUtc: '2026-07-27T12:16:01'
        }
      ]
    });
    const { x3ExcelImportService } = await importService();

    await x3ExcelImportService.refreshStatuses();

    expect(x3ExcelImportService.getStatus('USINAGE_2', 'YRJMCH').importBatchId).toBe('dup-only');
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
    getDiagnostic.mockResolvedValue({
      derniersImportBatches: [
        {
          id: 'stale-batch-id',
          hashFichierSha256: 'hash-stale',
          typeImport: 'MvtEntreeOF',
          profilImport: 'MVT_ENTREE_OF_V1',
          statut: 'EnCours',
          nombreLignesLues: 0,
          nombreLignesValides: 0,
          nombreLignesImportees: 0,
          nombreAnomalies: 0,
          dateCreationUtc: '2026-07-27T08:02:00Z',
          dateFinUtc: null
        },
        {
          id: 'batch-id',
          hashFichierSha256: 'hash',
          typeImport: 'MvtEntreeOF',
          profilImport: 'MVT_ENTREE_OF_V1',
          statut: 'Termine',
          nombreLignesLues: 1,
          nombreLignesValides: 1,
          nombreLignesImportees: 1,
          nombreAnomalies: 72888,
          dateCreationUtc: '2026-07-27T08:00:00Z',
          dateFinUtc: '2026-07-27T08:01:00Z'
        }
      ]
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

    expect(getDiagnostic).toHaveBeenCalled();
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
