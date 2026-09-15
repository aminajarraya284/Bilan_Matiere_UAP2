export interface ArchiveListItem {
  id: string;
  periode: string;
  dateEnregistrementUtc: string;
  enregistrePar: string;
  statutGlobal: string;
  nombreAlertes: number;
  nombreAnomalies: number;
  nombreDonneesManquantes: number;
  schemaVersion: number;
  versionCalcul: string;
}

export interface PagedArchives {
  items: ArchiveListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ArchivedLine {
  [key: string]: unknown;
  anomalies?: string[];
}

export interface ArchivedBilan {
  periode: string;
  lignes: ArchivedLine[];
  nombreLignes: number;
  nombreAnomalies: number;
  nombreLignesDonneesManquantes: number;
  [key: string]: unknown;
}

export interface ArchiveSnapshot {
  schemaVersion: number;
  versionCalcul: string;
  periode: string;
  dateEnregistrementUtc: string;
  enregistrePar: string;
  statutGlobal: string;
  fonderie: ArchivedBilan;
  usinage2: ArchivedBilan;
  uap2: ArchivedBilan;
  parametresCalcul: Record<string, string | null>;
  anomalies: string[];
}

export interface ArchiveDetail {
  metadata: ArchiveListItem;
  snapshot: ArchiveSnapshot;
}

