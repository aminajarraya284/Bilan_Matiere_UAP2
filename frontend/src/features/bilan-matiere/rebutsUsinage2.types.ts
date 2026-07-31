/**
 * Rebuts declares a l'atelier Usinage 2.
 *
 * CONVENTION D'UNITE : toutes les quantites sont en PIECES, jamais en kilogrammes.
 * Le suffixe `Pieces` est porte par le nom du champ, comme dans le contrat backend.
 */
export type StatutRebutUsinage2 = 'Brouillon' | 'Valide' | 'Annule';

export interface RebutUsinage2 {
  id: string;
  dateDeclaration: string;
  periode: string;
  numeroOF: string;
  codeArticle: string;
  /** Quantite rebutee en pieces. Seuls les rebuts VALIDES alimentent le Bilan U2. */
  quantiteRebutPieces: number;
  motif: string | null;
  observation: string | null;
  statut: StatutRebutUsinage2;
  creePar: string | null;
  creeLeUtc: string;
  validePar: string | null;
  valideLeUtc: string | null;
}

export interface PagedRebutsUsinage2 {
  items: RebutUsinage2[];
  total: number;
}

export interface RebutUsinage2Payload {
  dateDeclaration: string;
  numeroOF: string;
  codeArticle: string;
  quantiteRebutPieces: number;
  motif: string | null;
  observation: string | null;
}

export interface RebutsUsinage2Filters {
  annee?: number;
  mois?: number;
  numeroOF?: string;
  codeArticle?: string;
  page: number;
  pageSize: number;
}
