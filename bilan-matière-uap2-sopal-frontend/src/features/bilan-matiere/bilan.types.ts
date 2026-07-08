import { BilanStatus } from '@/types/common.types';

export interface BilanMatiereReport {
  id: number;
  periode: string; // e.g. '2026-06'
  typeMatiere: string;
  encoursDebut: number;
  sortiesX3: number;
  inputTotal: number;
  encoursFinReel: number;
  dechetsFonderie: number;
  receptionsUsinage2: number;
  poidsNonReceptionne: number;
  nombrePiecesNonReceptionnees: number;
  outputTotal: number;
  ecartKg: number;
  tauxEcart: number;
  statut: BilanStatus;
  justification?: string;
  dateCalcul: string;
}

export interface BilanUsinage2ParPiece {
  id: number;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  nombrePieces: number;
  quantiteEmrX3: number;
  quantiteReceptionneeU2: number;
  quantiteNonReceptionnee: number;
  ecartParPiece: number;
  statutControle: 'OK' | 'AVerifier' | 'Anomalie' | 'Justifie' | 'Regularise';
  observation?: string;
  periode: string;
  dateCalcul: string;
}

export interface BilanUap2ParOF {
  id: number;
  of: string;
  codeArticle: string;
  designationArticle?: string;
  codeTypeMatiere: string;
  quantiteSortieMP: number;
  qteCtrl: number;
  poidsPhy: number;
  poidsControle: number;
  rebutEMR: number;
  rebutSCI: number;
  rebutTRN: number;
  totalRebutEmrSciTrn: number;
  quantiteReceptionneeU2: number;
  quantiteNonReceptionnee: number;
  ecartParPiece: number;
  rebutUsinage2: number;
  entreeSF: number;
  outputOF: number;
  ecartBilanOF: number;
  tauxEcartOF: number;
  statut: 'OK' | 'AVerifier' | 'Anomalie' | 'Justifie' | 'Regularise' | 'Cloture';
  observation?: string;
  periode: string;
  dateCalcul: string;
}

