import type {
  BilanFonderieResponse,
  BilanFonderieComplet as BilanFonderieCompletResponse,
  BilanUap2Response,
  BilanUsinage2ApiResponse,
  BilanUsinage2LigneApiResponse,
  BilanUsinage2Response
} from '@/types/api.types';

export type BilanMatiereReport = BilanFonderieResponse;
/** Variante du Bilan Fonderie sans composante inconnue (jeu de demonstration). */
export type BilanFonderieComplet = BilanFonderieCompletResponse;
export type BilanUsinage2ParPiece = BilanUsinage2Response;
export type BilanUsinage2Ligne = BilanUsinage2LigneApiResponse;
export type BilanUsinage2Api = BilanUsinage2ApiResponse;
export type BilanUap2ParOF = BilanUap2Response;
