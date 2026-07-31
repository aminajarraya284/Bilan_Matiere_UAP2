export type NatureMouvementStock = 'EntreeOF' | 'SortieMP' | 'NonClassee';

export interface ImportExecutionSummary {
  importBatchId: string;
  statut: string;
  message: string | null;
  nomFichier: string | null;
  dateCreationUtc: string;
  dateFinUtc: string | null;
  lignesLues: number;
  lignesValides: number;
  lignesRejetees: number;
  lignesInserees: number;
  lignesMisesAJour: number;
  lignesInchangees: number;
}

export interface ImportTracking {
  derniereTentative: ImportExecutionSummary | null;
  dernierImportReussi: ImportExecutionSummary | null;
}

export interface MouvementStockItem {
  id: string;
  dateImputation: string;
  numeroOF: string | null;
  codeArticle: string | null;
  designation: string | null;
  quantiteSignee: number;
  unite: string | null;
  transaction: string | null;
  typePiece: string | null;
  pieceOrigine: string | null;
  nature: NatureMouvementStock;
  importBatchId: string;
}

export interface MouvementsStockPage {
  items: MouvementStockItem[];
  total: number;
  page: number;
  pageSize: number;
  imports: ImportTracking;
}

export interface MouvementsStockFilters {
  dateDebut?: string;
  dateFin?: string;
  numeroOF?: string;
  codeArticle?: string;
  designation?: string;
  transaction?: string;
  typePiece?: string;
  pieceOrigine?: string;
  nature?: NatureMouvementStock | '';
  lotImport?: string;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}
