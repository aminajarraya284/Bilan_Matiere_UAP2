import { getAuthConfig } from '@/config/auth.config';
import { AUTH_TOKEN_STORAGE_KEY } from './auth.session';
import { getMsalService } from './msal.service';

/**
 * Point d'entrée unique utilisé par Axios. Le token Development reste géré par
 * la session locale historique ; le token Entra reste exclusivement dans le
 * cache MSAL.
 */
export const acquireApiAccessToken = async (): Promise<string | null> => {
  const config = getAuthConfig();
  if (config.mode === 'development') return localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
  return getMsalService().acquireAccessToken();
};
