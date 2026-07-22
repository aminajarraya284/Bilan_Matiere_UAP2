export type DechetType = 'Bavure' | 'Poudre';

export type StatutDechet = 'Valide' | 'Annule';

export interface DechetFonderieResponse {
  id: string;
  dateDeclaration: string;
  semaineIso: number;
  typeMatiereId: string;
  typeMatiereCode: string;
  typeMatiereLibelle: string;
  typeDechet: DechetType;
  poidsKg: number;
  observation: string | null;
  statut: StatutDechet;
  declarant: string | null;
  dateCreationUtc: string;
  dateModificationUtc: string | null;
}

export interface DechetFonderiePayload {
  dateDeclaration: string;
  typeMatiereId: string;
  typeDechet: DechetType;
  poidsKg: number;
  observation: string | null;
}

export interface DechetsFonderieFilters {
  periode: string;
  typeMatiereId?: string;
  typeDechet?: DechetType;
  page?: number;
  pageSize?: number;
}

export interface PagedDechetsFonderieResponse {
  items: DechetFonderieResponse[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ResumeDechetsFonderieItemResponse {
  typeMatiereId: string;
  typeMatiereCode: string;
  totalBavureKg: number;
  totalPoudreKg: number;
  totalKg: number;
}

export interface ResumeDechetsFonderieResponse {
  periode: string;
  totauxParTypeMatiere: ResumeDechetsFonderieItemResponse[];
  totalGeneralKg: number;
}

// Legacy view model retained for the mock consumers used by the other modules.

export interface DechetMouvement {
  id: number;
  dateDeclaration: string;
  semaine: number;
  codeArticle?: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteKg: number;
  unite: 'KG';
  typeDechet: DechetType;
  commentaire?: string;
  statut: 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';
  utilisateur: string;
}
