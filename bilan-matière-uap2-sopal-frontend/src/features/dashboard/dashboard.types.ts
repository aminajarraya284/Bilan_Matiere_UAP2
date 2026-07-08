import { BilanStatus } from '@/types/common.types';

export interface DashboardStats {
  inputMatiere: number;
  outputMatiere: number;
  ecartMatiere: number;
  tauxEcart: number;
  periodeLabel: string;
}

export interface DashboardAlert {
  id: number;
  type: 'danger' | 'warning' | 'info';
  title: string;
  description: string;
}

export interface DashboardBilanRow {
  typeMatiere: string;
  inputX3: number;
  encoursFin: number;
  dechets: number;
  usinage2: number;
  poidsNonReceptionne: number;
  ecartKg: number;
  statut: BilanStatus;
}
