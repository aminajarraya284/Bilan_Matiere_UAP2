export interface MouvementStockSourceRow {
  transactionVcr: string;
  numeroOF: string;
  dateMouvement: string;
  codeTypeMatiere: string;
  sortieMP: number;
}

export interface EncoursProductionSourceRow {
  transactionVcr: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle: string;
}

export interface SortieX3Mouvement {
  id: number;
  dateMouvement: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle: string;
  codeTypeMatiere: string;
  sortieMP: number;
}

export interface ConsommationsImportResult {
  successCount: number;
  mouvementCount: number;
  encoursCount: number;
  unmatchedCount: number;
  consommations: ConsommationComposant[];
}

export interface ConsommationComposant {
  id: string;
  dateImputation: string;
  numeroOF: string;
  articlePrincipal: string;
  designationPrincipale: string | null;
  composantId: string;
  codeComposant: string;
  designationComposant: string | null;
  unite: string;
  sommeQuantiteX3Signee: number;
  consommation: number;
  statut: string;
  importBatchId: string;
}

export interface PagedConsommationsComposantsResponse {
  items: ConsommationComposant[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ImportX3Response {
  importBatchId: string;
  statut: string;
  estDoublon: boolean;
  encoursProduct: {
    lignesLues: number;
    lignesValides: number;
    lignesIgnorees: number;
  };
  mouvementsStock: {
    lignesLues: number;
    lignesValides: number;
    lignesIgnorees: number;
  };
  sortiesGenerees: number;
  nombreAnomalies: number;
  consommations: ConsommationComposant[];
}
