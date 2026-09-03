import * as XLSX from 'xlsx';
import api from '@/services/api';
import { importsHistoriqueService } from '@/features/imports-historique/importsHistorique.service';
import { rapportMachineService } from '@/features/rapport-machine/rapportMachine.service';
import type { ImportExecutionSummary, ImportTracking } from '@/features/rapport-machine/rapportMachine.types';
import { mouvementsStockService } from '@/features/mouvements-stock/mouvementsStock.service';
import { isApiSource } from '@/config/dataSources';

export type X3BilanType = 'USINAGE_2' | 'UAP2';
export type X3ExcelFileType = 'MVTSTOTRSVCR' | 'YRJMCH';
export type X3ImportStatus = 'Non importé' | 'Importé' | 'Erreur';

export type X3ClassificationResult = {
  lignesAnalysees: number;
  lignesModifiees: number;
  lignesInchangees: number;
  lignesSci: number;
  lignesEmr: number;
  lignesTrn: number;
  lignesInconnues: number;
};

type BackendImportResponse = {
  id?: string;
  importBatchId?: string;
  duplicate?: boolean;
  inserted?: number;
  updated?: number;
  unchanged?: number;
  rejected?: number;
  rowsRead?: number;
  readCount?: number;
  rowsValid?: number;
  validCount?: number;
  anomalies?: number;
  statut?: string;
  archiveErrorMessage?: string | null;
};

export type X3ImportedSource = {
  bilanType: X3BilanType;
  fileType: X3ExcelFileType;
  fileName: string;
  importedAt: string;
  rowCount: number;
  status: X3ImportStatus;
  error?: string;
  profile?: string;
  user?: string;
  rowsRead?: number;
  rowsInserted?: number;
  rowsUpdated?: number;
  rowsIgnored?: number;
  rowsRejected?: number;
  importBatchId?: string;
  hasArchivedFile?: boolean;
  archivedFileId?: string;
  rows: Record<string, string | number | boolean | null>[];
};

export type X3ImportedSourceStatus = Omit<X3ImportedSource, 'rows'>;

const STORAGE_PREFIX = 'sopal_x3_excel_import';
const MAX_EXCEL_FILE_SIZE_BYTES = 100 * 1024 * 1024;

const REQUIRED_MVTSTOTRSVCR_COLUMNS = [
  'Date imput',
  'Code',
  'Désignation',
  'Quantité',
  'Unité',
  'Transaction',
  'Type Pce',
  'Pièce Origin'
];

const REQUIRED_YRJMCH_COLUMNS = [
  'Date',
  'Poste',
  'Ope',
  'Article',
  'Désignation',
  'Numéro OF',
  'Equipe',
  'Qté Tot',
  'Qté R',
  'Alt GM'
];

const normalizeHeader = (value: string): string =>
  value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');

const storageKey = (bilanType: X3BilanType, fileType: X3ExcelFileType) => `${STORAGE_PREFIX}_${bilanType}_${fileType}`;

const useRealApi = isApiSource('etatsX3');

const realApiStatuses: Record<X3BilanType, Record<X3ExcelFileType, X3ImportedSourceStatus>> = {
  USINAGE_2: {
    MVTSTOTRSVCR: {
      bilanType: 'USINAGE_2',
      fileType: 'MVTSTOTRSVCR',
      fileName: '',
      importedAt: '',
      rowCount: 0,
      status: 'Non importé'
    },
    YRJMCH: {
      bilanType: 'USINAGE_2',
      fileType: 'YRJMCH',
      fileName: '',
      importedAt: '',
      rowCount: 0,
      status: 'Non importé'
    }
  },
  UAP2: {
    MVTSTOTRSVCR: {
      bilanType: 'UAP2',
      fileType: 'MVTSTOTRSVCR',
      fileName: '',
      importedAt: '',
      rowCount: 0,
      status: 'Non importé'
    },
    YRJMCH: {
      bilanType: 'UAP2',
      fileType: 'YRJMCH',
      fileName: '',
      importedAt: '',
      rowCount: 0,
      status: 'Non importé'
    }
  }
};

const assertExpectedFileName = (file: File, expectedType: X3ExcelFileType) => {
  const normalizedName = file.name.toUpperCase();
  const otherType: X3ExcelFileType = expectedType === 'MVTSTOTRSVCR' ? 'YRJMCH' : 'MVTSTOTRSVCR';

  if (normalizedName.includes(otherType)) {
    throw new Error(
      `Profil détecté : ${otherType}. Profil attendu : ${expectedType}. Importez ce fichier depuis la carte ${otherType}.`
    );
  }

  if (!normalizedName.includes(expectedType)) {
    throw new Error(`Nom de fichier invalide : le fichier doit contenir "${expectedType}" dans son nom.`);
  }

  if (!/\.(xlsx|xls)$/i.test(file.name)) {
    throw new Error('Format non supporté. Importez un fichier Excel .xlsx ou .xls.');
  }
};

const assertExcelFileSize = (file: File): void => {
  if (file.size === 0) throw new Error('Le fichier Excel est vide.');
  if (file.size > MAX_EXCEL_FILE_SIZE_BYTES) {
    throw new Error('Le fichier Excel dépasse la taille maximale autorisée de 100 Mo.');
  }
};

const assertRealApiFile = (file: File, expectedType: X3ExcelFileType): void => {
  assertExpectedFileName(file, expectedType);
  assertExcelFileSize(file);
};

const parseExcelRows = async (file: File): Promise<Record<string, unknown>[]> => {
  assertExcelFileSize(file);
  const buffer = await file.arrayBuffer();
  const workbook = XLSX.read(buffer, { type: 'array', cellDates: true });
  const firstSheetName = workbook.SheetNames[0];

  if (!firstSheetName) {
    throw new Error('Le fichier Excel ne contient aucune feuille.');
  }

  return XLSX.utils.sheet_to_json<Record<string, unknown>>(workbook.Sheets[firstSheetName], {
    defval: '',
    raw: false
  });
};

const validateColumns = (rows: Record<string, unknown>[], requiredColumns: string[]) => {
  const headers = Object.keys(rows[0] ?? {});
  const normalizedHeaders = new Set(headers.map(normalizeHeader));
  const missing = requiredColumns.filter((column) => !normalizedHeaders.has(normalizeHeader(column)));

  if (missing.length > 0) {
    throw new Error(`Colonnes obligatoires manquantes : ${missing.join(', ')}.`);
  }
};

const normalizeCellValue = (value: unknown): string | number | boolean | null => {
  if (value === null || value === undefined) return null;
  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') return value;
  return String(value);
};

const normalizeRows = (
  rows: Record<string, unknown>[],
  requiredColumns: string[]
): Record<string, string | number | boolean | null>[] => {
  const columnByNormalizedName = new Map<string, string>();
  Object.keys(rows[0] ?? {}).forEach((header) => {
    columnByNormalizedName.set(normalizeHeader(header), header);
  });

  return rows.map((row) => {
    const normalizedRow: Record<string, string | number | boolean | null> = {};
    requiredColumns.forEach((column) => {
      const sourceHeader = columnByNormalizedName.get(normalizeHeader(column));
      const value = sourceHeader ? row[sourceHeader] : null;
      normalizedRow[column] = normalizeCellValue(value);
    });
    return normalizedRow;
  });
};

const saveImportedSource = (
  bilanType: X3BilanType,
  fileType: X3ExcelFileType,
  fileName: string,
  rows: Record<string, string | number | boolean | null>[]
): X3ImportedSource => {
  const importedSource: X3ImportedSource = {
    bilanType,
    fileType,
    fileName,
    importedAt: new Date().toISOString(),
    rowCount: rows.length,
    status: 'Importé',
    rows
  };

  localStorage.setItem(storageKey(bilanType, fileType), JSON.stringify(importedSource));
  return importedSource;
};

const saveImportError = (
  bilanType: X3BilanType,
  fileType: X3ExcelFileType,
  fileName: string,
  error: string
): X3ImportedSourceStatus => {
  const sourceStatus: X3ImportedSourceStatus = {
    bilanType,
    fileType,
    fileName,
    importedAt: new Date().toISOString(),
    rowCount: 0,
    status: 'Erreur',
    error
  };

  localStorage.setItem(storageKey(bilanType, fileType), JSON.stringify({ ...sourceStatus, rows: [] }));
  return sourceStatus;
};

const toErrorMessage = (error: unknown, fallback: string): string => {
  if (error instanceof Error) return error.message;
  return fallback;
};

const getSource = (bilanType: X3BilanType, fileType: X3ExcelFileType): X3ImportedSource | null => {
  const key = storageKey(bilanType, fileType);
  const stored = localStorage.getItem(key);
  if (!stored) return null;

  try {
    const parsed = JSON.parse(stored) as Partial<X3ImportedSource> | null;
    const isValid =
      parsed !== null &&
      parsed.bilanType === bilanType &&
      parsed.fileType === fileType &&
      typeof parsed.fileName === 'string' &&
      typeof parsed.importedAt === 'string' &&
      typeof parsed.rowCount === 'number' &&
      (parsed.status === 'Importé' || parsed.status === 'Erreur') &&
      Array.isArray(parsed.rows);
    if (!isValid) {
      localStorage.removeItem(key);
      return null;
    }
    return parsed as X3ImportedSource;
  } catch {
    localStorage.removeItem(key);
    return null;
  }
};

const getSourceStatus = (bilanType: X3BilanType, fileType: X3ExcelFileType): X3ImportedSourceStatus => {
  const source = getSource(bilanType, fileType);
  if (!source) {
    return {
      bilanType,
      fileType,
      fileName: '',
      importedAt: '',
      rowCount: 0,
      status: 'Non importé'
    };
  }

  return {
    bilanType: source.bilanType,
    fileType: source.fileType,
    fileName: source.fileName,
    importedAt: source.importedAt,
    rowCount: source.rowCount,
    status: source.status,
    error: source.error
  };
};

const importFile = async (
  file: File,
  bilanType: X3BilanType,
  fileType: X3ExcelFileType,
  requiredColumns: string[]
): Promise<X3ImportedSource> => {
  try {
    assertExpectedFileName(file, fileType);
    const rows = await parseExcelRows(file);

    if (rows.length === 0) {
      throw new Error('Le fichier ne contient aucune ligne de données.');
    }

    validateColumns(rows, requiredColumns);
    return saveImportedSource(bilanType, fileType, file.name, normalizeRows(rows, requiredColumns));
  } catch (error) {
    saveImportError(bilanType, fileType, file.name, error instanceof Error ? error.message : 'Import Excel invalide.');
    throw error;
  }
};

const applyRealApiStatus = (
  fileType: X3ExcelFileType,
  fileName: string,
  response: BackendImportResponse,
  error?: string
): X3ImportedSource => {
  const rowsRead = response.rowsRead ?? response.readCount ?? 0;
  const rowsValid = response.rowsValid ?? response.validCount ?? ((response.inserted ?? 0) + (response.updated ?? 0) + (response.unchanged ?? 0));
  const importedSource: X3ImportedSource = {
    bilanType: 'USINAGE_2',
    fileType,
    fileName,
    importedAt: new Date().toISOString(),
    rowCount: rowsValid,
    status: error || response.statut === 'Echec' ? 'Erreur' : 'Importé',
    error: error ?? response.archiveErrorMessage ?? undefined,
    profile: fileType === 'YRJMCH' ? 'YRJMCH_V1' : 'MVT_ENTREE_OF_V1',
    rowsRead,
    rowsInserted: response.inserted ?? 0,
    rowsUpdated: response.updated ?? 0,
    rowsIgnored: response.unchanged ?? 0,
    rowsRejected: response.rejected ?? 0,
    importBatchId: response.importBatchId ?? response.id,
    rows: []
  };

  (['USINAGE_2', 'UAP2'] as X3BilanType[]).forEach((bilanType) => {
    realApiStatuses[bilanType][fileType] = { ...importedSource, bilanType };
  });

  return importedSource;
};

const applyRealApiError = (fileType: X3ExcelFileType, fileName: string, error: string): void => {
  (['USINAGE_2', 'UAP2'] as X3BilanType[]).forEach((bilanType) => {
    realApiStatuses[bilanType][fileType] = {
      bilanType,
      fileType,
      fileName,
      importedAt: new Date().toISOString(),
      rowCount: 0,
      status: 'Erreur',
      error,
      rowsRead: 0
    };
  });
};

/**
 * Traduit un suivi d'import (endpoint dedie) en statut de carte.
 *
 * Le nom de fichier est deja resolu par le backend depuis les fichiers archives ;
 * l'appel a l'historique ne sert plus qu'a retrouver l'identifiant permettant de
 * retelecharger la piece, et son echec ne doit pas invalider le statut.
 */
const statusFromTracking = async (
  batch: ImportExecutionSummary,
  fileType: X3ExcelFileType
): Promise<X3ImportedSourceStatus> => {
  // Un lot `Duplicate` (fichier deja importe) n'est pas une erreur : seul un echec
  // reel doit basculer la carte en rouge.
  const enEchec = batch.statut.toLowerCase().includes('echec');
  let archivedFileId: string | undefined;
  let hasArchivedFile = false;
  let uploadedBy: string | undefined;

  try {
    const files = await importsHistoriqueService.getFiles(batch.importBatchId);
    const file = files[0];
    if (file) {
      archivedFileId = file.id;
      hasArchivedFile = true;
      uploadedBy = file.uploadeParNom ?? undefined;
    }
  } catch {
    hasArchivedFile = false;
  }

  return {
    bilanType: 'USINAGE_2',
    fileType,
    fileName: batch.nomFichier ?? '',
    importedAt: batch.dateFinUtc ?? batch.dateCreationUtc,
    rowCount: batch.lignesValides,
    status: enEchec ? 'Erreur' : 'Importé',
    error: enEchec ? (batch.message ?? undefined) : undefined,
    user: uploadedBy ?? undefined,
    rowsRead: batch.lignesLues,
    rowsInserted: batch.lignesInserees,
    rowsUpdated: batch.lignesMisesAJour,
    rowsIgnored: batch.lignesInchangees,
    rowsRejected: batch.lignesRejetees,
    importBatchId: batch.importBatchId,
    hasArchivedFile,
    archivedFileId
  };
};

/**
 * Statut de chaque source, lu depuis l'endpoint de suivi DEDIE a son type d'import.
 *
 * Ne pas repasser par `/v1/diagnostics/imports-reels` : ce diagnostic ne renvoie que
 * les 10 derniers lots, tous types confondus. Comme chaque import (gammes,
 * nomenclatures, rapports machine, mouvements) ecrit dans la meme table, les lots
 * YRJMCH / MVTSTOTRSVCR en sortaient des que 10 imports plus recents existaient, et
 * les cartes retombaient a « Non importé » alors que les donnees etaient bien en base.
 *
 * Les endpoints dedies filtrent par `TypeImport` et renvoient le dernier import
 * reussi, sans troncature.
 */
const TRACKING_LOADERS: Record<X3ExcelFileType, () => Promise<ImportTracking>> = {
  YRJMCH: () => rapportMachineService.getImportTracking(),
  MVTSTOTRSVCR: () => mouvementsStockService.getImportTracking()
};

const refreshRealApiStatuses = async (): Promise<void> => {
  const fileTypes = Object.keys(TRACKING_LOADERS) as X3ExcelFileType[];

  await Promise.all(
    fileTypes.map(async (fileType) => {
      let tracking: ImportTracking;
      try {
        tracking = await TRACKING_LOADERS[fileType]();
      } catch {
        // Un suivi indisponible ne doit pas effacer le statut deja affiche.
        return;
      }

      // `dernierImportReussi` est la memoire de la carte ; `derniereTentative` ne sert
      // qu'a signaler un echec quand aucun import n'a jamais abouti.
      const batch = tracking.dernierImportReussi ?? tracking.derniereTentative;
      if (!batch) return;

      const status = await statusFromTracking(batch, fileType);
      (['USINAGE_2', 'UAP2'] as X3BilanType[]).forEach((bilanType) => {
        const current = realApiStatuses[bilanType][fileType];
        const mergedFileName =
          status.fileName || current.fileName || (fileType === 'YRJMCH' ? 'YRJMCH.xlsx' : 'MVTSTOTRSVCR.xls');
        realApiStatuses[bilanType][fileType] = {
          ...current,
          ...status,
          fileName: mergedFileName,
          bilanType
        };
      });
    })
  );
};

const importMvtViaBackend = async (file: File): Promise<X3ImportedSource> => {
  assertRealApiFile(file, 'MVTSTOTRSVCR');
  try {
    const form = new FormData();
    form.append('file', file);
    const response = await api.post<BackendImportResponse>('/v1/mouvements-entree-of/import', form, {
      params: { sourceSystem: 'X3' },
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    const imported = applyRealApiStatus('MVTSTOTRSVCR', file.name, response.data);
    await refreshRealApiStatuses();
    return imported;
  } catch (error) {
    const message = toErrorMessage(error, 'Import MVTSTOTRSVCR invalide.');
    applyRealApiError('MVTSTOTRSVCR', file.name, message);
    throw error;
  }
};

const importYrjmchViaBackend = async (file: File): Promise<X3ImportedSource> => {
  assertRealApiFile(file, 'YRJMCH');
  try {
    const form = new FormData();
    form.append('file', file);
    const response = await api.post<BackendImportResponse>('/v1/rapports-machine/import', form, {
      params: { profilImport: 'YRJMCH_V1', sourceSystem: 'X3' },
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    const imported = applyRealApiStatus('YRJMCH', file.name, response.data);
    await refreshRealApiStatuses();
    return imported;
  } catch (error) {
    const message = toErrorMessage(error, 'Import YRJMCH invalide.');
    applyRealApiError('YRJMCH', file.name, message);
    throw error;
  }
};

export const x3ExcelImportService = {
  importMVTSTOTRSVCR(file: File, bilanType: X3BilanType): Promise<X3ImportedSource> {
    if (useRealApi) {
      return importMvtViaBackend(file);
    }
    return importFile(file, bilanType, 'MVTSTOTRSVCR', REQUIRED_MVTSTOTRSVCR_COLUMNS);
  },

  importYRJMCH(file: File, bilanType: X3BilanType): Promise<X3ImportedSource> {
    if (useRealApi) {
      return importYrjmchViaBackend(file);
    }
    return importFile(file, bilanType, 'YRJMCH', REQUIRED_YRJMCH_COLUMNS);
  },

  validateMVTSTOTRSVCRColumns(rows: Record<string, unknown>[]): void {
    validateColumns(rows, REQUIRED_MVTSTOTRSVCR_COLUMNS);
  },

  validateYRJMCHColumns(rows: Record<string, unknown>[]): void {
    validateColumns(rows, REQUIRED_YRJMCH_COLUMNS);
  },

  normalizeMVTSTOTRSVCR(rows: Record<string, unknown>[]): Record<string, string | number | boolean | null>[] {
    return normalizeRows(rows, REQUIRED_MVTSTOTRSVCR_COLUMNS);
  },

  normalizeYRJMCH(rows: Record<string, unknown>[]): Record<string, string | number | boolean | null>[] {
    return normalizeRows(rows, REQUIRED_YRJMCH_COLUMNS);
  },

  getStatus(bilanType: X3BilanType, fileType: X3ExcelFileType): X3ImportedSourceStatus {
    if (useRealApi) {
      return realApiStatuses[bilanType][fileType];
    }
    return getSourceStatus(bilanType, fileType);
  },

  async refreshStatuses(): Promise<void> {
    if (useRealApi) {
      await refreshRealApiStatuses();
    }
  },

  async recalculerClassificationYRJMCH(): Promise<X3ClassificationResult> {
    const { data } = await api.post<X3ClassificationResult>('/v1/rapports-machine/recalculer-classification');
    return data;
  },

  getRows(bilanType: X3BilanType, fileType: X3ExcelFileType): Record<string, string | number | boolean | null>[] {
    if (useRealApi) {
      return [];
    }
    return getSource(bilanType, fileType)?.rows ?? [];
  },

  hasBothSources(bilanType: X3BilanType): boolean {
    if (useRealApi) {
      return (
        realApiStatuses[bilanType].MVTSTOTRSVCR.status === 'Importé' &&
        realApiStatuses[bilanType].YRJMCH.status === 'Importé'
      );
    }
    return (
      getSourceStatus(bilanType, 'MVTSTOTRSVCR').status === 'Importé' &&
      getSourceStatus(bilanType, 'YRJMCH').status === 'Importé'
    );
  },

  async downloadArchivedFile(status: X3ImportedSourceStatus): Promise<void> {
    if (!status.importBatchId || !status.archivedFileId || !status.fileName) return;
    await importsHistoriqueService.download(status.importBatchId, status.archivedFileId, status.fileName);
  }
};
