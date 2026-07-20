import { BilanStatus } from '@/types/common.types';

export interface DashboardStats {
  inputMatiere: number;
  outputMatiere: number;
  stockDebut?: number;
  sortiesMagasinX3?: number;
  ecartMatiere: number;
  tauxEcart: number;
  periodeLabel: string;
  dechetsFonderie: number;
  encoursFinUtiliseBilan: number;
  receptionsU2: number;
  alertesPoids: number;
  alertesBilan: number;
}

export interface DashboardAlert {
  id: number;
  type: 'danger' | 'warning' | 'info';
  category?: 'weight' | 'bilan';
  title: string;
  description: string;
  actionLabel?: string;
  actionTo?: string;
  numeroOF?: string;
  codeArticle?: string;
  typeMatiere?: string;
  poidsTheoriqueUnitaireKg?: number;
  poidsPhysiqueUnitaireKg?: number;
  tauxEcart?: number;
  ecartKg?: number;
  seuil?: number;
  statut?: string;
}

export interface DashboardBilanRow {
  typeMatiere: string;
  inputX3: number;
  stockDebut?: number;
  sortiesX3?: number;
  encoursFin: number;
  dechets: number;
  usinage2: number;
  poidsNonReceptionne: number;
  ecartKg: number;
  statut: BilanStatus;
}

export type DashboardDataSourceGroup = 'X3' | 'Application';
export type DashboardDataSourceStatusValue = 'À jour' | 'Ancien' | 'Non disponible';

export interface DashboardDataSourceStatus {
  id: string;
  group: DashboardDataSourceGroup;
  name: string;
  updatedAt?: string;
  rowCount?: number;
  status: DashboardDataSourceStatusValue;
}

export interface DashboardFilterOptions {
  typeMatieres: string[];
  numerosOF: string[];
  ofTypeMatieres?: Record<string, string[]>;
}
