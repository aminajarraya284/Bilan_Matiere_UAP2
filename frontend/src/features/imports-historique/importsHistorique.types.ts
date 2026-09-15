export interface ImportBatchSummary {
  id: string;
  hashFichierSha256: string;
  typeImport: string;
  profilImport: string | null;
  statut: string;
  nombreLignesLues: number;
  nombreLignesValides: number;
  nombreLignesImportees: number;
  nombreAnomalies: number;
  readCount: number;
  validCount: number;
  insertedCount: number;
  updatedCount: number;
  unchangedCount: number;
  rejectedCount: number;
  archiveErrorMessage: string | null;
  creeParUtilisateurId: string | null;
  creeParNom: string | null;
  dateCreationUtc: string;
  dateFinUtc: string | null;
}

export interface ImportsReelsDiagnostic {
  gammesActives: number;
  nomenclaturesActives: number;
  rapportsMachine: number;
  derniersImportBatches: ImportBatchSummary[];
}

export interface ImportSourceFile {
  id: string;
  importBatchId: string;
  typeImport: string;
  profilImport: string | null;
  nomFichierOriginal: string;
  extension: string;
  tailleOctets: number;
  hashSha256: string;
  dateUploadUtc: string;
  uploadeParNom: string | null;
}
