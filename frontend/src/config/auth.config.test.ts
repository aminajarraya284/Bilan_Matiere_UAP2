import { afterEach, describe, expect, it, vi } from 'vitest';
import { getAuthConfig } from './auth.config';
import { AuthConfigurationError } from '@/features/auth/auth.errors';

const setValidEntraEnv = () => {
  vi.stubEnv('VITE_AUTH_MODE', 'entra');
  vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'frontend-client-id');
  vi.stubEnv('VITE_ENTRA_TENANT_ID', 'tenant-id');
  vi.stubEnv('VITE_ENTRA_AUTHORITY', '');
  vi.stubEnv('VITE_ENTRA_API_SCOPE', 'api://backend/access_as_user');
  vi.stubEnv('VITE_ENTRA_REDIRECT_URI', 'https://app.example.test/');
  vi.stubEnv('VITE_ENTRA_POST_LOGOUT_REDIRECT_URI', 'https://app.example.test/login');
};

describe('auth config', () => {
  afterEach(() => vi.unstubAllEnvs());

  it('accepte development sans aucune variable Entra', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', '');
    vi.stubEnv('VITE_ENTRA_API_SCOPE', '');
    expect(getAuthConfig()).toEqual({ mode: 'development' });
  });

  it('construit une configuration Entra valide et dérive l’autorité du tenant', () => {
    setValidEntraEnv();
    expect(getAuthConfig()).toMatchObject({
      mode: 'entra',
      clientId: 'frontend-client-id',
      authority: 'https://login.microsoftonline.com/tenant-id/v2.0',
      apiScope: 'api://backend/access_as_user'
    });
  });

  it('préfère l’autorité explicite', () => {
    setValidEntraEnv();
    vi.stubEnv('VITE_ENTRA_AUTHORITY', 'https://login.microsoftonline.com/custom/v2.0');
    expect(getAuthConfig()).toMatchObject({ authority: 'https://login.microsoftonline.com/custom/v2.0' });
  });

  it('refuse un mode inconnu', () => {
    vi.stubEnv('VITE_AUTH_MODE', 'production');
    expect(() => getAuthConfig()).toThrow(AuthConfigurationError);
  });

  it('refuse Entra sans Client ID', () => {
    setValidEntraEnv();
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', '');
    expect(() => getAuthConfig()).toThrow(/VITE_ENTRA_CLIENT_ID/);
  });

  it('refuse Entra sans scope API', () => {
    setValidEntraEnv();
    vi.stubEnv('VITE_ENTRA_API_SCOPE', '');
    expect(() => getAuthConfig()).toThrow(/VITE_ENTRA_API_SCOPE/);
  });

  it('refuse Entra sans tenant ni autorité', () => {
    setValidEntraEnv();
    vi.stubEnv('VITE_ENTRA_TENANT_ID', '');
    expect(() => getAuthConfig()).toThrow(/AUTHORITY.*TENANT_ID|TENANT_ID.*AUTHORITY/);
  });
});
