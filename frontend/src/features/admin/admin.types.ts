import { UserRole } from '@/types/common.types';

export interface AdminUser {
  id: number;
  fullName: string;
  role: UserRole;
  email: string;
  actif: boolean;
}

export interface AdminArticle {
  id: number;
  code: string;
  designation: string;
  typeMatiere: string;
  unite: string;
  actif: boolean;
}

export interface AdminSettings {
  seuilAlerteEcart: number; // e.g. 3 %
  seuilCritiqueEcart: number; // e.g. 5 %
  toleranceAlertePoids: number; // e.g. 500 kg
  frequenceSyncX3: string; // e.g. 'Toutes les 4 heures'
}
