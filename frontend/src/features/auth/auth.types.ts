import type { AuthMode } from '@/config/auth.config';
import type { ApplicationRole } from './auth.roles';

export type { AuthUser, LoginRequest, LoginResponse } from '@/types/api.types';
export type { ApplicationRole } from './auth.roles';

/** Modèle commun exposé au reste de l'application, indépendant de MSAL. */
export interface AuthenticatedUser {
  id: string;
  displayName: string;
  email?: string;
  roles: ApplicationRole[];
  authenticationMode: AuthMode;

  // Alias de compatibilité avec les composants historiques, à retirer lorsque
  // tous les écrans consommeront displayName/roles directement.
  fullName: string;
  username: string;
  role?: ApplicationRole;
  status: 'Actif';
}
