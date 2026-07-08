import { storage } from '@/utils/storage';
import { ReceptionUsinage2Mouvement } from '@/features/receptions-usinage2/receptions.types';
import type { Sheet } from 'read-excel-file/browser';

export type X3Cell = string | number | boolean;

export type X3ParseResult = {
  sheetName: string;
  headers: string[];
  rows: X3Cell[][];
  ofs: string[];
};

type ReadExcelFileBrowser = (input: File | Blob | ArrayBuffer) => Promise<Sheet<number>[]>;

const normalizeCell = (cell: unknown): X3Cell => {
  if (cell === null || cell === undefined) return '';
  if (cell instanceof Date) return cell.toISOString().slice(0, 10);
  if (typeof cell === 'string' || typeof cell === 'number' || typeof cell === 'boolean') return cell;
  return String(cell);
};

const cellToString = (cell: X3Cell | undefined): string => {
  if (cell === undefined) return '';
  return String(cell).trim();
};

const parsePoids = (cell: X3Cell | undefined): number => {
  if (cell === undefined || cell === '') return 0;
  const value = typeof cell === 'number' ? cell : Number(String(cell).replace(',', '.'));
  return Number.isFinite(value) ? value : 0;
};

export async function parseX3ArrayBuffer(buffer: ArrayBuffer): Promise<X3ParseResult[]> {
  let readExcelFile: ReadExcelFileBrowser;
  try {
    readExcelFile = (await import('read-excel-file/browser')).default;
  } catch {
    throw new Error(
      'La librairie "read-excel-file" est introuvable. Installez-la avec `npm install read-excel-file` ou utilisez le script Node fourni.'
    );
  }

  const sheets = await readExcelFile(buffer);
  const results: X3ParseResult[] = [];

  sheets.forEach(({ sheet, data }) => {
    const raw = data.map((row) => row.map(normalizeCell));
    const headers = raw[0]?.map((h) => cellToString(h)) || [];
    const rows = raw.slice(1);

    const ofSet = new Set<string>();
    rows.forEach((row) => {
      row.forEach((cell) => {
        const s = cellToString(cell);
        if (!s) return;
        if (/OF-?\d{3,}/i.test(s)) {
          ofSet.add(s.toUpperCase().replace(/\s+/g, ''));
        }
        if (/^\d{4,}$/.test(s)) {
          ofSet.add('OF-' + s);
        }
      });
    });

    results.push({ sheetName: sheet, headers, rows, ofs: Array.from(ofSet) });
  });

  return results;
}

export async function parseX3File(file: File): Promise<X3ParseResult[]> {
  const fileName = file.name.toLowerCase();
  if (fileName.endsWith('.xls') && !fileName.endsWith('.xlsx')) {
    throw new Error(
      'Fichier .xls legacy détecté. Cette partie frontend lit les exports .xlsx. Convertissez le fichier X3 en .xlsx, ou branchez le backend pour traiter le format .xls natif.'
    );
  }
  if (!fileName.endsWith('.xlsx')) {
    throw new Error('Format non supporté. Importez un fichier .xlsx exporté depuis X3.');
  }
  const buffer = await file.arrayBuffer();
  return parseX3ArrayBuffer(buffer);
}

export function detectMappingFromHeaders(fileName: string, headers: string[]): ColumnMapping | null {
  const lower = headers.map((h) => (h || '').toString().toLowerCase());
  const mapping: ColumnMapping = { ofIndex: 0 };

  // Try to find OF column
  const ofCandidates = [
    'of',
    'numof',
    'num_of',
    'numeroof',
    'ordre',
    'ordrefabrication',
    'ofnum',
    'ordre_fabrication',
    'of'
  ];
  for (let i = 0; i < lower.length; i++) {
    const h = lower[i];
    if (!h) continue;
    if (ofCandidates.some((c) => h.includes(c))) {
      mapping.ofIndex = i;
      break;
    }
  }

  // poids/quantite
  const poidsCandidates = ['poids', 'weight', 'quantite', 'qty', 'qte', 'poid', 'poids_kg', 'poidskg'];
  for (let i = 0; i < lower.length; i++) {
    const h = lower[i];
    if (!h) continue;
    if (poidsCandidates.some((c) => h.includes(c))) {
      mapping.poidsIndex = i;
      break;
    }
  }

  // article
  const artCandidates = [
    'article',
    'codearticle',
    'code article',
    'articlecode',
    'article_ref',
    'partie',
    'piece',
    'piece_origine',
    'designation'
  ];
  for (let i = 0; i < lower.length; i++) {
    const h = lower[i];
    if (!h) continue;
    if (artCandidates.some((c) => h.includes(c))) {
      mapping.articleIndex = i;
      break;
    }
  }

  // If filename contains known X3 export id, prefer certain heuristics
  const fname = (fileName || '').toLowerCase();
  if (fname.includes('mvts') || fname.includes('mvt') || fname.includes('mvts') || fname.includes('mvstot')) {
    // common columns might be piece_origine / transaction
    for (let i = 0; i < lower.length; i++) {
      const h = lower[i];
      if (!h) continue;
      if (h.includes('piece') && !mapping.articleIndex) mapping.articleIndex = i;
      if (h.includes('transaction') && !mapping.ofIndex) mapping.ofIndex = i;
    }
  }

  // Fallbacks: ensure ofIndex is a number
  if (mapping.ofIndex === undefined || mapping.ofIndex === null) return null;
  return mapping;
}

// Insert minimal receptions entries for testing purposes using OF numbers
export function insertOfsAsReceptions(
  ofs: string[],
  options?: { user?: string; month?: string }
): ReceptionUsinage2Mouvement[] {
  const existing = storage.getReceptions();
  const startId = existing.length ? Math.max(...existing.map((r) => r.id)) + 1 : 1;
  const now = new Date();
  const mois = options?.month || `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  const user = options?.user || 'IMPORT_TEST';

  const created: ReceptionUsinage2Mouvement[] = ofs.map((of, idx) => ({
    id: startId + idx,
    numOf: of,
    dateReception: mois + '-01',
    codeArticle: 'ART-IMPRT',
    designationArticle: 'Article importé',
    poidsKg: 0,
    nombrePieces: 0,
    atelierOrigine: 'FONDERIE',
    codeTypeMatiere: 'M0000',
    designationTypeMatiere: 'Matière importée',
    mois: mois,
    poidsPhysique: 0,
    source: 'Import X3',
    utilisateur: user
  }));

  const merged = existing.concat(created);
  storage.saveReceptions(merged);
  return created;
}

export type ColumnMapping = {
  ofIndex: number;
  articleIndex?: number;
  poidsIndex?: number;
  codeTypeMatiere?: string;
  user?: string;
  month?: string;
};

// Map rows to detailed receptions using selected column indices
export function mapRowsToReceptions(
  headers: string[],
  rows: X3Cell[][],
  mapping: ColumnMapping,
  options?: { sync?: boolean; backup?: boolean }
): ReceptionUsinage2Mouvement[] {
  const existing = storage.getReceptions();
  const startId = existing.length ? Math.max(...existing.map((r) => r.id)) + 1 : 1;
  const now = new Date();
  const mois = mapping.month || `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  const user = mapping.user || 'IMPORT_MAP';

  const created: ReceptionUsinage2Mouvement[] = [];
  let idx = 0;
  rows.forEach((row) => {
    const rawOf = row[mapping.ofIndex];
    if (!rawOf) return;
    const of = cellToString(rawOf).toUpperCase().replace(/\s+/g, '');
    const article =
      mapping.articleIndex !== undefined && row[mapping.articleIndex]
        ? cellToString(row[mapping.articleIndex])
        : 'ART-IMPORT';
    const poids = parsePoids(mapping.poidsIndex !== undefined ? row[mapping.poidsIndex] : undefined);

    created.push({
      id: startId + idx,
      numOf: of,
      dateReception: mois + '-01',
      codeArticle: article,
      designationArticle: 'Article importé',
      poidsKg: Number(Number.isFinite(poids) ? poids : 0),
      nombrePieces: 0,
      atelierOrigine: 'FONDERIE',
      codeTypeMatiere: mapping.codeTypeMatiere || 'M0000',
      designationTypeMatiere: 'Matière importée',
      mois: mois,
      poidsPhysique: Number(Number.isFinite(poids) ? poids : 0),
      source: options && options.sync ? 'Synchronisé ERP' : 'Import X3',
      utilisateur: options && options.sync ? 'SYSTEM' : user
    });
    idx++;
  });

  const merged = existing.concat(created);

  // Optional backup in localStorage before overwrite
  if (options && options.backup) {
    try {
      const key = `sopal_receptions_backup_${Date.now()}`;
      localStorage.setItem(key, JSON.stringify(existing));
    } catch {
      // ignore backup failure
    }
  }

  storage.saveReceptions(merged);
  return created;
}
