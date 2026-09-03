import * as XLSX from 'xlsx';
import api from '@/services/api';
import { testDataRepository } from '@/services/testDataRepository.service';
import type { ImportBatchTest, RefGammeFabrication, RefNomenclature, TypeMatiereCode } from '@/mocks/sopalTestDataset';
import { isApiSource } from '@/config/dataSources';

export type ReferenceTabKey = 'gamme' | 'nomenclature';

export type GammeReference = {
  id: number;
  codeArticle: string;
  designationArticle: string;
  atelier: string;
  poste: string;
  groupePosteCharge: string;
  numeroOperation: string;
  designationOperation: string;
  unite: string;
  alternative: string;
  dateFinValidite: string;
  source: string;
  lotImport: string;
  importeLe: string;
  statut: 'Actif' | 'Brouillon' | 'Importé';
};

export type NomenclatureReference = {
  id: number;
  articleParent: string;
  designationArticleParent: string;
  composant: string;
  designationComposant: string;
  niveau: string;
  sequence: string;
  quantiteLien: string;
  quantiteLienCumulee: string;
  unite: string;
  categorie: string;
  typeLien: string;
  pourcentageRebut: string;
  alternative: string;
  source: string;
  dateFinValidite: string;
  statutValidite: string;
  lotImport: string;
  importeLe: string;
  statut: 'Actif' | 'Brouillon' | 'Importé';
};

export type ReferenceImportResult<T> = {
  items: T[];
  importedCount: number;
  ignoredCount: number;
  fileName: string;
};

type RawRow = unknown[];
type ColumnAliases = Record<string, string[]>;

// Contrats renvoyes par l'API reelle (voir backend ReferencesController + PagedResponse<T>).
type PagedResponse<T> = { items: T[]; total: number };

type GammeApiResponse = {
  codeArticle: string;
  designationArticle: string | null;
  dateFinValidite: string | null;
  etapeSuivi: string | null;
  alternativeGamme: string | null;
  numeroOperation: string;
  codeAtelier: string;
  codePoste: string;
  groupePosteCharge: string | null;
  designationOperation: string | null;
  uniteOperation: string | null;
  cleMetier: string;
  isActive: boolean;
  importBatchId: string;
  sourceSystem: string;
  importedAt: string;
};

type NomenclatureApiResponse = {
  codeArticleParent: string;
  alternativeNomenclature: string | null;
  codeParentNiveau: string;
  codeComposant: string;
  niveau: string;
  sequence: string;
  designationComposant: string | null;
  quantiteLien: number | null;
  quantiteLienCumulee: number | null;
  uniteQuantite: string | null;
  categorieComposant: string | null;
  typeLien: string | null;
  pourcentageRebut: number | null;
  cleMetier: string;
  isActive: boolean;
  importBatchId: string;
  sourceSystem: string;
  importedAt: string;
  dateFinValidite: string | null;
  statutValidite: string;
};

type ImportReferenceApiResponse = { duplicate: boolean; inserted: number; updated: number };

const useRealApi = (): boolean => isApiSource('referencesFabrication');

/**
 * Tailles de page proposees a l'utilisateur. Volontairement bornees : le referentiel
 * complet depasse 260 000 lignes, il ne doit jamais etre rapatrie dans le navigateur.
 */
export const REFERENCE_PAGE_SIZES = [50, 100, 200] as const;
export type ReferencePageSize = (typeof REFERENCE_PAGE_SIZES)[number];
export const DEFAULT_REFERENCE_PAGE_SIZE: ReferencePageSize = 100;

/** Page de resultats telle que rendue par l'ecran : uniquement les lignes visibles. */
export interface ReferencePage<T> {
  items: T[];
  total: number;
}

interface BasePageQuery {
  page: number;
  pageSize: number;
  /** Permet d'annuler la requete en cours lors d'une navigation rapide. */
  signal?: AbortSignal;
}

export interface GammePageQuery extends BasePageQuery {
  recherche?: string;
  codeArticle?: string;
  designation?: string;
  valideLe?: string;
  alternative?: string;
  atelier?: string;
  poste?: string;
  groupe?: string;
  numeroOperation?: string;
  unite?: string;
  source?: string;
  lotImport?: string;
  actif?: boolean;
}

export interface NomenclaturePageQuery extends BasePageQuery {
  recherche?: string;
  codeArticleParent?: string;
  codeComposant?: string;
  designation?: string;
  alternative?: string;
  niveau?: string;
  unite?: string;
  source?: string;
  lotImport?: string;
  actif?: boolean;
}

/** Retire les filtres vides pour ne pas envoyer de parametres inutiles. */
const cleanParams = (params: Record<string, unknown>): Record<string, unknown> =>
  Object.fromEntries(
    Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== '')
  );

/**
 * Recupere UNE page depuis l'API. Remplace l'ancien `fetchAllPaged`, qui bouclait
 * jusqu'a la derniere page (~268 requetes) pour repaginer ensuite cote client.
 */
const fetchPage = async <T>(
  url: string,
  params: Record<string, unknown>,
  signal?: AbortSignal
): Promise<PagedResponse<T>> => {
  const { data } = await api.get<PagedResponse<T>>(url, { params: cleanParams(params), signal });
  return { items: data.items ?? [], total: data.total ?? 0 };
};

const downloadCsv = async (
  url: string,
  params: Record<string, unknown>,
  fileName: string
): Promise<void> => {
  const { data } = await api.get<Blob>(url, {
    params: cleanParams(params),
    responseType: 'blob'
  });
  const objectUrl = URL.createObjectURL(data);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(objectUrl);
};

/**
 * Decalage d'index applique aux identifiants generes cote client : sans lui, deux
 * pages differentes produiraient les memes `id` (1..n), ce qui casserait les cles
 * de rendu Vue lors d'un changement de page.
 */
const offsetOf = (page: number, pageSize: number): number => Math.max(0, (page - 1) * pageSize);

/** Filtrage et decoupage locaux, pour que le mode mock expose le meme contrat pagine. */
const paginateLocally = <T>(rows: T[], page: number, pageSize: number): ReferencePage<T> => {
  const start = offsetOf(page, pageSize);
  return { items: rows.slice(start, start + pageSize), total: rows.length };
};

const matches = (value: string | number | null | undefined, needle?: string): boolean => {
  if (!needle) return true;
  return String(value ?? '').toUpperCase().includes(needle.trim().toUpperCase());
};

const toGammeReferenceFromApi = (gamme: GammeApiResponse, index: number): GammeReference => ({
  id: index + 1,
  codeArticle: gamme.codeArticle,
  designationArticle: gamme.designationArticle ?? '',
  atelier: gamme.codeAtelier,
  poste: gamme.codePoste,
  groupePosteCharge: gamme.groupePosteCharge ?? '',
  numeroOperation: gamme.numeroOperation,
  designationOperation: gamme.designationOperation ?? '',
  unite: gamme.uniteOperation ?? '',
  alternative: gamme.alternativeGamme ?? '',
  dateFinValidite: gamme.dateFinValidite ?? '',
  source: gamme.sourceSystem || 'API',
  lotImport: gamme.importBatchId ?? '',
  importeLe: gamme.importedAt ?? '',
  statut: gamme.isActive ? 'Actif' : 'Brouillon'
});

const toNomenclatureReferenceFromApi = (
  nomenclature: NomenclatureApiResponse,
  index: number
): NomenclatureReference => ({
  id: index + 1,
  articleParent: nomenclature.codeArticleParent,
  designationArticleParent: '',
  composant: nomenclature.codeComposant,
  designationComposant: nomenclature.designationComposant ?? '',
  niveau: nomenclature.niveau,
  sequence: nomenclature.sequence,
  quantiteLien: nomenclature.quantiteLien?.toString() ?? '',
  quantiteLienCumulee: nomenclature.quantiteLienCumulee?.toString() ?? '',
  unite: nomenclature.uniteQuantite ?? '',
  categorie: nomenclature.categorieComposant ?? '',
  typeLien: nomenclature.typeLien ?? '',
  pourcentageRebut: nomenclature.pourcentageRebut?.toString() ?? '',
  alternative: nomenclature.alternativeNomenclature ?? '',
  source: nomenclature.sourceSystem || 'API',
  dateFinValidite: nomenclature.dateFinValidite ?? '',
  statutValidite: nomenclature.statutValidite || 'Règle métier à valider',
  lotImport: nomenclature.importBatchId ?? '',
  importeLe: nomenclature.importedAt ?? '',
  statut: nomenclature.isActive ? 'Actif' : 'Brouillon'
});

const MAX_FILE_SIZE_BYTES = 100 * 1024 * 1024;
const MATERIAL_CODES = new Set<TypeMatiereCode>(['M3000L0', 'M3010L0', 'M3060L0']);

const GAMME_ALIASES: ColumnAliases = {
  codeArticle: ['code article', 'article', 'itmref'],
  designationArticle: ['designation article', 'libelle article', 'itmdes'],
  dateFinValidite: ['date fin validite', 'fin validite', 'date fin'],
  etapeSuivi: ['etape suivi', 'etape'],
  numeroOperation: ['numero operation', 'num operation', 'operation', 'ope'],
  codeAtelier: ['code atelier', 'atelier'],
  codePoste: ['code poste', 'poste'],
  nombrePostes: ['nombre postes', 'nb postes'],
  groupePosteCharge: ['groupe poste charge', 'groupe poste', 'centre charge'],
  designationOperation: ['designation operation', 'libelle operation'],
  alternativeGamme: ['alternative gamme', 'alternative'],
  uniteOperation: ['unite operation', 'unite'],
  coefficientOperationStock: ['coefficient operation stock', 'coefficient stock'],
  tempsCycle: ['temps cycle'],
  trs: ['trs'],
  tempsAttente: ['temps attente'],
  jalon: ['jalon'],
  quantite: ['quantite', 'qte'],
  cadence: ['cadence'],
  dateModificationX3: ['date modification x3', 'date modification'],
  tempsReglage: ['temps reglage'],
  coefficientCCPT: ['coefficient ccpt', 'ccpt']
};

const NOMENCLATURE_ALIASES: ColumnAliases = {
  codeParentNiveau: ['code parent niveau', 'parent niveau'],
  codeComposant: ['code composant', 'composant', 'matiere'],
  codeArticleParent: ['code article parent', 'article parent'],
  designationArticleParent: ['designation article parent', 'designation parent'],
  niveau: ['niveau'],
  designationComposant: ['designation composant', 'libelle composant'],
  sequence: ['sequence', 'seq'],
  quantiteLien: ['quantite lien', 'qte lien'],
  quantiteLienCumulee: ['quantite lien cumulee', 'qte lien cumulee'],
  uniteQuantite: ['unite quantite', 'unite'],
  categorieComposant: ['categorie composant', 'categorie'],
  typeLien: ['type lien'],
  pourcentageRebut: ['pourcentage rebut', 'rebut', 'taux rebut']
};

const normalizeHeader = (value: unknown): string =>
  String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');

const textValue = (value: unknown): string => String(value ?? '').trim();

const numberValue = (value: unknown, fallback = 0): number => {
  if (typeof value === 'number') return Number.isFinite(value) ? value : fallback;
  const normalized = textValue(value)
    .replace(/\s|\u00a0/g, '')
    .replace(',', '.');
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : fallback;
};

const dateValue = (value: unknown, fallback: string): string => {
  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
  }
  if (typeof value === 'number') {
    const parsed = XLSX.SSF.parse_date_code(value);
    if (parsed) {
      return `${parsed.y}-${String(parsed.m).padStart(2, '0')}-${String(parsed.d).padStart(2, '0')}`;
    }
  }
  const text = textValue(value);
  const frenchDate = text.match(/^(\d{1,2})[/-](\d{1,2})[/-](\d{4})$/);
  if (frenchDate) {
    return `${frenchDate[3]}-${frenchDate[2].padStart(2, '0')}-${frenchDate[1].padStart(2, '0')}`;
  }
  const isoDate = text.match(/^(\d{4})-(\d{2})-(\d{2})/);
  return isoDate ? `${isoDate[1]}-${isoDate[2]}-${isoDate[3]}` : fallback;
};

const locateHeaderRow = (rows: RawRow[], aliases: ColumnAliases, requiredFields: string[]): number => {
  const candidates = rows.slice(0, 25);
  let bestIndex = -1;
  let bestScore = 0;

  candidates.forEach((row, index) => {
    const headers = new Set(row.map(normalizeHeader));
    const score = Object.values(aliases).filter((values) =>
      values.some((alias) => headers.has(normalizeHeader(alias)))
    ).length;
    const hasRequired = requiredFields.every((field) =>
      aliases[field].some((alias) => headers.has(normalizeHeader(alias)))
    );
    if (hasRequired && score > bestScore) {
      bestIndex = index;
      bestScore = score;
    }
  });

  if (bestIndex < 0) {
    throw new Error(`En-têtes obligatoires introuvables : ${requiredFields.join(', ')}.`);
  }
  return bestIndex;
};

const parseWorkbook = async (file: File): Promise<RawRow[]> => {
  if (!/\.(xlsx|xls)$/i.test(file.name)) {
    throw new Error('Format non supporté. Importez un fichier Excel .xlsx ou .xls.');
  }
  if (file.size === 0) throw new Error('Le fichier Excel est vide.');
  if (file.size > MAX_FILE_SIZE_BYTES) throw new Error('Le fichier Excel dépasse la taille maximale de 100 Mo.');

  const workbook = XLSX.read(await file.arrayBuffer(), { type: 'array', cellDates: true });
  const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
  if (!firstSheet) throw new Error('Le fichier Excel ne contient aucune feuille.');

  return XLSX.utils.sheet_to_json<RawRow>(firstSheet, { header: 1, defval: '', raw: true });
};

const uniqueRows = <T>(items: T[], keyOf: (item: T) => string): T[] =>
  Array.from(new Map(items.map((item) => [keyOf(item), item])).values());

const createImportBatch = (
  id: string,
  type: 'GAMMEFAB' | 'BOMARTPARENT',
  file: File,
  importedCount: number,
  ignoredCount: number,
  startedAt: string
): ImportBatchTest => {
  return {
    id,
    typeImport: type,
    profilImport: `${type}_EXCEL`,
    sourceSystem: 'Excel',
    nomFichier: file.name,
    fileHash: `${file.name}-${file.size}-${file.lastModified}`,
    dateDebut: startedAt,
    dateFin: new Date().toISOString(),
    nombreLignesLues: importedCount + ignoredCount,
    nombreLignesImportees: importedCount,
    nombreLignesIgnorees: ignoredCount,
    nombreErreurs: ignoredCount,
    statut: ignoredCount > 0 ? 'Partiel' : 'Succes',
    importePar: 'Administrateur SOPAL'
  };
};

const toGammeReference = (gamme: RefGammeFabrication): GammeReference => ({
  id: gamme.id,
  codeArticle: gamme.codeArticle,
  designationArticle: gamme.designationArticle,
  atelier: gamme.codeAtelier,
  poste: gamme.codePoste,
  groupePosteCharge: gamme.groupePosteCharge,
  numeroOperation: String(gamme.numeroOperation),
  designationOperation: gamme.designationOperation,
  unite: gamme.uniteOperation,
  alternative: gamme.alternativeGamme,
  dateFinValidite: gamme.dateFinValidite,
  source: gamme.sourceSystem,
  lotImport: gamme.importBatchId,
  importeLe: '',
  statut: gamme.isActive ? 'Actif' : 'Brouillon'
});

const toNomenclatureReference = (nomenclature: RefNomenclature): NomenclatureReference => ({
  id: nomenclature.id,
  articleParent: nomenclature.codeArticleParent,
  designationArticleParent: nomenclature.designationArticleParent,
  composant: nomenclature.codeComposant,
  designationComposant: nomenclature.designationComposant,
  niveau: String(nomenclature.niveau),
  sequence: String(nomenclature.sequence),
  quantiteLien: nomenclature.quantiteLien.toFixed(3),
  quantiteLienCumulee: nomenclature.quantiteLienCumulee.toFixed(3),
  unite: nomenclature.uniteQuantite,
  categorie: nomenclature.categorieComposant,
  typeLien: nomenclature.typeLien,
  pourcentageRebut: `${nomenclature.pourcentageRebut}`,
  alternative: '',
  source: nomenclature.sourceSystem,
  dateFinValidite: '',
  statutValidite: 'Règle métier à valider',
  lotImport: nomenclature.importBatchId,
  importeLe: '',
  statut: nomenclature.isActive ? 'Actif' : 'Brouillon'
});

export const parseGammeRows = (
  rows: RawRow[],
  importBatchId: string
): { items: RefGammeFabrication[]; ignoredCount: number } => {
  const headerIndex = locateHeaderRow(rows, GAMME_ALIASES, ['codeArticle', 'numeroOperation', 'codeAtelier']);
  // Les exports GAMMEFAB X3 historiques ont parfois des libellés d'en-tête
  // décalés. Le contrat métier officiel reste strictement positionnel A:V.
  const columns: Record<string, number> = {
    codeArticle: 0,
    designationArticle: 1,
    dateFinValidite: 2,
    etapeSuivi: 3,
    numeroOperation: 4,
    codeAtelier: 5,
    codePoste: 6,
    nombrePostes: 7,
    groupePosteCharge: 8,
    designationOperation: 9,
    alternativeGamme: 10,
    uniteOperation: 11,
    coefficientOperationStock: 12,
    tempsCycle: 13,
    trs: 14,
    tempsAttente: 15,
    jalon: 16,
    quantite: 17,
    cadence: 18,
    dateModificationX3: 19,
    tempsReglage: 20,
    coefficientCCPT: 21
  };
  const dataRows = rows.slice(headerIndex + 1).filter((row) => row.some((value) => textValue(value)));
  const valid: RefGammeFabrication[] = [];

  dataRows.forEach((row) => {
    const codeArticle = textValue(row[columns.codeArticle]);
    const numeroOperation = textValue(row[columns.numeroOperation]);
    const codeAtelier = textValue(row[columns.codeAtelier]);
    if (!codeArticle || !numeroOperation || !codeAtelier) return;
    valid.push({
      id: valid.length + 1,
      codeArticle,
      designationArticle: textValue(row[columns.designationArticle]),
      dateFinValidite: dateValue(row[columns.dateFinValidite], '2099-12-31'),
      etapeSuivi: textValue(row[columns.etapeSuivi]) || '1',
      numeroOperation,
      codeAtelier,
      codePoste: textValue(row[columns.codePoste]),
      nombrePostes: numberValue(row[columns.nombrePostes], 1),
      groupePosteCharge: textValue(row[columns.groupePosteCharge]),
      designationOperation: textValue(row[columns.designationOperation]),
      alternativeGamme: textValue(row[columns.alternativeGamme]) || '1',
      uniteOperation: textValue(row[columns.uniteOperation]) || 'PC',
      coefficientOperationStock: numberValue(row[columns.coefficientOperationStock], 1),
      tempsCycle: numberValue(row[columns.tempsCycle]),
      trs: numberValue(row[columns.trs]),
      tempsAttente: numberValue(row[columns.tempsAttente]),
      jalon: textValue(row[columns.jalon]),
      quantite: numberValue(row[columns.quantite], 1),
      cadence: numberValue(row[columns.cadence]),
      dateModificationX3: dateValue(row[columns.dateModificationX3], new Date().toISOString().slice(0, 10)),
      tempsReglage: numberValue(row[columns.tempsReglage]),
      coefficientCCPT: numberValue(row[columns.coefficientCCPT], 1),
      sourceSystem: 'Excel',
      importBatchId,
      isActive: true
    });
  });

  const unique = uniqueRows(valid, (item) => `${item.codeArticle}|${item.numeroOperation}|${item.alternativeGamme}`);
  unique.forEach((item, index) => (item.id = index + 1));
  return { items: unique, ignoredCount: dataRows.length - unique.length };
};

export const parseNomenclatureRows = (
  rows: RawRow[],
  importBatchId: string
): { items: RefNomenclature[]; ignoredCount: number } => {
  const headerIndex = locateHeaderRow(rows, NOMENCLATURE_ALIASES, [
    'codeComposant',
    'codeArticleParent',
    'quantiteLien'
  ]);
  // Le contrat BOMARTPARENT est positionnel A:M, notamment H/I/J pour
  // quantité lien, quantité cumulée et unité.
  const columns: Record<string, number> = {
    codeParentNiveau: 0,
    codeComposant: 1,
    codeArticleParent: 2,
    designationArticleParent: 3,
    niveau: 4,
    designationComposant: 5,
    sequence: 6,
    quantiteLien: 7,
    quantiteLienCumulee: 8,
    uniteQuantite: 9,
    categorieComposant: 10,
    typeLien: 11,
    pourcentageRebut: 12
  };
  const dataRows = rows.slice(headerIndex + 1).filter((row) => row.some((value) => textValue(value)));
  const valid: RefNomenclature[] = [];

  dataRows.forEach((row) => {
    const codeArticleParent = textValue(row[columns.codeArticleParent]);
    const codeComposant = textValue(row[columns.codeComposant]).toUpperCase() as TypeMatiereCode;
    const quantiteLien = numberValue(row[columns.quantiteLien], Number.NaN);
    const uniteQuantite = textValue(row[columns.uniteQuantite]).toUpperCase() || 'KG';
    const categorieComposant = textValue(row[columns.categorieComposant]).toUpperCase() || 'MATIERE';
    const typeLien = textValue(row[columns.typeLien]).toUpperCase() || 'NORMAL';
    if (
      !codeArticleParent ||
      !MATERIAL_CODES.has(codeComposant) ||
      !Number.isFinite(quantiteLien) ||
      uniteQuantite !== 'KG' ||
      categorieComposant !== 'MATIERE' ||
      typeLien !== 'NORMAL'
    ) {
      return;
    }
    valid.push({
      id: valid.length + 1,
      codeParentNiveau: textValue(row[columns.codeParentNiveau]) || codeArticleParent,
      codeComposant,
      codeArticleParent,
      designationArticleParent: textValue(row[columns.designationArticleParent]),
      niveau: numberValue(row[columns.niveau], 1),
      designationComposant: textValue(row[columns.designationComposant]) || `Matière ${codeComposant}`,
      sequence: textValue(row[columns.sequence]) || '10',
      quantiteLien,
      quantiteLienCumulee: numberValue(row[columns.quantiteLienCumulee], quantiteLien),
      uniteQuantite,
      categorieComposant,
      typeLien,
      pourcentageRebut: numberValue(row[columns.pourcentageRebut]),
      sourceSystem: 'Excel',
      importBatchId,
      isActive: true
    });
  });

  const unique = uniqueRows(valid, (item) => `${item.codeArticleParent}|${item.codeComposant}|${item.sequence}`);
  unique.forEach((item, index) => (item.id = index + 1));
  return { items: unique, ignoredCount: dataRows.length - unique.length };
};

export const referenceService = {
  async exportGammes(filters: Omit<GammePageQuery, 'page' | 'pageSize' | 'signal'> = {}): Promise<void> {
    if (!useRealApi()) throw new Error("L'export CSV fiable requiert la source API.");
    await downloadCsv('/v1/references/gammes/export', filters, 'gammes-fabrication.csv');
  },

  async exportNomenclatures(
    filters: Omit<NomenclaturePageQuery, 'page' | 'pageSize' | 'signal'> = {}
  ): Promise<void> {
    if (!useRealApi()) throw new Error("L'export CSV fiable requiert la source API.");
    await downloadCsv('/v1/references/nomenclatures/export', filters, 'nomenclatures.csv');
  },

  /**
   * Une page de gammes. Les filtres sont appliques par le backend
   * (`codeArticle`, `atelier`, `numeroOperation`, `actif`) : aucune ligne hors page
   * n'est rapatriee.
   */
  async getGammes(query: GammePageQuery): Promise<ReferencePage<GammeReference>> {
    const { page, pageSize, signal, ...filters } = query;

    if (useRealApi()) {
      const { items, total } = await fetchPage<GammeApiResponse>(
        '/v1/references/gammes',
        { ...filters, page, pageSize },
        signal
      );
      const offset = offsetOf(page, pageSize);
      return { items: items.map((item, index) => toGammeReferenceFromApi(item, offset + index)), total };
    }

    const rows = testDataRepository
      .getDataset()
      .refGammeFabrication.filter(
        (row) =>
          matches(row.codeArticle, filters.codeArticle) &&
          matches(row.codeAtelier, filters.atelier) &&
          matches(row.numeroOperation, filters.numeroOperation)
      )
      .map(toGammeReference);
    return paginateLocally(rows, page, pageSize);
  },

  /**
   * Une page de nomenclatures. Le backend expose `codeArticleParent`, `codeComposant`
   * et `niveau` ; l'ecran n'en propose pas encore la saisie, mais le service les
   * transmet des qu'ils sont fournis.
   */
  async getNomenclatures(query: NomenclaturePageQuery): Promise<ReferencePage<NomenclatureReference>> {
    const { page, pageSize, signal, ...filters } = query;

    if (useRealApi()) {
      const { items, total } = await fetchPage<NomenclatureApiResponse>(
        '/v1/references/nomenclatures',
        { ...filters, page, pageSize },
        signal
      );
      const offset = offsetOf(page, pageSize);
      return { items: items.map((item, index) => toNomenclatureReferenceFromApi(item, offset + index)), total };
    }

    const rows = testDataRepository
      .getDataset()
      .refNomenclature.filter(
        (row) =>
          matches(row.codeArticleParent, filters.codeArticleParent) &&
          matches(row.codeComposant, filters.codeComposant) &&
          matches(row.niveau, filters.niveau)
      )
      .map(toNomenclatureReference);
    return paginateLocally(rows, page, pageSize);
  },

  async importGammes(file: File): Promise<ReferenceImportResult<GammeReference>> {
    if (useRealApi()) {
      const form = new FormData();
      form.append('file', file);
      const { data } = await api.post<ImportReferenceApiResponse>('/v1/references/gammes/import', form, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return { items: [], importedCount: data.inserted + data.updated, ignoredCount: 0, fileName: file.name };
    }

    const startedAt = new Date().toISOString();
    const rows = await parseWorkbook(file);
    const importBatchId = `IMP-GAMMEFAB-${Date.now()}`;
    const parsed = parseGammeRows(rows, importBatchId);
    if (!parsed.items.length) throw new Error('Aucune référence gamme exploitable trouvée dans le fichier.');

    const dataset = testDataRepository.getDataset();
    dataset.refGammeFabrication = parsed.items;
    dataset.importBatches = [
      createImportBatch(importBatchId, 'GAMMEFAB', file, parsed.items.length, parsed.ignoredCount, startedAt),
      ...dataset.importBatches
    ];
    testDataRepository.replaceDataset(dataset);
    return {
      items: parsed.items.map(toGammeReference),
      importedCount: parsed.items.length,
      ignoredCount: parsed.ignoredCount,
      fileName: file.name
    };
  },

  async importNomenclatures(file: File): Promise<ReferenceImportResult<NomenclatureReference>> {
    if (useRealApi()) {
      const form = new FormData();
      form.append('file', file);
      const { data } = await api.post<ImportReferenceApiResponse>('/v1/references/nomenclatures/import', form, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return { items: [], importedCount: data.inserted + data.updated, ignoredCount: 0, fileName: file.name };
    }

    const startedAt = new Date().toISOString();
    const rows = await parseWorkbook(file);
    const importBatchId = `IMP-BOMARTPARENT-${Date.now()}`;
    const parsed = parseNomenclatureRows(rows, importBatchId);
    if (!parsed.items.length) throw new Error('Aucune référence nomenclature exploitable trouvée dans le fichier.');

    const dataset = testDataRepository.getDataset();
    dataset.refNomenclature = parsed.items;
    dataset.importBatches = [
      createImportBatch(importBatchId, 'BOMARTPARENT', file, parsed.items.length, parsed.ignoredCount, startedAt),
      ...dataset.importBatches
    ];
    testDataRepository.replaceDataset(dataset);
    return {
      items: parsed.items.map(toNomenclatureReference),
      importedCount: parsed.items.length,
      ignoredCount: parsed.ignoredCount,
      fileName: file.name
    };
  }
};
