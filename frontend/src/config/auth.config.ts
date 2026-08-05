import { AuthConfigurationError } from '@/features/auth/auth.errors';

export type AuthMode = 'development' | 'entra';

export interface DevelopmentAuthConfig {
  mode: 'development';
}

export interface EntraAuthConfig {
  mode: 'entra';
  clientId: string;
  tenantId?: string;
  authority: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
  apiScope: string;
}

export type AuthConfig = DevelopmentAuthConfig | EntraAuthConfig;

const readEnv = (key: keyof ImportMetaEnv): string | undefined => {
  const value = import.meta.env[key];
  return typeof value === 'string' && value.trim() !== '' ? value.trim() : undefined;
};

/**
 * Indication non bloquante utilisée pour afficher le bon écran de connexion avant
 * validation complète. Une valeur inconnue n'active jamais Entra silencieusement.
 */
export const getAuthModeHint = (): AuthMode => (readEnv('VITE_AUTH_MODE') === 'entra' ? 'entra' : 'development');

const requireValue = (key: keyof ImportMetaEnv, label: string): string => {
  const value = readEnv(key);
  if (!value) throw new AuthConfigurationError(`Configuration Microsoft Entra ID incomplète : ${label} est requis.`);
  return value;
};

const browserOrigin = (): string => globalThis.location?.origin || 'http://localhost:3000';

export const getAuthConfig = (): AuthConfig => {
  const rawMode = readEnv('VITE_AUTH_MODE') ?? 'development';
  if (rawMode !== 'development' && rawMode !== 'entra') {
    throw new AuthConfigurationError(
      `VITE_AUTH_MODE="${rawMode}" est invalide. Valeurs autorisées : development ou entra.`
    );
  }

  if (rawMode === 'development') return { mode: 'development' };

  const clientId = requireValue('VITE_ENTRA_CLIENT_ID', 'VITE_ENTRA_CLIENT_ID');
  const apiScope = requireValue('VITE_ENTRA_API_SCOPE', 'VITE_ENTRA_API_SCOPE');
  const tenantId = readEnv('VITE_ENTRA_TENANT_ID');
  const explicitAuthority = readEnv('VITE_ENTRA_AUTHORITY');
  if (!explicitAuthority && !tenantId) {
    throw new AuthConfigurationError(
      'Configuration Microsoft Entra ID incomplète : VITE_ENTRA_AUTHORITY ou VITE_ENTRA_TENANT_ID est requis.'
    );
  }

  const authority = explicitAuthority ?? `https://login.microsoftonline.com/${tenantId}/v2.0`;
  const redirectUri = readEnv('VITE_ENTRA_REDIRECT_URI') ?? `${browserOrigin()}/`;
  const postLogoutRedirectUri = readEnv('VITE_ENTRA_POST_LOGOUT_REDIRECT_URI') ?? `${browserOrigin()}/login`;

  return {
    mode: 'entra',
    clientId,
    tenantId,
    authority,
    redirectUri,
    postLogoutRedirectUri,
    apiScope
  };
};
