export type DechetType = 'Bavure' | 'Poudre';
export type OrigineDechet = 'Fonderie' | 'SCI' | 'EMR' | 'TRN' | 'U2';
export type StatutDechet = 'Brouillon' | 'Valide' | 'Annule';

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
  origine: OrigineDechet;
  validePar: string | null;
  valideLeUtc: string | null;
  annulePar: string | null;
  annuleLeUtc: string | null;
}

export interface DechetFonderiePayload {
  dateDeclaration: string;
  typeMatiereId: string;
  typeDechet: DechetType;
  poidsKg: number;
  observation: string | null;
  origine: OrigineDechet;
}

export interface DechetsFonderieFilters {
  periode: string;
  typeMatiereId?: string;
  typeDechet?: DechetType;
  origine?: OrigineDechet;
  statut?: StatutDechet;
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
