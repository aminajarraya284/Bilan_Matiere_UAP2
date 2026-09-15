import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const entraUser = {
  id: 'entra-user-id',
  displayName: 'Amina SOPAL',
  email: 'amina@sopal.test',
  roles: ['Admin'] as const,
  authenticationMode: 'entra' as const,
  fullName: 'Amina SOPAL',
  username: 'amina@sopal.test',
  role: 'Admin' as const,
  status: 'Actif' as const
};

const mocks = vi.hoisted(() => ({
  initialize: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(),
  acquireAccessToken: vi.fn(),
  getAuthenticatedUser: vi.fn()
}));

vi.mock('./msal.service', () => ({
  getMsalService: () => mocks
}));
vi.mock('@/services/api', () => ({ default: { post: vi.fn() }, api: { post: vi.fn() } }));

import { useAuthStore } from './auth.store';

const setEntraEnv = () => {
  vi.stubEnv('VITE_AUTH_MODE', 'entra');
  vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'client');
  vi.stubEnv('VITE_ENTRA_TENANT_ID', 'tenant');
  vi.stubEnv('VITE_ENTRA_API_SCOPE', 'api://backend/access_as_user');
};

describe('auth store in Entra mode', () => {
  beforeEach(() => {
    setEntraEnv();
    localStorage.clear();
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mocks.initialize.mockResolvedValue(undefined);
    mocks.login.mockResolvedValue(undefined);
    mocks.logout.mockResolvedValue(undefined);
    mocks.acquireAccessToken.mockResolvedValue('access-token');
    mocks.getAuthenticatedUser.mockReturnValue(entraUser);
  });

  afterEach(() => vi.unstubAllEnvs());

  it('initialise MSAL et expose un utilisateur normalisé sans persister de token', async () => {
    localStorage.setItem('sopal_auth_token', 'ancien-token-dev');
    const store = useAuthStore();
    await store.initialize();

    expect(store.mode).toBe('entra');
    expect(store.user).toEqual(entraUser);
    expect(store.isAuthenticated).toBe(true);
    expect(store.roles).toEqual(['Admin']);
    expect(store.token).toBeNull();
    expect(localStorage.getItem('sopal_auth_token')).toBeNull();
  });

  it('délègue login, token et logout au service MSAL', async () => {
    const store = useAuthStore();
    await store.initialize();
    await store.login();
    await expect(store.getAccessToken()).resolves.toBe('access-token');
    await store.logout();

    expect(mocks.login).toHaveBeenCalledTimes(1);
    expect(mocks.acquireAccessToken).toHaveBeenCalledTimes(1);
    expect(mocks.logout).toHaveBeenCalledTimes(1);
    expect(store.user).toBeNull();
    expect(store.roles).toEqual([]);
  });

  it('évalue hasRole et hasAnyRole sur tous les rôles du compte', async () => {
    mocks.getAuthenticatedUser.mockReturnValue({
      ...entraUser,
      roles: ['ResponsableFonderie', 'ResponsableUsinage2'],
      role: 'ResponsableFonderie'
    });
    const store = useAuthStore();
    await store.initialize();
    expect(store.hasRole('ResponsableUsinage2')).toBe(true);
    expect(store.hasAnyRole(['Admin', 'ResponsableFonderie'])).toBe(true);
    expect(store.hasRole('Admin')).toBe(false);
  });

  it('garde authentifié un compte sans rôle mais expose l’erreur d’autorisation', async () => {
    mocks.getAuthenticatedUser.mockReturnValue({ ...entraUser, roles: [], role: undefined });
    const store = useAuthStore();
    await store.initialize();
    expect(store.isAuthenticated).toBe(true);
    expect(store.error).toMatch(/rôle autorisé/i);
    expect(store.hasAnyRole(['Admin'])).toBe(false);
  });

  it('expose une erreur sûre si l’initialisation MSAL échoue', async () => {
    mocks.initialize.mockRejectedValue(new Error('technical MSAL details'));
    const store = useAuthStore();
    await expect(store.initialize()).rejects.toThrow('technical MSAL details');
    expect(store.initialized).toBe(true);
    expect(store.isAuthenticated).toBe(false);
    expect(store.error).toBe("Une erreur d’authentification est survenue.");
  });
});
