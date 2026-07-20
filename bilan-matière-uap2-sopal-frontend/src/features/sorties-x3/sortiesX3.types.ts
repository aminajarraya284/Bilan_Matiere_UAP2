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
}
