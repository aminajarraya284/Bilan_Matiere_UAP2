export type UserRole = 'Admin' | 'ResponsableFonderie' | 'ResponsableUsinage2';

export type BilanStatus = 'Brouillon' | 'EnControle' | 'AvecEcart' | 'Valide' | 'Cloture';

export type ValidationStatus = 'Brouillon' | 'AValider' | 'Valide' | 'Cloture';

export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface TypeMatiere {
  id: number;
  code: string;
  designation: string;
  actif: boolean;
}

export interface Article {
  id: number;
  code: string;
  designation: string;
  typeMatiereId: number;
  unite: string;
  actif: boolean;
}

export interface Periode {
  id: number;
  mois: number;
  annee: number;
  libelle: string;
  statut: BilanStatus;
}
