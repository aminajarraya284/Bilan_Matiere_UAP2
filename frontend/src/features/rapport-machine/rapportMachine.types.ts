export type ProcessusRapportMachine = 'CTRL' | 'SCI' | 'EMR' | 'TRN' | 'Inconnue';

export interface ImportExecutionSummary {
  importBatchId: string;
  statut: string;
  message: string | null;
  nomFichier: string | null;
  dateCreationUtc: string;
  dateFinUtc: string | null;
  lignesLues: number;
  lignesValides: number;
  lignesRejetees: number;
  lignesInserees: number;
  lignesMisesAJour: number;
  lignesInchangees: number;
}

export interface ImportTracking {
  derniereTentative: ImportExecutionSummary | null;
  dernierImportReussi: ImportExecutionSummary | null;
}

export interface RapportMachineItem {
  id: string;
  dateProduction: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle: string | null;
  codePoste: string;
  numeroOperation: string;
  equipe: string | null;
  processus: ProcessusRapportMachine;
  quantiteTotale: number;
  quantiteRebut: number;
  quantiteNette: number;
  importBatchId: string;
}

export interface RapportMachineKpis {
  nombreLignes: number;
  nombreOF: number;
  ctrl: number;
  sci: number;
  emr: number;
  trn: number;
  inconnues: number;
  lignesRejetees: number;
}

export interface RapportMachinePage {
  items: RapportMachineItem[];
  total: number;
  page: number;
  pageSize: number;
  kpis: RapportMachineKpis;
  imports: ImportTracking;
}

export interface RapportMachineFilters {
  dateDebut?: string;
  dateFin?: string;
  numeroOF?: string;
  codeArticle?: string;
  codePoste?: string;
  processus?: ProcessusRapportMachine | '';
  lotImport?: string;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}
