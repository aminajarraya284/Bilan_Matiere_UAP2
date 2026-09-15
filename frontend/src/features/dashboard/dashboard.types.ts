export type DashboardStatus = 'OK' | 'AVerifier' | 'Anomalie' | 'Partiel' | 'NonDisponible';

export type DashboardUnit = 'kg' | 'pieces';

export interface DashboardBilanSummary {
  code: 'FONDERIE' | 'USINAGE2' | 'UAP2';
  ecart: number | null;
  unite: DashboardUnit;
  nombreLignes: number;
  nombreOf: number | null;
  nombreOk: number;
  nombreAVerifier: number;
  nombreAnomalies: number;
  nombreDonneesManquantes: number;
  statut: DashboardStatus;
}

export interface DashboardOperationsSummary {
  inventairesBrouillon: number;
  inventairesValides: number;
  receptionsNonReceptionnees: number;
  receptionsValidees: number;
  controlesPoidsAmbigus: number | null;
  ofReferencesManquantes: number;
}

export interface DashboardFonderieSummary {
  totalInputKg: number | null;
  totalOutputKg: number | null;
  totalEcartKg: number | null;
  tauxEcartPourcent: number | null;
  nombreTypesComplets: number;
  nombreTypesAvecDonneesManquantesOuAnomalies: number;
  types: DashboardFonderieTypeSummary[];
}

export interface DashboardFonderieTypeSummary {
  codeTypeMatiere: string;
  stockDebutKg: number | null;
  sortiesMagasinX3Kg: number | null;
  inputKg: number | null;
  stockFinKg: number | null;
  dechetsFonderieKg: number | null;
  receptionsUsinage2Kg: number | null;
  outputKg: number | null;
  ecartKg: number | null;
  tauxEcartPourcent: number | null;
  anomalies: string[];
  statut: string;
}

export interface DashboardImportSummary {
  reussis: number;
  partiels: number;
  echoues: number;
  fichiersIdentiques: number;
  dernierImportUtc: string | null;
}

export interface DashboardDataQualitySummary {
  nombreAnomalies: number;
  nombreDonneesManquantes: number;
  nombreConversionsImpossibles: number;
  nombreReferencesManquantes: number;
  statutGlobal: DashboardStatus;
}

export interface DashboardWarning {
  code: string;
  source: string;
  message: string;
  statut: DashboardStatus;
}

export interface DashboardResponse {
  periode: string;
  calculeLeUtc: string;
  statutGlobal: DashboardStatus;
  fonderie: DashboardBilanSummary;
  fonderieDetail: DashboardFonderieSummary;
  usinage2: DashboardBilanSummary;
  uap2: DashboardBilanSummary;
  operations: DashboardOperationsSummary;
  imports: DashboardImportSummary;
  qualiteDonnees: DashboardDataQualitySummary;
  avertissements: DashboardWarning[];
}
