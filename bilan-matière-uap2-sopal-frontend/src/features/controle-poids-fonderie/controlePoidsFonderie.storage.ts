export type ControlePoidsPoint = {
  pieces?: number | null;
  poidsBrutKg?: number | null;
  poidsNetKg?: number | null;
};

export type ControlePoidsFonderieRecord = {
  dateControle: string;
  code: string;
  designation: string;
  numeroOF: string;
  atelier: string;
  points?: Record<string, ControlePoidsPoint>;
  totalPiecesControle?: number;
  totalPoidsNetKg?: number;
  poidsPhysiqueUnitaireKg?: number;
  // Ancien nom conservé pour relire les saisies déjà enregistrées.
  poidsPhyKg?: number;
  controleurOperateur: string;
  visa: string;
};

export const CONTROLE_POIDS_FONDERIE_STORAGE_KEY = 'sopal_controles_poids_fonderie';

export const readControlesPoidsFonderie = (): ControlePoidsFonderieRecord[] => {
  if (typeof localStorage === 'undefined') return [];

  try {
    const stored = localStorage.getItem(CONTROLE_POIDS_FONDERIE_STORAGE_KEY);
    if (!stored) return [];
    const parsed = JSON.parse(stored) as unknown;
    if (!Array.isArray(parsed)) {
      localStorage.removeItem(CONTROLE_POIDS_FONDERIE_STORAGE_KEY);
      return [];
    }
    return parsed as ControlePoidsFonderieRecord[];
  } catch {
    localStorage.removeItem(CONTROLE_POIDS_FONDERIE_STORAGE_KEY);
    return [];
  }
};

export const writeControlesPoidsFonderie = (controles: ControlePoidsFonderieRecord[]): void => {
  localStorage.setItem(CONTROLE_POIDS_FONDERIE_STORAGE_KEY, JSON.stringify(controles));
};
