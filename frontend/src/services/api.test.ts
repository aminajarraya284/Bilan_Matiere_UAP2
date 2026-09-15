import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const mocks = vi.hoisted(() => ({
  currentRoute: { value: { name: 'Dashboard', fullPath: '/dashboard' } },
  replace: vi.fn(),
  acquireToken: vi.fn()
}));

vi.mock('@/router', () => ({
  default: {
    currentRoute: mocks.currentRoute,
    replace: mocks.replace
  }
}));
vi.mock('@/features/auth/token.service', () => ({
  acquireApiAccessToken: mocks.acquireToken
}));

import { api } from './api';
import { useAuthStore } from '@/features/auth/auth.store';

const activeAdmin = {
  id: 1,
  fullName: 'Administrateur SOPAL',
  username: 'admin',
  email: 'admin@sopal.com',
  role: 'Admin' as const,
  status: 'Actif' as const
};

describe('API authentication interceptor', () => {
  beforeEach(() => {
    localStorage.clear();
    mocks.currentRoute.value.name = 'Dashboard';
    mocks.currentRoute.value.fullPath = '/dashboard';
    mocks.replace.mockClear();
    mocks.acquireToken.mockImplementation(async () => localStorage.getItem('sopal_auth_token'));
    mocks.replace.mockImplementation(async () => {
      mocks.currentRoute.value.name = 'Login';
    });
    setActivePinia(createPinia());
  });

  it('clears the reactive session before redirecting after a 401 response', async () => {
    localStorage.setItem('sopal_auth_token', 'expired-token');
    localStorage.setItem('sopal_auth_user', JSON.stringify(activeAdmin));
    const authStore = useAuthStore();

    await expect(
      api.get('/protected-resource', {
        adapter: async (config) =>
          Promise.reject({
            config,
            response: { status: 401 },
            isAxiosError: true
          })
      })
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(authStore.isAuthenticated).toBe(false);
    expect(authStore.token).toBeNull();
    expect(authStore.user).toBeNull();
    expect(localStorage.getItem('sopal_auth_token')).toBeNull();
    expect(localStorage.getItem('sopal_auth_user')).toBeNull();
    expect(mocks.replace).toHaveBeenCalledWith({
      name: 'Login',
      query: { error: 'session_expired', redirect: '/dashboard' }
    });
  });

  it('keeps the session intact on a 403 response (insufficient role, not expired)', async () => {
    localStorage.setItem('sopal_auth_token', 'valid-token');
    localStorage.setItem('sopal_auth_user', JSON.stringify(activeAdmin));
    const authStore = useAuthStore();

    await expect(
      api.get('/forbidden-resource', {
        adapter: async (config) =>
          Promise.reject({ config, response: { status: 403 }, isAxiosError: true })
      })
    ).rejects.toMatchObject({ response: { status: 403 } });

    // Un 403 signifie « role insuffisant », pas « session invalide » : on ne deconnecte pas.
    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.token).toBe('valid-token');
    expect(localStorage.getItem('sopal_auth_token')).toBe('valid-token');
    expect(mocks.replace).toHaveBeenCalledWith({
      name: 'Forbidden',
      query: { from: '/dashboard' }
    });
  });

  it('does not expire the session when the login request itself returns 401', async () => {
    const authStore = useAuthStore();

    await expect(
      api.post('/auth/login', { username: 'admin', password: 'faux' }, {
        adapter: async (config) =>
          Promise.reject({ config, response: { status: 401 }, isAxiosError: true })
      })
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(mocks.replace).not.toHaveBeenCalled();
    expect(authStore.isAuthenticated).toBe(false);
  });

  it('redirects only once when several requests receive 401 concurrently', async () => {
    localStorage.setItem('sopal_auth_token', 'expired-token');
    localStorage.setItem('sopal_auth_user', JSON.stringify(activeAdmin));
    useAuthStore();
    const rejectWith401 = async (config: unknown) =>
      Promise.reject({ config, response: { status: 401 }, isAxiosError: true });

    await Promise.allSettled([
      api.get('/first-resource', { adapter: rejectWith401 }),
      api.get('/second-resource', { adapter: rejectWith401 })
    ]);

    expect(mocks.replace).toHaveBeenCalledTimes(1);
  });

  it('ajoute le Bearer fourni par le service de token central', async () => {
    mocks.acquireToken.mockResolvedValue('entra-access-token');
    let authorization: unknown;

    await api.get('/protected-resource', {
      adapter: async (config) => {
        authorization = config.headers?.Authorization;
        return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
      }
    });

    expect(authorization).toBe('Bearer entra-access-token');
  });

  it('n’ajoute aucun Bearer si aucun token n’est disponible', async () => {
    mocks.acquireToken.mockResolvedValue(null);
    let authorization: unknown;

    await api.get('/public-resource', {
      adapter: async (config) => {
        authorization = config.headers?.Authorization;
        return { data: {}, status: 200, statusText: 'OK', headers: {}, config };
      }
    });

    expect(authorization).toBeUndefined();
  });

  it('ne journalise jamais le contenu du token lors d’un 403', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    mocks.acquireToken.mockResolvedValue('token-secret-a-ne-jamais-logger');

    await expect(
      api.get('/forbidden-resource', {
        adapter: async (config) =>
          Promise.reject({ config, response: { status: 403 }, isAxiosError: true })
      })
    ).rejects.toMatchObject({ response: { status: 403 } });

    expect(JSON.stringify(warn.mock.calls)).not.toContain('token-secret-a-ne-jamais-logger');
  });
});
