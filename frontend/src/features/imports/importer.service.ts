import { testDataRepository } from '@/services/testDataRepository.service';
import { buildReceptionDefaults, calculateReceptionWeight } from '@/services/bilanCalculation.service';
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

const getArticleReference = (codeArticle: string) => {
  const dataset = testDataRepository.getDataset();
  const nomenclature = dataset.refNomenclature.find(
    (item) => item.codeArticleParent.toLowerCase() === codeArticle.toLowerCase()
  );
  const ordre = dataset.x3OrdresFabrication.find(
    (item) => item.codeArticleParent.toLowerCase() === codeArticle.toLowerCase()
  );
  return nomenclature
    ? {
        codeArticle,
        designation: nomenclature.designationArticleParent,
        poidsUnitaireKg: nomenclature.quantiteLienCumulee,
        typeMatiere: nomenclature.codeComposant
      }
    : ordre
      ? { codeArticle, designation: ordre.designationArticle, poidsUnitaireKg: 0, typeMatiere: 'M3010L0' as const }
      : undefined;
};

const getOfReference = (numeroOF: string) =>
  testDataRepository
    .getDataset()
    .x3OrdresFabrication.find((of) => of.numeroOF.toLowerCase() === numeroOF.toLowerCase());

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
        if (/^OF\d{8}$/i.test(s)) {
          ofSet.add(s.toUpperCase().replace(/\s+/g, ''));
        }
        if (/^\d{4,}$/.test(s)) {
          ofSet.add(`OF${s.padStart(8, '0')}`);
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
  const dataset = testDataRepository.getDataset();
  const existing = dataset.receptionsUsinage2;
  const startId = existing.length ? Math.max(...existing.map((r) => r.id)) + 1 : 1;
  const now = new Date();
  const mois = options?.month || `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  const user = options?.user || 'IMPORT_TEST';

  const created: ReceptionUsinage2Mouvement[] = ofs.map((of, idx) => {
    const ofRef = getOfReference(of);
    const numeroOF = ofRef?.numeroOF || of;
    const pieces = ofRef
      ? (dataset.x3OperationsOF.find((item) => item.numeroOF === ofRef.numeroOF)?.quantiteRealisee ?? 1)
      : 1;
    const poidsKg = ofRef
      ? Number((buildReceptionDefaults(dataset, numeroOF).poidsTheoriqueUnitaireKg * pieces).toFixed(2))
      : 0;
    const calculated = ofRef
      ? calculateReceptionWeight(dataset, {
          numeroOF,
          dateReception: `${mois}-01`,
          origine: 'FONDERIE',
          poidsTotalReceptionneKg: poidsKg,
          nombrePiecesReceptionnees: pieces
        })
      : null;
    const item = calculated
      ? {
          id: startId + idx,
          ...calculated,
          statut: 'Valide' as const,
          source: 'Excel' as const,
          creePar: user,
          creeLe: now.toISOString()
        }
      : null;
    if (!item)
      return {
        id: startId + idx,
        numeroOF: of,
        dateReception: `${mois}-01`,
        codeArticle: '',
        designationArticle: 'Article importe',
        origine: 'FONDERIE' as const,
        poidsTotalReceptionneKg: 0,
        nombrePiecesReceptionnees: 0,
        atelierOrigine: '',
        codeTypeMatiere: 'M3010L0',
        designationTypeMatiere: 'Matiere importee',
        mois,
        poidsPhysiqueUnitaireKg: 0,
        quantiteLienUtilisee: 0,
        uniteQuantiteLien: 'KG' as const,
        poidsTheoriqueUnitaireKg: 0,
        poidsTheoriqueTotalKg: 0,
        ecartPoidsUnitaireKg: 0,
        tauxEcartPoids: 0,
        statutAlertePoids: 'Conforme' as const,
        seuilAlertePoidsPourcentage: 0,
        gammeFabricationId: 0,
        nomenclatureId: 0,
        statut: 'Brouillon' as const,
        source: 'Excel' as const,
        utilisateur: user,
        creeLe: now.toISOString()
      };
    dataset.receptionsUsinage2.push(item);
    return {
      id: item.id,
      numeroOF: item.numeroOF,
      dateReception: item.dateReception,
      codeArticle: item.codeArticle,
      designationArticle: item.designationArticle,
      origine: item.origine,
      poidsTotalReceptionneKg: item.poidsTotalReceptionneKg,
      nombrePiecesReceptionnees: item.nombrePiecesReceptionnees,
      atelierOrigine: item.atelierOrigine,
      codeTypeMatiere: item.codeTypeMatiere,
      designationTypeMatiere: `Matiere ${item.codeTypeMatiere}`,
      mois: item.mois,
      poidsPhysiqueUnitaireKg: item.poidsPhysiqueUnitaireKg,
      quantiteLienUtilisee: item.quantiteLienUtilisee,
      uniteQuantiteLien: item.uniteQuantiteLien,
      poidsTheoriqueUnitaireKg: item.poidsTheoriqueUnitaireKg,
      poidsTheoriqueTotalKg: item.poidsTheoriqueTotalKg,
      ecartPoidsUnitaireKg: item.ecartPoidsUnitaireKg,
      tauxEcartPoids: item.tauxEcartPoids,
      statutAlertePoids: item.statutAlertePoids,
      seuilAlertePoidsPourcentage: item.seuilAlertePoidsPourcentage,
      gammeFabricationId: item.gammeFabricationId,
      nomenclatureId: item.nomenclatureId,
      statut: item.statut,
      source: item.source,
      utilisateur: item.creePar,
      creeLe: item.creeLe
    };
  });

  testDataRepository.saveReceptions(dataset.receptionsUsinage2);
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
  const dataset = testDataRepository.getDataset();
  const existing = dataset.receptionsUsinage2;
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
        : '0101A03-1';
    const articleRef = getArticleReference(article);
    const poids = parsePoids(mapping.poidsIndex !== undefined ? row[mapping.poidsIndex] : undefined);

    created.push({
      id: startId + idx,
      numeroOF: of,
      dateReception: mois + '-01',
      codeArticle: article,
      designationArticle: articleRef?.designation || 'Article importe',
      poidsTotalReceptionneKg: Number(Number.isFinite(poids) ? poids : 0),
      nombrePiecesReceptionnees: 0,
      atelierOrigine: '19',
      codeTypeMatiere: mapping.codeTypeMatiere || articleRef?.typeMatiere || 'M3010L0',
      designationTypeMatiere: articleRef ? `Matiere ${articleRef.typeMatiere}` : 'Matiere importee',
      mois: mois,
      origine: 'FONDERIE',
      poidsPhysiqueUnitaireKg: 0,
      quantiteLienUtilisee: 0,
      uniteQuantiteLien: 'KG',
      poidsTheoriqueUnitaireKg: articleRef?.poidsUnitaireKg ?? 0,
      poidsTheoriqueTotalKg: 0,
      ecartPoidsUnitaireKg: 0,
      tauxEcartPoids: 0,
      statutAlertePoids: 'Conforme',
      seuilAlertePoidsPourcentage: dataset.parametresApplication.seuilAlertePoidsPourcentage,
      gammeFabricationId: 0,
      nomenclatureId: 0,
      statut: 'Brouillon',
      source: options && options.sync ? 'X3Api' : 'Excel',
      utilisateur: options && options.sync ? 'SYSTEM' : user,
      creeLe: now.toISOString()
    });
    idx++;
  });

  created.forEach((reception) => {
    const of = getOfReference(reception.numeroOF);
    if (!of) return;
    const defaults = buildReceptionDefaults(dataset, of.numeroOF);
    const pieces = 1;
    const poids =
      reception.poidsTotalReceptionneKg > 0 ? reception.poidsTotalReceptionneKg : defaults.poidsTheoriqueUnitaireKg;
    const calculated = calculateReceptionWeight(dataset, {
      numeroOF: of.numeroOF,
      dateReception: reception.dateReception,
      origine: 'FONDERIE',
      poidsTotalReceptionneKg: poids,
      nombrePiecesReceptionnees: pieces
    });
    dataset.receptionsUsinage2.push({
      id: startId + dataset.receptionsUsinage2.length,
      ...calculated,
      statut: 'Valide',
      source: options?.sync ? 'X3Api' : 'Excel',
      creePar: options?.sync ? 'SYSTEM' : user,
      creeLe: now.toISOString()
    });
  });

  // Optional backup in localStorage before overwrite
  if (options && options.backup) {
    try {
      const key = `sopal_receptions_backup_${Date.now()}`;
      localStorage.setItem(key, JSON.stringify(existing));
    } catch {
      // ignore backup failure
    }
  }

  testDataRepository.saveReceptions(dataset.receptionsUsinage2);
  return created;
}
