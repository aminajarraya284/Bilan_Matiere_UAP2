import { afterEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ acquireAccessToken: vi.fn() }));
vi.mock('./msal.service', () => ({
  getMsalService: () => ({ acquireAccessToken: mocks.acquireAccessToken })
}));

import { acquireApiAccessToken } from './token.service';

describe('API access token service', () => {
  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    vi.unstubAllEnvs();
  });

  it('retourne le token local uniquement en Development', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    localStorage.setItem('sopal_auth_token', 'development-token');
    await expect(acquireApiAccessToken()).resolves.toBe('development-token');
    expect(mocks.acquireAccessToken).not.toHaveBeenCalled();
  });

  it('ne retourne pas de faux token si la session Development est absente', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    await expect(acquireApiAccessToken()).resolves.toBeNull();
  });

  it('délègue l’acquisition à MSAL en mode Entra', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'entra');
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'client');
    vi.stubEnv('VITE_ENTRA_TENANT_ID', 'tenant');
    vi.stubEnv('VITE_ENTRA_API_SCOPE', 'api://backend/access_as_user');
    mocks.acquireAccessToken.mockResolvedValue('entra-token');
    await expect(acquireApiAccessToken()).resolves.toBe('entra-token');
  });
});
