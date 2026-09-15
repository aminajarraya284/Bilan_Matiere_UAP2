export type AlerteStatut = 'Ouverte' | 'Acquittee' | 'Resolue' | 'Ignoree';

export type AlerteSeverite = 'Information' | 'Avertissement' | 'Erreur';

export type AlerteType = 'DonneeManquante' | 'AnomalieMetier' | 'AnomalieTechnique';

export interface AlerteListItem {
  id: string;
  code: string;
  type: AlerteType;
  severite: AlerteSeverite;
  statut: AlerteStatut;
  titre: string;
  source: string;
  periode: string | null;
  numeroOF: string | null;
  codeArticle: string | null;
  typeMatiere: string | null;
  detecteeLeUtc: string;
  derniereDetectionLeUtc: string;
  description?: string | null;
  acquitteeLeUtc?: string | null;
  acquitteePar?: string | null;
  resolueLeUtc?: string | null;
  resoluePar?: string | null;
  ignoreeLeUtc?: string | null;
  ignoreePar?: string | null;
  commentaireTraitement?: string | null;
}

export interface AlerteDetail extends AlerteListItem {
  description: string;
  acquitteeLeUtc: string | null;
  acquitteePar: string | null;
  resolueLeUtc: string | null;
  resoluePar: string | null;
  ignoreeLeUtc: string | null;
  ignoreePar: string | null;
  commentaireTraitement: string | null;
  resolutionAutomatique: boolean;
  donneesAssociees: Readonly<Record<string, string | null>>;
}

export interface AlerteSummary {
  total: number;
  ouvertes: number;
  acquittees: number;
  resolues: number;
  ignorees: number;
}

export interface PagedAlertesResponse {
  items: AlerteListItem[];
  total: number;
  page: number;
  pageSize: number;
  summary: AlerteSummary;
  derniereDetectionLeUtc: string | null;
}

export interface AlertesFilters {
  periode: string;
  statut: AlerteStatut | '';
  severite: AlerteSeverite | '';
  source: string;
  numeroOF: string;
  codeArticle: string;
  recherche: string;
}

export interface AlerteRecalculResult {
  periode: string;
  recalculeLeUtc: string;
  nombreDetections: number;
  nombreCreees: number;
  nombreMisesAJour: number;
  nombreResoluesAutomatiquement: number;
}

export interface TraiterAlerteRequest {
  commentaire: string | null;
}

export interface IgnorerAlerteRequest {
  commentaire: string;
}
