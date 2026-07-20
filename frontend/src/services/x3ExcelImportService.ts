import * as XLSX from 'xlsx';

export type X3BilanType = 'USINAGE_2' | 'UAP2';
export type X3ExcelFileType = 'MVTSTOTRSVCR' | 'YRJMCH';
export type X3ImportStatus = 'Non importé' | 'Importé' | 'Erreur';

export type X3ImportedSource = {
  bilanType: X3BilanType;
  fileType: X3ExcelFileType;
  fileName: string;
  importedAt: string;
  rowCount: number;
  status: X3ImportStatus;
  error?: string;
  rows: Record<string, string | number | boolean | null>[];
};

export type X3ImportedSourceStatus = Omit<X3ImportedSource, 'rows'>;

const STORAGE_PREFIX = 'sopal_x3_excel_import';
const MAX_EXCEL_FILE_SIZE_BYTES = 20 * 1024 * 1024;

const REQUIRED_MVTSTOTRSVCR_COLUMNS = [
  'Date imput',
  'Code',
  'Désignation',
  'Date crea',
  'Heure crea',
  'Utilisateur',
  'Quantité',
  'Unité',
  'Transaction',
  'Type Pce',
  'Lot',
  'Pièce Origin',
  'Emplacement',
  'Statut'
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

const assertExpectedFileName = (file: File, expectedType: X3ExcelFileType) => {
  const normalizedName = file.name.toUpperCase();
  const otherType: X3ExcelFileType = expectedType === 'MVTSTOTRSVCR' ? 'YRJMCH' : 'MVTSTOTRSVCR';

  if (normalizedName.includes(otherType)) {
    throw new Error(`Le fichier sélectionné semble être ${otherType}. Importez-le dans la carte ${otherType}.`);
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

export const x3ExcelImportService = {
  importMVTSTOTRSVCR(file: File, bilanType: X3BilanType): Promise<X3ImportedSource> {
    return importFile(file, bilanType, 'MVTSTOTRSVCR', REQUIRED_MVTSTOTRSVCR_COLUMNS);
  },

  importYRJMCH(file: File, bilanType: X3BilanType): Promise<X3ImportedSource> {
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
    return getSourceStatus(bilanType, fileType);
  },

  getRows(bilanType: X3BilanType, fileType: X3ExcelFileType): Record<string, string | number | boolean | null>[] {
    return getSource(bilanType, fileType)?.rows ?? [];
  },

  hasBothSources(bilanType: X3BilanType): boolean {
    return (
      getSourceStatus(bilanType, 'MVTSTOTRSVCR').status === 'Importé' &&
      getSourceStatus(bilanType, 'YRJMCH').status === 'Importé'
    );
  }
};
