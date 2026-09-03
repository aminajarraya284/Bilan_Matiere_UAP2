import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
  get: vi.fn()
}));

vi.mock('@/services/api', () => ({ default: mocks, api: mocks }));

import { authService, INVALID_CREDENTIALS_MESSAGE, MOCK_USERS } from './auth.service';
import { useAuthStore } from './auth.store';
import { AUTH_TOKEN_STORAGE_KEY, AUTH_USER_STORAGE_KEY, expireAuthSession } from './auth.session';
import { hasPermission } from '@/utils/permissions';

const ADMIN_USER = {
  id: 1,
  fullName: 'Administrateur SOPAL',
  username: 'admin',
  email: 'admin@sopal.com',
  role: 'Admin' as const,
  status: 'Actif' as const
};

const LOGIN_OK = {
  data: {
    token: 'dev-token-abc',
    expiresAtUtc: '2026-07-27T18:52:27.441Z',
    user: ADMIN_USER
  }
};

// Erreur axios telle que reçue apres une reponse ProblemDetails du backend.
const httpError = (status: number, data: unknown = {}) => ({ response: { status, data } });

describe('authentification en mode API reelle', () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.stubEnv('VITE_AUTH_SOURCE', 'api');
  });

  afterEach(() => vi.unstubAllEnvs());

  // 1. Le formulaire valide appelle bien la route backend retenue.
  it('appelle POST /auth/login avec les identifiants saisis', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);

    await authService.login('admin', 'admin');

    expect(mocks.post).toHaveBeenCalledTimes(1);
    expect(mocks.post).toHaveBeenCalledWith('/auth/login', { username: 'admin', password: 'admin' });
  });

  // 2. Un succes stocke la session (token + utilisateur).
  it('stocke le token et l utilisateur apres un succes', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    const store = useAuthStore();

    await store.login('admin', 'admin');

    expect(store.isAuthenticated).toBe(true);
    expect(store.token).toBe('dev-token-abc');
    expect(localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe('dev-token-abc');
    expect(JSON.parse(localStorage.getItem(AUTH_USER_STORAGE_KEY)!)).toMatchObject({ username: 'admin' });
  });

  // 3. Le role renvoye determine la redirection (Admin -> /dashboard).
  //    On verifie la donnee qui pilote la redirection de LoginView / du router guard.
  it('renvoie un role Admin permettant d acceder au dashboard', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    const store = useAuthStore();

    const user = await store.login('admin', 'admin');

    expect(user).not.toBeNull();
    expect(user?.role).toBe('Admin');
    expect(hasPermission('Admin', 'Dashboard')).toBe(true);
  });

  // 4. Un 401 affiche un message clair et non le message generique serveur.
  it('affiche « Identifiants incorrects » sur une erreur 401', async () => {
    mocks.post.mockRejectedValueOnce(
      httpError(401, { title: 'Identifiants incorrects', detail: 'Identifiant ou mot de passe incorrect.' })
    );

    await expect(authService.login('admin', 'mauvais')).rejects.toThrow(INVALID_CREDENTIALS_MESSAGE);
  });

  // 4bis. Un 403 est traite separement d'un 401.
  it('distingue un 403 d un 401', async () => {
    mocks.post.mockRejectedValueOnce(httpError(403));

    await expect(authService.login('admin', 'admin')).rejects.toThrow(/permissions requises/i);
  });

  // 5. Une erreur reseau ne doit JAMAIS rebasculer sur les comptes mock.
  it('n active aucun repli mock sur erreur reseau', async () => {
    mocks.post.mockRejectedValueOnce(new Error('Network Error'));
    const store = useAuthStore();

    // « admin »/« admin » est un couple valide cote mock : s'il passait, c'est qu'un repli existe.
    await expect(store.login('admin', 'admin')).rejects.toThrow();

    expect(store.isAuthenticated).toBe(false);
    expect(store.token).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(MOCK_USERS.some((u) => u.username === 'admin')).toBe(true);
  });

  // 6. La deconnexion efface la session persistee.
  it('efface la session a la deconnexion', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    const store = useAuthStore();
    await store.login('admin', 'admin');

    store.logout();

    expect(store.isAuthenticated).toBe(false);
    expect(localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(localStorage.getItem(AUTH_USER_STORAGE_KEY)).toBeNull();
  });

  // 7. Un token expire (401 sur un appel protege) invalide la session.
  //    expireAuthSession est ce que declenche l'intercepteur axios sur 401.
  it('invalide la session lorsqu un token expire est rejete', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    const store = useAuthStore();
    await store.login('admin', 'admin');
    expect(store.isAuthenticated).toBe(true);

    expireAuthSession();

    expect(store.isAuthenticated).toBe(false);
    expect(store.token).toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
  });

  // 8. Une nouvelle session (localStorage vide) n'est pas authentifiee -> ecran de login.
  it('demarre non authentifie quand localStorage est vide', () => {
    const store = useAuthStore();

    expect(localStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(store.isAuthenticated).toBe(false);
    expect(store.user).toBeNull();
  });

  // 9. Apres connexion, une page protegee est accessible pour le role obtenu.
  it('autorise une page protegee apres connexion', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    const store = useAuthStore();

    await store.login('admin', 'admin');

    expect(store.isAuthenticated).toBe(true);
    expect(hasPermission(store.userRole, 'ReceptionsUsinage2')).toBe(true);
    expect(hasPermission(store.userRole, 'EtatsX3')).toBe(true);
  });

  // 10. Un rechargement de page (nouvelle instance de store) conserve une session valide.
  it('conserve la session apres un rechargement de page', async () => {
    mocks.post.mockResolvedValueOnce(LOGIN_OK);
    await useAuthStore().login('admin', 'admin');

    // Simule un rechargement : nouvelle instance Pinia, localStorage inchange.
    setActivePinia(createPinia());
    const rehydrated = useAuthStore();

    expect(rehydrated.isAuthenticated).toBe(true);
    expect(rehydrated.token).toBe('dev-token-abc');
    expect(rehydrated.userRole).toBe('Admin');
  });
});
