import type { SourceDonnee } from '@/types/domain.types';

/** Types conservés uniquement pour les jeux de démonstration historiques hors module /alertes. */
export interface AlertesWeightAlert {
  id: number;
  numeroOF: string;
  dateReception: string;
  codeArticle: string;
  designationArticle: string;
  codeTypeMatiere: string;
  poidsTheoriqueTotalKg: number;
  poidsTotalReceptionneKg: number;
  poidsKg?: number;
  poidsPhysique?: number;
  poidsTheoriqueUnitaireKg?: number;
  poidsPhysiqueUnitaireKg?: number;
  ecartPoidsUnitaireKg?: number;
  tauxEcartPoids?: number;
  difference: number;
  atelierOrigine: string;
  mois: string;
  source: SourceDonnee;
}

export interface AlertesBilanAlert {
  id: number;
  typeMatiere: string;
  inputTotal: number;
  outputTotal: number;
  ecartKg: number;
  tauxEcart: number;
  statut: string;
  justification?: string;
}
