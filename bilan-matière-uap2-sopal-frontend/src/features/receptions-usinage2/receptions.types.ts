import type { ReceptionU2Response, VerifyControleNonReceptionRequest } from '@/types/api.types';
import type { StatutControle, StatutSynchronisation } from '@/types/domain.types';

export type ReceptionUsinage2Mouvement = ReceptionU2Response;

export type StatutSynchronisationX3 = StatutSynchronisation;

export type StatutControleNonReception = StatutControle;

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
  bilanKey: string;
  numeroOF: string;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteEmrPieces: number;
  quantiteReceptionneeU2Pieces: number;
  quantiteNonReceptionneePieces: number;
  poidsNonReceptionneEstimeKg: number;
  statutControle: StatutControleNonReception;
  observation?: string;
  justification?: string;
  dateSynchronisationX3?: string;
  sourceEmr?: 'RapportJournalierMachine' | 'EtatX3Synchronise';
}

export type VerificationControleNonReceptionPayload = VerifyControleNonReceptionRequest;
