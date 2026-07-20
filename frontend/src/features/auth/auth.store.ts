import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { AuthUser } from './auth.types';
import { UserRole } from '@/types/common.types';
import { authService } from './auth.service';

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
  const storedUser = localStorage.getItem('sopal_auth_user');
  if (!storedUser) return null;

  try {
    const parsed = JSON.parse(storedUser) as unknown;
    if (!isAuthUser(parsed)) throw new Error('Invalid stored user');
    return parsed;
  } catch {
    localStorage.removeItem('sopal_auth_token');
    localStorage.removeItem('sopal_auth_user');
    return null;
  }
};

export const useAuthStore = defineStore('auth', () => {
  const storedUser = readStoredUser();
  const token = ref<string | null>(storedUser ? localStorage.getItem('sopal_auth_token') : null);
  const user = ref<AuthUser | null>(storedUser);

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const userRole = computed<UserRole | undefined>(() => user.value?.role);

  async function login(username: string, password: string) {
    const result = await authService.login(username, password);
    token.value = result.token;
    user.value = result.user;

    localStorage.setItem('sopal_auth_token', result.token);
    localStorage.setItem('sopal_auth_user', JSON.stringify(result.user));

    return result.user;
  }

  function logout() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('sopal_auth_token');
    localStorage.removeItem('sopal_auth_user');
  }

  return {
    token,
    user,
    isAuthenticated,
    userRole,
    login,
    logout
  };
});
