export type IsoDateString = string;
export type IsoDateTimeString = string;

export type SourceDonnee = 'Excel' | 'X3Api' | 'SaisieManuelle';

export type BilanType = 'fonderie' | 'usinage2' | 'uap2';

export type StatutControle = 'OK' | 'AVerifier' | 'Anomalie' | 'Justifie' | 'Regularise';

export type StatutSynchronisation = 'Synchronise' | 'ASynchroniser' | 'Erreur' | 'Partiel' | 'NonDisponible';

const normalizeKeyPart = (value: string): string => value.trim().toUpperCase();

export const buildBilanFonderieKey = (periode: string, codeTypeMatiere: string): string =>
  `fonderie:${periode}:${normalizeKeyPart(codeTypeMatiere)}`;

export const buildBilanUsinage2Key = (periode: string, numeroOF: string, codeArticle: string): string =>
  `usinage2:${periode}:${normalizeKeyPart(numeroOF)}:${normalizeKeyPart(codeArticle)}`;

export const buildBilanUap2Key = (periode: string, numeroOF: string): string =>
  `uap2:${periode}:${normalizeKeyPart(numeroOF)}`;
