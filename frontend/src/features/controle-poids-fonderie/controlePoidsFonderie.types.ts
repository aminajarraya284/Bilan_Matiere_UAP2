export type StatutControlePoidsFonderie = 'Brouillon' | 'Valide' | 'Annule';

export interface ControlePoidsFonderieApi {
  id: string;
  dateControle: string;
  numeroOF: string | null;
  codeArticle: string;
  typeMatiereId: string;
  typeMatiereCode: string;
  numeroLotOuSerie: string | null;
  poidsPhysiqueUnitaireKg: number;
  nombreEchantillons: number | null;
  observation: string | null;
  statut: StatutControlePoidsFonderie;
  creePar: string | null;
  creeLeUtc: string;
  validePar: string | null;
  valideLeUtc: string | null;
}

export interface CreateControlePoidsFonderieRequest {
  dateControle: string;
  numeroOF: string | null;
  codeArticle: string;
  typeMatiereId: string;
  numeroLotOuSerie: string | null;
  poidsPhysiqueUnitaireKg: number;
  nombreEchantillons: number | null;
  observation: string | null;
}

export type UpdateControlePoidsFonderieRequest = CreateControlePoidsFonderieRequest;
