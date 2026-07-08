export interface AlertesWeightAlert {
  id: number;
  numOf: string;
  dateReception: string;
  codeArticle: string;
  designationArticle: string;
  codeTypeMatiere: string;
  poidsKg: number;
  poidsPhysique: number;
  difference: number;
  atelierOrigine: string;
  mois: string;
  source: string;
}

export interface AlertesBilanAlert {
  id: number;
  typeMatiere: string;
  inputTotal: number;
  outputTotal: number;
  ecartKg: number;
  tauxEcart: number;
  statut: string;
  justification: string;
}

export interface AlertesSummary {
  totalLostKg: number;
  totalWeightAlerts: number;
  totalBilanAlerts: number;
}
