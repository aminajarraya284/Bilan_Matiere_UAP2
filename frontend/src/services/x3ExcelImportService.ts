import * as XLSX from 'xlsx';
import api from '@/services/api';
import { importsHistoriqueService } from '@/features/imports-historique/importsHistorique.service';
import type { ImportBatchSummary } from '@/features/imports-historique/importsHistorique.types';
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
  importBatchId?: string;
  duplicate?: boolean;
  inserted?: number;
  updated?: number;
  unchanged?: number;
  rejected?: number;
  rowsRead?: number;
  rowsValid?: number;
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
const MAX_EXCEL_FILE_SIZE_BYTES = 20 * 1024 * 1024;

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
    throw new Error('Le fichier Excel dépasse la taille maximale autorisée de 20 Mo.');
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
  const importedSource: X3ImportedSource = {
    bilanType: 'USINAGE_2',
    fileType,
    fileName,
    importedAt: new Date().toISOString(),
    rowCount: response.rowsValid ?? 0,
    status: error || response.statut === 'Echec' ? 'Erreur' : 'Importé',
    error: error ?? response.archiveErrorMessage ?? undefined,
    profile: fileType === 'YRJMCH' ? 'YRJMCH_V1' : 'MVT_ENTREE_OF_V1',
    rowsRead: response.rowsRead,
    rowsInserted: response.inserted,
    rowsUpdated: response.updated,
    rowsIgnored: response.unchanged,
    rowsRejected: response.rejected,
    importBatchId: response.importBatchId,
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

const toFileTypeFromBatch = (batch: ImportBatchSummary): X3ExcelFileType | null => {
  const typeImport = batch.typeImport.toLowerCase();
  const profilImport = (batch.profilImport ?? '').toLowerCase();
  if (typeImport.includes('rapportmachine') || profilImport.includes('yrjmch')) return 'YRJMCH';
  if (typeImport.includes('mvtentreeof') || profilImport.includes('mvt_entree_of')) return 'MVTSTOTRSVCR';
  return null;
};

const statusFromBatch = async (batch: ImportBatchSummary, fileType: X3ExcelFileType): Promise<X3ImportedSourceStatus> => {
  let fileName = '';
  let archivedFileId: string | undefined;
  let hasArchivedFile = false;
  let uploadedBy: string | undefined;

  try {
    const files = await importsHistoriqueService.getFiles(batch.id);
    const file = files[0];
    if (file) {
      fileName = file.nomFichierOriginal;
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
    fileName,
    importedAt: batch.dateFinUtc ?? batch.dateCreationUtc,
    rowCount: batch.validCount ?? batch.nombreLignesValides,
    status: batch.statut.toLowerCase().includes('echec') ? 'Erreur' : 'Importé',
    error: batch.archiveErrorMessage ?? undefined,
    profile: batch.profilImport ?? undefined,
    user: uploadedBy ?? batch.creeParNom ?? undefined,
    rowsRead: batch.readCount ?? batch.nombreLignesLues,
    rowsInserted: batch.insertedCount,
    rowsUpdated: batch.updatedCount,
    rowsIgnored: batch.unchangedCount,
    rowsRejected:
      batch.rejectedCount ??
      Math.max(0, batch.nombreLignesLues - batch.nombreLignesValides),
    importBatchId: batch.id,
    hasArchivedFile,
    archivedFileId
  };
};

/**
 * Un lot `Duplicate` signale que le fichier deposé avait deja ete importe : il est
 * enregistre avec des compteurs a zero, puisqu'aucune ligne n'a ete relue. L'afficher
 * comme « dernier import » ferait donc tomber la carte a « 0 ligne lue » alors que les
 * donnees sont bien en base. On retient le dernier lot ayant reellement ingere le
 * fichier, et on ne retombe sur un lot `Duplicate` que si aucun autre n'existe.
 */
const isEffectiveBatch = (batch: ImportBatchSummary): boolean => {
  const statut = batch.statut.toLowerCase();
  return !statut.includes('encours') && !statut.includes('duplicate');
};

const refreshRealApiStatuses = async (): Promise<void> => {
  const diagnostic = await importsHistoriqueService.getDiagnostic();
  const latestByType = new Map<X3ExcelFileType, ImportBatchSummary>();
  const fallbackByType = new Map<X3ExcelFileType, ImportBatchSummary>();

  diagnostic.derniersImportBatches.forEach((batch) => {
    const fileType = toFileTypeFromBatch(batch);
    if (!fileType || batch.statut.toLowerCase().includes('encours')) return;

    if (isEffectiveBatch(batch)) {
      if (!latestByType.has(fileType)) latestByType.set(fileType, batch);
    } else if (!fallbackByType.has(fileType)) {
      fallbackByType.set(fileType, batch);
    }
  });

  fallbackByType.forEach((batch, fileType) => {
    if (!latestByType.has(fileType)) latestByType.set(fileType, batch);
  });

  for (const [fileType, batch] of latestByType) {
    const status = await statusFromBatch(batch, fileType);
    (['USINAGE_2', 'UAP2'] as X3BilanType[]).forEach((bilanType) => {
      realApiStatuses[bilanType][fileType] = { ...status, bilanType };
    });
  }
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
