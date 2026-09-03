export interface AdminParameter {
  cle: string;
  valeur: string;
  description: string;
  modifiable: boolean;
  typeValeur: string;
}

export interface AdminMaterialType {
  id: string;
  code: string;
  libelle: string;
  estActif: boolean;
  dateCreationUtc: string;
  dateModificationUtc: string | null;
}

/**
 * Profil utilisateur local. Ne porte aucun secret : l'identité, le mot de passe et le
 * rôle sont gérés par Microsoft Entra ID. L'application ne décide que de `estActif`.
 */
export interface AdminUserProfile {
  id: number;
  entraObjectId: string;
  email: string | null;
  nomAffichage: string | null;
  /** Rôle porté par le jeton à la dernière connexion. Lecture seule. */
  roleObserve: string | null;
  estActif: boolean;
  dateCreationUtc: string;
  dateModificationUtc: string;
  derniereConnexionUtc: string | null;
}

export interface PagedAdminUsers {
  items: AdminUserProfile[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminUsersQuery {
  recherche?: string;
  estActif?: boolean;
  page?: number;
  pageSize?: number;
}

/** Compte de connexion locale, exposé en lecture seule et uniquement en Development. */
export interface AdminDevelopmentAccount {
  username: string;
  nomAffichage: string;
  email: string;
  role: string;
  /** Faux si le secret n'est pas configuré sur ce poste : le compte est inutilisable. */
  estConfigure: boolean;
}
