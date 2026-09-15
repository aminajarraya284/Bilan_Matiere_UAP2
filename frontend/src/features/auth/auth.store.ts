import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { getAuthConfig, getAuthModeHint, type AuthMode } from '@/config/auth.config';
import type { AuthUser } from '@/types/api.types';
import { authService } from './auth.service';
import { AuthConfigurationError, MissingApplicationRoleError, toAuthUserMessage } from './auth.errors';
import { getMsalService } from './msal.service';
import type { ApplicationRole, AuthenticatedUser } from './auth.types';
import { APPLICATION_ROLES, getPrimaryRole } from './auth.roles';
import {
  AUTH_TOKEN_STORAGE_KEY,
  AUTH_USER_STORAGE_KEY,
  clearPersistedAuthSession,
  registerAuthSessionInvalidator
} from './auth.session';

const isRole = (value: unknown): value is ApplicationRole =>
  typeof value === 'string' && (APPLICATION_ROLES as readonly string[]).includes(value);

const normalizeDevelopmentUser = (user: AuthUser): AuthenticatedUser => ({
  id: String(user.id),
  displayName: user.fullName,
  email: user.email,
  roles: [user.role],
  authenticationMode: 'development',
  fullName: user.fullName,
  username: user.username,
  role: user.role,
  status: 'Actif'
});

const parseStoredDevelopmentUser = (value: unknown): AuthenticatedUser | null => {
  if (!value || typeof value !== 'object') return null;
  const candidate = value as Partial<AuthenticatedUser> & Partial<AuthUser>;

  if (
    typeof candidate.id === 'string' &&
    typeof candidate.displayName === 'string' &&
    typeof candidate.username === 'string' &&
    Array.isArray(candidate.roles) &&
    candidate.roles.length > 0 &&
    candidate.roles.every(isRole) &&
    candidate.authenticationMode === 'development'
  ) {
    // Construction explicite plutôt qu'un spread suivi d'un cast : les gardes
    // ci-dessus ont déjà vérifié chaque champ requis, autant les nommer.
    return {
      id: candidate.id,
      displayName: candidate.displayName,
      email: candidate.email,
      roles: candidate.roles,
      authenticationMode: 'development',
      fullName: candidate.displayName,
      username: candidate.username,
      role: getPrimaryRole(candidate.roles),
      status: 'Actif'
    };
  }

  // Migration transparente des sessions Development créées avant le Lot 2.
  if (
    typeof candidate.id === 'number' &&
    typeof candidate.fullName === 'string' &&
    typeof candidate.username === 'string' &&
    typeof candidate.email === 'string' &&
    isRole(candidate.role) &&
    candidate.status === 'Actif'
  ) {
    return normalizeDevelopmentUser(candidate as AuthUser);
  }

  return null;
};

const readStoredDevelopmentUser = (): AuthenticatedUser | null => {
  const storedUser = localStorage.getItem(AUTH_USER_STORAGE_KEY);
  if (!storedUser) return null;
  try {
    const user = parseStoredDevelopmentUser(JSON.parse(storedUser) as unknown);
    if (!user) throw new Error('Invalid stored user');
    return user;
  } catch {
    clearPersistedAuthSession();
    return null;
  }
};

export const useAuthStore = defineStore('auth', () => {
  const mode = ref<AuthMode>(getAuthModeHint());
  const initialUser = mode.value === 'development' ? readStoredDevelopmentUser() : null;
  const token = ref<string | null>(
    initialUser && mode.value === 'development' ? localStorage.getItem(AUTH_TOKEN_STORAGE_KEY) : null
  );
  const user = ref<AuthenticatedUser | null>(initialUser);
  const initialized = ref(false);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const configurationError = ref(false);
  let initializationPromise: Promise<void> | null = null;

  const isAuthenticated = computed(() => {
    if (!user.value) return false;
    return mode.value === 'entra' || Boolean(token.value);
  });
  const roles = computed<ApplicationRole[]>(() => user.value?.roles ?? []);
  const userRole = computed<ApplicationRole | undefined>(() => getPrimaryRole(roles.value));

  async function initialize(): Promise<void> {
    if (initialized.value) return;
    if (initializationPromise) return initializationPromise;

    initializationPromise = (async () => {
      loading.value = true;
      error.value = null;
      configurationError.value = false;
      try {
        const config = getAuthConfig();
        mode.value = config.mode;

        if (config.mode === 'development') {
          user.value = readStoredDevelopmentUser();
          token.value = user.value ? localStorage.getItem(AUTH_TOKEN_STORAGE_KEY) : null;
        } else {
          clearPersistedAuthSession();
          token.value = null;
          const service = getMsalService();
          await service.initialize();
          user.value = service.getAuthenticatedUser();
          if (user.value && user.value.roles.length === 0) {
            error.value = new MissingApplicationRoleError().message;
          }
        }
      } catch (cause: unknown) {
        user.value = null;
        token.value = null;
        configurationError.value = cause instanceof AuthConfigurationError;
        error.value = toAuthUserMessage(cause);
        throw cause;
      } finally {
        initialized.value = true;
        loading.value = false;
        initializationPromise = null;
      }
    })();

    return initializationPromise;
  }

  async function login(username = '', password = ''): Promise<AuthenticatedUser | null> {
    if (!initialized.value) await initialize();
    if (configurationError.value) throw new AuthConfigurationError(error.value ?? undefined);
    loading.value = true;
    error.value = null;
    try {
      if (mode.value === 'entra') {
        await getMsalService().login();
        return null;
      }

      const result = await authService.login(username, password);
      const normalizedUser = normalizeDevelopmentUser(result.user);
      token.value = result.token;
      user.value = normalizedUser;
      localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, result.token);
      localStorage.setItem(AUTH_USER_STORAGE_KEY, JSON.stringify(normalizedUser));
      return normalizedUser;
    } catch (cause: unknown) {
      error.value = cause instanceof Error ? cause.message : toAuthUserMessage(cause);
      throw cause;
    } finally {
      loading.value = false;
    }
  }

  async function refreshUser(): Promise<AuthenticatedUser | null> {
    if (mode.value === 'development') return user.value;
    await getMsalService().initialize();
    user.value = getMsalService().getAuthenticatedUser();
    error.value = user.value && user.value.roles.length === 0 ? new MissingApplicationRoleError().message : null;
    return user.value;
  }

  async function getAccessToken(): Promise<string> {
    if (mode.value === 'development') {
      if (!token.value) throw new Error('Session Development absente.');
      return token.value;
    }
    return getMsalService().acquireAccessToken();
  }

  async function logout(): Promise<void> {
    const activeMode = mode.value;
    token.value = null;
    user.value = null;
    error.value = null;
    clearPersistedAuthSession();
    if (activeMode === 'entra') await getMsalService().logout();
  }

  const hasRole = (role: ApplicationRole): boolean => roles.value.includes(role);
  const hasAnyRole = (allowedRoles: readonly ApplicationRole[]): boolean =>
    allowedRoles.some((role) => roles.value.includes(role));

  registerAuthSessionInvalidator(() => {
    token.value = null;
    user.value = null;
  });

  return {
    mode,
    initialized,
    loading,
    error,
    configurationError,
    token,
    user,
    roles,
    isAuthenticated,
    userRole,
    initialize,
    login,
    logout,
    refreshUser,
    hasRole,
    hasAnyRole,
    getAccessToken
  };
});
