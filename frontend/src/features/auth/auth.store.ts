import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { AuthUser } from './auth.types';
import { UserRole } from '@/types/common.types';
import { authService } from './auth.service';
import {
  AUTH_TOKEN_STORAGE_KEY,
  AUTH_USER_STORAGE_KEY,
  clearPersistedAuthSession,
  registerAuthSessionInvalidator
} from './auth.session';

const VALID_ROLES: UserRole[] = ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'];

const isAuthUser = (value: unknown): value is AuthUser => {
  if (!value || typeof value !== 'object') return false;
  const user = value as Partial<AuthUser>;
  return (
    typeof user.id === 'number' &&
    typeof user.fullName === 'string' &&
    typeof user.username === 'string' &&
    typeof user.email === 'string' &&
    typeof user.role === 'string' &&
    VALID_ROLES.includes(user.role as UserRole) &&
    user.status === 'Actif'
  );
};

const readStoredUser = (): AuthUser | null => {
  const storedUser = localStorage.getItem(AUTH_USER_STORAGE_KEY);
  if (!storedUser) return null;

  try {
    const parsed = JSON.parse(storedUser) as unknown;
    if (!isAuthUser(parsed)) throw new Error('Invalid stored user');
    return parsed;
  } catch {
    clearPersistedAuthSession();
    return null;
  }
};

export const useAuthStore = defineStore('auth', () => {
  const storedUser = readStoredUser();
  const token = ref<string | null>(storedUser ? localStorage.getItem(AUTH_TOKEN_STORAGE_KEY) : null);
  const user = ref<AuthUser | null>(storedUser);

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const userRole = computed<UserRole | undefined>(() => user.value?.role);

  async function login(username: string, password: string) {
    const result = await authService.login(username, password);
    token.value = result.token;
    user.value = result.user;

    localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, result.token);
    localStorage.setItem(AUTH_USER_STORAGE_KEY, JSON.stringify(result.user));

    return result.user;
  }

  function logout() {
    token.value = null;
    user.value = null;
    clearPersistedAuthSession();
  }

  registerAuthSessionInvalidator(() => {
    token.value = null;
    user.value = null;
  });

  return {
    token,
    user,
    isAuthenticated,
    userRole,
    login,
    logout
  };
});
