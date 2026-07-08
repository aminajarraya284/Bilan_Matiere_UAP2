export interface ReceptionUsinage2Mouvement {
  id: number;
  numOf: string;
  dateReception: string;
  codeArticle: string;
  designationArticle: string;
  poidsKg: number;
  nombrePieces: number;
  atelierOrigine: 'FONDERIE' | 'USINAGE 1' | 'AUTRE';
  codeTypeMatiere: string;
  designationTypeMatiere: string;
  mois: string; // e.g., '2026-06'
  poidsPhysique: number;
  source: 'Synchronisé ERP' | 'Saisie Manuelle' | 'Import X3';
  utilisateur: string;
}

export type StatutSynchronisationX3 =
  | 'Synchronise'
  | 'ASynchroniser'
  | 'Erreur'
  | 'Partiel'
  | 'NonDisponible';

export type StatutControleNonReception =
  | 'OK'
  | 'AVerifier'
  | 'Anomalie'
  | 'Justifie'
  | 'Regularise';

export interface SourceSynchronisationX3 {
  id: string;
  nom: string;
  statut: StatutSynchronisationX3;
  derniereSynchronisation?: string;
  nombreLignes?: number;
  nombreErreurs?: number;
  source: 'X3' | 'RapportMachine';
}

export interface DonneeApplicationStatus {
  id: string;
  nom: string;
  origine: 'Application';
  derniereMiseAJour?: string;
  statut: 'OK' | 'ACompleter' | 'Erreur' | 'NonDisponible';
  nombreLignes?: number;
}

export interface ControleNonReception {
  id: number;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  nombrePieces: number;
  quantiteEmrX3: number;
  quantiteReceptionneeU2Application: number;
  quantiteNonReceptionnee: number;
  ecartParPiece: number;
  statutControle: StatutControleNonReception;
  observation?: string;
  justification?: string;
  dateSynchronisationX3?: string;
  // Compatibility with old views if any:
  of?: string;
  sourceEmr?: 'RapportJournalierMachine' | 'EtatX3Synchronise';
}

// Keep old names or alias them to avoid compile errors in other areas
export type StatutControleNonReceptionOld =
  | 'AucunDecalage'
  | 'AVerifier'
  | 'DecalageImportant'
  | 'Justifie'
  | 'Regularise'
  | 'Anomalie';

export interface ControleNonReceptionU2 extends ControleNonReception {
  of: string;
  quantiteReceptionneeU2: number;
  sourceEmr: 'RapportJournalierMachine' | 'EtatX3Synchronise';
}

export interface VerificationControleNonReceptionPayload {
  statutControle: StatutControleNonReception;
  commentaireControle?: string;
  justification?: string;
  observation?: string;
}
