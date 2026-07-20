import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore } from './auth.store';

describe('auth store persistence', () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
  });

  it('rejects a malformed stored user and clears stale credentials', () => {
    localStorage.setItem('sopal_auth_user', JSON.stringify({ role: 'Admin' }));
    localStorage.setItem('sopal_auth_token', 'stale-token');

    const store = useAuthStore();

    expect(store.user).toBeNull();
    expect(store.token).toBeNull();
    expect(localStorage.getItem('sopal_auth_user')).toBeNull();
    expect(localStorage.getItem('sopal_auth_token')).toBeNull();
  });

  it('restores a complete active user and token', () => {
    localStorage.setItem(
      'sopal_auth_user',
      JSON.stringify({
        id: 1,
        fullName: 'Administrateur SOPAL',
        username: 'admin',
        email: 'admin@sopal.com',
        role: 'Admin',
        status: 'Actif'
      })
    );
    localStorage.setItem('sopal_auth_token', 'valid-token');

    const store = useAuthStore();

    expect(store.userRole).toBe('Admin');
    expect(store.token).toBe('valid-token');
    expect(store.isAuthenticated).toBe(true);
  });
});
