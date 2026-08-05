import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import type { RouteLocationNormalized } from 'vue-router';
import { useAuthStore } from '@/features/auth/auth.store';
import { rememberAuthReturnPath } from '@/features/auth/auth.navigation';
import { authGuard } from './index';

const from = {} as RouteLocationNormalized;
const next = vi.fn();
const route = (overrides: Partial<RouteLocationNormalized>): RouteLocationNormalized =>
  ({
    name: 'Dashboard',
    path: '/dashboard',
    fullPath: '/dashboard',
    query: {},
    hash: '',
    params: {},
    matched: [],
    redirectedFrom: undefined,
    meta: {},
    ...overrides
  }) as RouteLocationNormalized;

const setUser = (roles: Array<'Admin' | 'ResponsableFonderie' | 'ResponsableUsinage2'>, token = 'dev-token') => {
  const store = useAuthStore();
  store.user = {
    id: '1',
    displayName: 'Utilisateur',
    fullName: 'Utilisateur',
    username: 'user',
    email: 'user@sopal.test',
    roles,
    role: roles[0],
    authenticationMode: 'development',
    status: 'Actif'
  };
  store.token = token;
  store.initialized = true;
  return store;
};

const runGuard = (target: RouteLocationNormalized) => Promise.resolve(authGuard(target, from, next));

describe('authentication router guard', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    localStorage.clear();
    sessionStorage.clear();
    setActivePinia(createPinia());
  });

  afterEach(() => vi.unstubAllEnvs());

  it('autorise une route publique sans utilisateur', async () => {
    useAuthStore().initialized = true;
    await expect(runGuard(route({ name: 'Login', path: '/login', fullPath: '/login' }))).resolves.toBe(true);
  });

  it('mémorise et redirige une route protégée sans utilisateur', async () => {
    useAuthStore().initialized = true;
    await expect(
      runGuard(route({ meta: { requiresAuth: true, roles: ['Admin'] } }))
    ).resolves.toEqual({ name: 'Login', query: { redirect: '/dashboard' } });
    expect(sessionStorage.getItem('sopal_auth_return_path')).toBe('/dashboard');
  });

  it('autorise une route protégée avec un rôle accepté', async () => {
    setUser(['Admin']);
    await expect(runGuard(route({ meta: { requiresAuth: true, roles: ['Admin'] } }))).resolves.toBe(true);
  });

  it('redirige vers forbidden avec un rôle insuffisant', async () => {
    setUser(['ResponsableFonderie']);
    await expect(runGuard(route({ meta: { requiresAuth: true, roles: ['Admin'] } }))).resolves.toEqual({
      name: 'Forbidden',
      query: { from: '/dashboard' }
    });
  });

  it('restaure la route initiale depuis Login après authentification', async () => {
    setUser(['Admin']);
    rememberAuthReturnPath('/rapports?periode=2026-08');
    await expect(runGuard(route({ name: 'Login', path: '/login', fullPath: '/login' }))).resolves.toBe(
      '/rapports?periode=2026-08'
    );
  });

  it('redirige un utilisateur connecté sans rôle vers forbidden', async () => {
    setUser([]);
    await expect(runGuard(route({ name: 'Login', path: '/login', fullPath: '/login' }))).resolves.toBe('/forbidden');
  });

  it('redirige une erreur de configuration vers Login sans boucle', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'inconnu');
    const store = useAuthStore();
    await expect(runGuard(route({ meta: { requiresAuth: true, roles: ['Admin'] } }))).resolves.toEqual({
      name: 'Login',
      query: { error: 'configuration', redirect: '/dashboard' }
    });
    expect(store.error).toMatch(/VITE_AUTH_MODE/);
  });
});
