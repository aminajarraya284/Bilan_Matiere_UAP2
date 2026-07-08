import { X3_REFERENCE_URL, X3_REFERENCE_WORKBOOK_URL } from '@/data/x3Reference';
import type { X3Cell, X3ParseResult } from '@/features/imports/importer.service';

export type X3ReferenceSortie = {
  of: string;
  composant: string;
  qtePrevue: number;
  qteRealisee: number;
  qteRestante: number;
  qteBesoin: number;
  qteConsommee: number;
};

export type X3ReferenceArticle = {
  code: string;
  designation: string;
  atelier: string;
  poste: string;
};

export type X3ReferenceMaterial = {
  code: string;
  designation: string;
};

export type X3ReferenceData = {
  sourceFile: string;
  sheets: {
    LISTEOFX3: {
      rawRows: number;
      aggregatedRows: number;
      uniqueOf: number;
      uniqueComponents: number;
    };
    GAMME: {
      rawRows: number;
      uniqueArticles: number;
      uniqueAteliers: number;
      uniquePostes: number;
    };
    BOM: {
      rawRows: number;
      uniqueMaterials: number;
    };
  };
  sortiesX3: X3ReferenceSortie[];
  articles: X3ReferenceArticle[];
  materials: X3ReferenceMaterial[];
  ateliers: string[];
  postes: string[];
};

let cachedReference: X3ReferenceData | null = null;

const toCell = (value: string | number): X3Cell => {
  return typeof value === 'number' ? value : value || '';
};

export const x3ReferenceService = {
  async getReference(): Promise<X3ReferenceData> {
    if (cachedReference) return cachedReference;

    const response = await fetch(X3_REFERENCE_URL);
    if (!response.ok) {
      throw new Error('Impossible de charger les données de référence X3 intégrées.');
    }

    cachedReference = (await response.json()) as X3ReferenceData;
    return cachedReference;
  },

  async getReferenceAsParseResult(): Promise<X3ParseResult[]> {
    const reference = await this.getReference();
    const rows = reference.sortiesX3.map((row) => [
      toCell(row.of),
      toCell(row.composant),
      toCell(row.qtePrevue),
      toCell(row.qteRealisee),
      toCell(row.qteRestante),
      toCell(row.qteBesoin),
      toCell(row.qteConsommee)
    ]);

    return [
      {
        sheetName: 'LISTEOFX3 référence intégrée',
        headers: ['OF', 'Composant', 'Qté prévue', 'Qté Réalisé', 'Qté Restante', 'Qté Besoin', 'Qté consommée'],
        rows,
        ofs: Array.from(new Set<string>(reference.sortiesX3.map((row) => row.of))).sort()
      }
    ];
  },

  getWorkbookUrl(): string {
    return X3_REFERENCE_WORKBOOK_URL;
  }
};
