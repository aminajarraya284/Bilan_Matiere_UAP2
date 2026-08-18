import type { AlerteListItem, PagedAlertesResponse } from '@/features/alertes/alertes.types';
import type {
  BilanFonderieApiResponse,
  BilanUap2ApiLigne,
  BilanUap2ApiResponse,
  BilanUsinage2ApiResponse,
  BilanUsinage2LigneApiResponse
} from '@/types/api.types';

export type RapportCode =
  | 'RPT-001'
  | 'RPT-002'
  | 'RPT-003'
  | 'RPT-004'
  | 'RPT-005'
  | 'RPT-006'
  | 'RPT-007'
  | 'RPT-008';

export interface RapportCatalogueItem {
  code: RapportCode;
  titre: string;
  description: string;
  module: string;
  unitePrincipale: string | null;
  filtres: RapportFilterKey[];
  formats: ('CSV')[];
  disponible: boolean;
  motifIndisponibilite: string | null;
}

export interface RapportMetadata {
  code: RapportCode;
  titre: string;
  genereLeUtc: string;
  periode: string | null;
  unitePrincipale: string | null;
  estPartiel: boolean;
  avertissements: string[];
  generePar: string | null;
  unites: string[];
  sources: string[];
  filtresAppliques: Partial<Record<RapportFilterKey, string | null>>;
}

export interface RapportResponse<T> {
  metadata: RapportMetadata;
  donnees: T;
}

export type RapportFilterKey =
  | 'periode'
  | 'numeroOF'
  | 'date'
  | 'statut'
  | 'severite'
  | 'codeTypeMatiere'
  | 'dateDepuis'
  | 'dateJusqua';

export interface RapportsFilters {
  periode: string;
  numeroOF: string;
  date: string;
  statut: string;
  severite: string;
  codeTypeMatiere: string;
  dateDepuis: string;
  dateJusqua: string;
}

export interface InventaireSyntheseLigne {
  codeTypeMatiere: string;
  partie1Kg: number;
  partie2Kg: number;
  partie3FourKg: number;
  totalRapportInventaireKg: number;
  totalBilanMatiereKg: number;
  statut: string;
}

export interface InventaireSynthese {
  mois: number;
  annee: number;
  lignes: InventaireSyntheseLigne[];
  totalGeneral: Omit<InventaireSyntheseLigne, 'codeTypeMatiere' | 'statut'>;
}

export interface RapportImportLigne {
  id: string;
  typeImport: string;
  profilImport: string | null;
  statut: string;
  nomFichier: string | null;
  dateCreationUtc: string;
  dateFinUtc: string | null;
  lignesLues: number;
  lignesValides: number;
  lignesInserees: number;
  lignesMisesAJour: number;
  lignesImportees: number;
  lignesRejetees: number;
  nombreAnomalies: number;
  creePar: string | null;
  archivageEnErreur: boolean;
}

export interface PagedRapportImports {
  items: RapportImportLigne[];
  total: number;
  page: number;
  pageSize: number;
}

export interface NonReceptionLigne {
  date: string;
  numeroOF: string;
  codeArticle: string | null;
  designationArticle: string | null;
  codeTypeMatiere: string | null;
  totalEmrPieces: number | null;
  rebutEmrPieces: number | null;
  netEmrPieces: number | null;
  quantiteReceptionneeU2Pieces: number;
  piecesNonReceptionnees: number | null;
  statut: string;
  anomalies: string[];
}

export interface NonReceptionResponse {
  date: string;
  lignes: NonReceptionLigne[];
  total: number;
  page: number;
  pageSize: number;
  nombreAVerifier: number;
  nombreSurReceptions: number;
  nombreDonneesManquantes: number;
  totalPiecesNonReceptionnees: number | null;
}

export interface RapportOfDetail {
  metadata: RapportMetadata;
  contexte: {
    numeroOF: string;
    codeArticle: string | null;
    designationArticle: string | null;
    typeMatiere: string | null;
    codeAtelier: string | null;
    groupe: string | null;
    eligibleCalculUap2: boolean;
    statutRapprochement: string;
    anomalies: string[];
  };
  receptions: Array<{
    id: string;
    dateReception: string;
    nombrePiecesReceptionnees: number;
    poidsTotalReceptionneKg: number | null;
    codeArticle: string | null;
    typeMatiere: string | null;
    atelierOrigine: string | null;
    statut: string;
  }>;
  processus: Array<{
    id: string;
    dateProduction: string;
    codePoste: string;
    processus: string;
    numeroOperation: string;
    codeArticle: string;
    quantiteCtrlPieces: number;
    quantiteRebutPieces: number;
  }>;
  mouvements: Array<{
    id: string;
    dateImputation: string;
    codeArticle: string | null;
    quantiteSignee: number;
    unite: string | null;
    transaction: string | null;
    nature: string;
    statutRapprochement: string;
  }>;
  controlesPoids: Array<{
    id: string;
    dateControle: string;
    codeArticle: string;
    typeMatiere: string;
    poidsPhysiqueUnitaireKg: number;
    statut: string;
  }>;
  references: Array<{
    typeReference: string;
    codeArticle: string;
    codeComposant: string | null;
    numeroOperation: string | null;
    codeAtelier: string | null;
    codePoste: string | null;
    quantiteLien: number | null;
    uniteQuantite: string | null;
  }>;
  bilanUsinage2: BilanUsinage2LigneApiResponse | null;
  bilanUap2: BilanUap2ApiLigne | null;
  alertes: AlerteListItem[];
  donneesManquantes: string[];
}

export type RapportApiPayload =
  | RapportResponse<BilanFonderieApiResponse>
  | RapportResponse<BilanUsinage2ApiResponse>
  | RapportResponse<BilanUap2ApiResponse>
  | RapportOfDetail
  | RapportResponse<PagedAlertesResponse>
  | RapportResponse<InventaireSynthese>
  | RapportResponse<PagedRapportImports>
  | RapportResponse<NonReceptionResponse>;

export type RapportCellValue = string | number | boolean | null;

export interface RapportTableColumn {
  label: string;
  unite?: string;
}

export interface RapportTableRow {
  id: string;
  cells: RapportCellValue[];
}

export interface RapportPreview {
  metadata: RapportMetadata;
  columns: RapportTableColumn[];
  rows: RapportTableRow[];
  total: number;
  summary: Array<{ label: string; value: RapportCellValue; unite?: string }>;
}

export interface RapportDownload {
  blob: Blob;
  fileName: string;
}
