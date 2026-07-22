import * as XLSX from 'xlsx';
import { testDataRepository } from '@/services/testDataRepository.service';
import type {
  ConsommationsImportResult,
  EncoursProductionSourceRow,
  MouvementStockSourceRow,
  SortieX3Mouvement
} from './sortiesX3.types';

const STORAGE_KEY = 'sopal_consommations_mvt_encours_rows';
const MAX_EXCEL_FILE_SIZE_BYTES = 20 * 1024 * 1024;

type ColumnRole =
  | 'transactionVcr'
  | 'numeroOF'
  | 'dateMouvement'
  | 'codeTypeMatiere'
  | 'sortieMP'
  | 'codeArticle'
  | 'designationArticle';
type ColumnIndexes = Partial<Record<ColumnRole, number>>;

const HEADER_ALIASES: Record<ColumnRole, string[]> = {
  transactionVcr: [
    'transvcr',
    'transactionvcr',
    'transaction',
    'vcr',
    'numtransvcr',
    'numerotransvcr',
    'notransvcr',
    'referencevcr',
    'referenceorigine',
    'referenceoriginex3'
  ],
  numeroOF: [
    'of',
    'nof',
    'numof',
    'numeroof',
    'ordre',
    'numordre',
    'numeroordre',
    'ordrefabrication',
    'ordredefabrication',
    'pieceorigin',
    'pieceorigine'
  ],
  dateMouvement: ['date', 'dateimput', 'dateimputation', 'datemouvement', 'datemvt', 'datecreation', 'datecrea'],
  codeTypeMatiere: ['code', 'codematiere', 'typematiere', 'codetypematiere', 'matiere', 'articlematiere', 'composant'],
  sortieMP: [
    'sortiemp',
    'quantite',
    'qte',
    'quantitesortie',
    'qtesortie',
    'quantitemp',
    'qtemp',
    'quantiteconsommee',
    'qteconsommee'
  ],
  codeArticle: ['article', 'codearticle', 'articlelance', 'produit', 'codeproduit', 'articleparent'],
  designationArticle: [
    'designation',
    'designationarticle',
    'designationproduit',
    'libelle',
    'libellearticle',
    'libelleproduit'
  ]
};

const text = (value: unknown): string => String(value ?? '').trim();

export const normalizeHeader = (value: unknown): string =>
  text(value)
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');

const normalizeJoinKey = (value: unknown): string => normalizeHeader(value);

export const normalizeOF = (value: unknown): string => {
  const raw = text(value).toUpperCase().replace(/\s+/g, '');
  if (!raw) return '';
  if (/^OF[\w-]+$/.test(raw)) return raw;
  return /^\d{4,}$/.test(raw) ? `OF${raw}` : raw;
};

export const toNumber = (value: unknown): number => {
  if (value === null || value === undefined || value === '') return 0;
  if (typeof value === 'number') return Number.isFinite(value) ? value : 0;

  const normalized = text(value).replace(/\s/g, '').replace(/kg$/i, '').replace(',', '.');
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
};

export const parseExcelDate = (value: unknown): string => {
  if (value instanceof Date && !Number.isNaN(value.getTime())) return value.toISOString().slice(0, 10);

  if (typeof value === 'number') {
    const date = XLSX.SSF.parse_date_code(value);
    if (!date) return '';
    return `${date.y}-${String(date.m).padStart(2, '0')}-${String(date.d).padStart(2, '0')}`;
  }

  const raw = text(value);
  const iso = /^(\d{4})-(\d{2})-(\d{2})/.exec(raw);
  if (iso) return `${iso[1]}-${iso[2]}-${iso[3]}`;

  const french = /^(\d{2})[/-](\d{2})[/-](\d{4})$/.exec(raw);
  if (french) return `${french[3]}-${french[2]}-${french[1]}`;

  return raw;
};

const findColumnIndexes = (headers: unknown[]): ColumnIndexes => {
  const normalizedHeaders = headers.map(normalizeHeader);
  const indexes: ColumnIndexes = {};

  (Object.keys(HEADER_ALIASES) as ColumnRole[]).forEach((role) => {
    const index = normalizedHeaders.findIndex((header) => HEADER_ALIASES[role].includes(header));
    if (index >= 0) indexes[role] = index;
  });

  return indexes;
};

const hasMouvementColumns = (indexes: ColumnIndexes): boolean =>
  indexes.dateMouvement !== undefined &&
  indexes.codeTypeMatiere !== undefined &&
  indexes.sortieMP !== undefined &&
  (indexes.transactionVcr !== undefined || indexes.numeroOF !== undefined);

const hasEncoursColumns = (indexes: ColumnIndexes): boolean =>
  indexes.numeroOF !== undefined &&
  indexes.codeArticle !== undefined &&
  indexes.designationArticle !== undefined &&
  indexes.transactionVcr !== undefined;

const findHeader = (
  sheetRows: unknown[][],
  validator: (indexes: ColumnIndexes) => boolean,
  sourceLabel: string
): { rowIndex: number; indexes: ColumnIndexes } => {
  const scanLimit = Math.min(sheetRows.length, 15);

  for (let rowIndex = 0; rowIndex < scanLimit; rowIndex += 1) {
    const indexes = findColumnIndexes(sheetRows[rowIndex] ?? []);
    if (validator(indexes)) return { rowIndex, indexes };
  }

  throw new Error(`Colonnes obligatoires introuvables dans le fichier « ${sourceLabel} ».`);
};

export const parseMouvementStockRows = (sheetRows: unknown[][]): MouvementStockSourceRow[] => {
  const { rowIndex, indexes } = findHeader(sheetRows, hasMouvementColumns, 'Mvt de stock par trans-VCR');

  return sheetRows
    .slice(rowIndex + 1)
    .filter((row) => row.some((cell) => text(cell) !== ''))
    .map((row) => ({
      transactionVcr: indexes.transactionVcr === undefined ? '' : text(row[indexes.transactionVcr]),
      numeroOF: indexes.numeroOF === undefined ? '' : normalizeOF(row[indexes.numeroOF]),
      dateMouvement: parseExcelDate(row[indexes.dateMouvement!]),
      codeTypeMatiere: text(row[indexes.codeTypeMatiere!]),
      sortieMP: toNumber(row[indexes.sortieMP!])
    }))
    .filter((row) => row.transactionVcr || row.numeroOF);
};

export const parseEncoursProductionRows = (sheetRows: unknown[][]): EncoursProductionSourceRow[] => {
  const { rowIndex, indexes } = findHeader(sheetRows, hasEncoursColumns, 'Encours production');

  return sheetRows
    .slice(rowIndex + 1)
    .filter((row) => row.some((cell) => text(cell) !== ''))
    .map((row) => ({
      transactionVcr: text(row[indexes.transactionVcr!]),
      numeroOF: normalizeOF(row[indexes.numeroOF!]),
      codeArticle: text(row[indexes.codeArticle!]),
      designationArticle: text(row[indexes.designationArticle!])
    }))
    .filter((row) => row.numeroOF && row.codeArticle);
};

export const mergeConsommationRows = (
  mouvements: MouvementStockSourceRow[],
  encoursRows: EncoursProductionSourceRow[]
): { rows: SortieX3Mouvement[]; unmatchedCount: number } => {
  const encoursByTransaction = new Map<string, EncoursProductionSourceRow>();
  const encoursByOF = new Map<string, EncoursProductionSourceRow>();

  encoursRows.forEach((row) => {
    const transactionKey = normalizeJoinKey(row.transactionVcr);
    const ofKey = normalizeJoinKey(row.numeroOF);
    if (transactionKey && !encoursByTransaction.has(transactionKey)) encoursByTransaction.set(transactionKey, row);
    if (ofKey && !encoursByOF.has(ofKey)) encoursByOF.set(ofKey, row);
  });

  let unmatchedCount = 0;
  const grouped = new Map<string, Omit<SortieX3Mouvement, 'id'>>();

  mouvements.forEach((mouvement) => {
    const encours =
      encoursByTransaction.get(normalizeJoinKey(mouvement.transactionVcr)) ??
      encoursByOF.get(normalizeJoinKey(mouvement.numeroOF));

    if (!encours) {
      unmatchedCount += 1;
      return;
    }

    const key = [
      mouvement.dateMouvement,
      encours.numeroOF,
      encours.codeArticle,
      encours.designationArticle,
      mouvement.codeTypeMatiere
    ]
      .map(normalizeJoinKey)
      .join('|');
    const existing = grouped.get(key);

    if (existing) {
      existing.sortieMP += mouvement.sortieMP;
    } else {
      grouped.set(key, {
        dateMouvement: mouvement.dateMouvement,
        numeroOF: encours.numeroOF,
        codeArticle: encours.codeArticle,
        designationArticle: encours.designationArticle,
        codeTypeMatiere: mouvement.codeTypeMatiere,
        sortieMP: mouvement.sortieMP
      });
    }
  });

  const rows = Array.from(grouped.values())
    .sort(
      (a, b) =>
        a.dateMouvement.localeCompare(b.dateMouvement) ||
        a.numeroOF.localeCompare(b.numeroOF, 'fr', { numeric: true }) ||
        a.codeTypeMatiere.localeCompare(b.codeTypeMatiere, 'fr', { numeric: true })
    )
    .map((row, index) => ({ id: index + 1, ...row }));

  return { rows, unmatchedCount };
};

const readFirstSheet = async (file: File): Promise<unknown[][]> => {
  if (file.size === 0) throw new Error(`Le fichier « ${file.name} » est vide.`);
  if (file.size > MAX_EXCEL_FILE_SIZE_BYTES) {
    throw new Error(`Le fichier « ${file.name} » dépasse la taille maximale autorisée de 20 Mo.`);
  }

  if (!/\.(xlsx|xls)$/i.test(file.name)) {
    throw new Error(`Le fichier « ${file.name} » doit être au format .xlsx ou .xls.`);
  }

  const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array', cellDates: true });
  const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
  if (!firstSheet) throw new Error(`Le fichier « ${file.name} » ne contient aucune feuille.`);

  return XLSX.utils.sheet_to_json<unknown[]>(firstSheet, { header: 1, defval: '' });
};

const getStoredRows = (): SortieX3Mouvement[] => {
  try {
    const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]') as unknown;
    if (!Array.isArray(parsed)) {
      localStorage.removeItem(STORAGE_KEY);
      return [];
    }
    return parsed as SortieX3Mouvement[];
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return [];
  }
};

const mockRows = (): SortieX3Mouvement[] => {
  const dataset = testDataRepository.getDataset();
  const mouvements = dataset.x3MouvementsStock.map((row) => ({
    transactionVcr: row.referenceOrigineX3,
    numeroOF: row.numeroOF,
    dateMouvement: row.dateImputation,
    codeTypeMatiere: row.codeArticle,
    sortieMP: row.quantite
  }));
  const encoursRows = dataset.x3OrdresFabrication.map((row) => ({
    transactionVcr: '',
    numeroOF: row.numeroOF,
    codeArticle: row.codeArticleParent,
    designationArticle: row.designationArticle
  }));

  return mergeConsommationRows(mouvements, encoursRows).rows;
};

export const sortiesX3Service = {
  async getMouvements(): Promise<SortieX3Mouvement[]> {
    const stored = getStoredRows();
    return stored.length ? stored : mockRows();
  },

  async importExcelFiles(mouvementFile: File, encoursFile: File): Promise<ConsommationsImportResult> {
    const [mouvementSheet, encoursSheet] = await Promise.all([
      readFirstSheet(mouvementFile),
      readFirstSheet(encoursFile)
    ]);
    const mouvements = parseMouvementStockRows(mouvementSheet);
    const encoursRows = parseEncoursProductionRows(encoursSheet);
    const merged = mergeConsommationRows(mouvements, encoursRows);

    if (!mouvements.length) throw new Error('Aucun mouvement de stock exploitable n’a été trouvé.');
    if (!encoursRows.length) throw new Error('Aucun encours de production exploitable n’a été trouvé.');
    if (!merged.rows.length) {
      throw new Error('Aucun rapprochement trouvé entre les transactions VCR et les encours de production.');
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify(merged.rows));
    return {
      successCount: merged.rows.length,
      mouvementCount: mouvements.length,
      encoursCount: encoursRows.length,
      unmatchedCount: merged.unmatchedCount
    };
  },

  async triggerSynchroniser(): Promise<{ count: number }> {
    return { count: (await this.getMouvements()).length };
  }
};
