export type StatutAlerteNonReception =
  | 'OK'
  | 'AVerifier'
  | 'SurReception'
  | 'DonneeManquante';

export interface AlerteNonReceptionLigne {
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
  statut: StatutAlerteNonReception;
  anomalies: string[];
}

export interface AlerteNonReception {
  date: string;
  lignes: AlerteNonReceptionLigne[];
  total: number;
  page: number;
  pageSize: number;
  nombreAVerifier: number;
  nombreSurReceptions: number;
  nombreDonneesManquantes: number;
  totalPiecesNonReceptionnees: number | null;
}
