export type AuditAction =
  | 'Creation'
  | 'Modification'
  | 'Validation'
  | 'Annulation'
  | 'ImportReussi'
  | 'ImportPartiel'
  | 'ImportEchoue'
  | 'ImportDuplicate'
  | 'Acquittement'
  | 'Resolution'
  | 'Ignorance'
  | 'Recalcul';

export type AuditDomaine =
  | 'InventaireFonderie'
  | 'DechetFonderie'
  | 'ControlePoidsFonderie'
  | 'ReceptionUsinage2'
  | 'RebutUsinage2'
  | 'ImportX3'
  | 'Alerte';

export type AuditJsonValue =
  | string
  | number
  | boolean
  | null
  | AuditJsonValue[]
  | { [key: string]: AuditJsonValue };

export type AuditJsonObject = { [key: string]: AuditJsonValue };

export interface AuditListItem {
  id: string;
  dateUtc: string;
  action: AuditAction;
  domaine: AuditDomaine;
  entite: string;
  entiteId: string;
  referenceMetier: string | null;
  utilisateurNom: string;
  role: string;
  source: string;
  correlationId: string;
  resume: string | null;
}

export interface AuditDetail extends AuditListItem {
  utilisateurId: string;
  utilisateurEmail: string | null;
  anciennesValeurs: AuditJsonObject | null;
  nouvellesValeurs: AuditJsonObject | null;
  informations: AuditJsonObject | null;
}

export interface PagedAuditResponse {
  items: AuditListItem[];
  total: number;
  page: number;
  pageSize: number;
  derniereActiviteUtc: string | null;
}

export interface AuditFilters {
  dateDepuis: string;
  dateJusqua: string;
  utilisateur: string;
  role: string;
  action: '' | AuditAction;
  domaine: '' | AuditDomaine;
  entite: string;
  referenceMetier: string;
  correlationId: string;
  recherche: string;
}
