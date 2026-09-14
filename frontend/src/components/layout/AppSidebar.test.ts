import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useAuthStore } from '@/features/auth/auth.store';
import AppSidebar from './AppSidebar.vue';
import type { UserRole } from '@/types/common.types';

// Sans routeur installe, `router-link` n'est pas resolu : un stub par nom n'est jamais
// applique et le libelle du lien n'est pas rendu. On enregistre donc un vrai composant.
const routerLinkStub = { props: ['to'], template: '<a><slot /></a>' };

describe('AppSidebar', () => {
  it.each<[UserRole, boolean, boolean]>([
    ['Admin', true, true],
    ['ResponsableFonderie', true, false],
    ['ResponsableUsinage2', false, true]
  ])('affiche uniquement les accès autorisés pour %s', (role, inventaire, receptions) => {
    const pinia = createPinia();
    setActivePinia(pinia);
    useAuthStore().user = {
      id: '1', displayName: 'Test', fullName: 'Test', username: 'test', email: 'test@sopal.test',
      roles: [role], role, authenticationMode: 'development', status: 'Actif'
    };
    const wrapper = mount(AppSidebar, { global: {
      plugins: [pinia], mocks: { $route: { path: '/dashboard' } },
      components: { RouterLink: routerLinkStub }
    } });
    expect(wrapper.text().includes('Inventaire Fonderie')).toBe(inventaire);
    expect(wrapper.text().includes('Réceptions Usinage 2')).toBe(receptions);
    wrapper.unmount();
  });
  afterEach(() => {
    vi.unstubAllEnvs();
    localStorage.clear();
  });

  it('affiche le mode global hybride', () => {
    vi.stubEnv('VITE_AUTH_SOURCE', 'api');
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');
    vi.stubEnv('VITE_SYNCHRONISATION_X3_SOURCE', 'disabled');

    const pinia = createPinia();
    setActivePinia(pinia);
    useAuthStore().user = {
      id: '1',
      displayName: 'Administrateur SOPAL',
      fullName: 'Administrateur SOPAL',
      username: 'admin',
      email: 'admin@sopal.com',
      roles: ['Admin'],
      role: 'Admin',
      authenticationMode: 'development',
      status: 'Actif'
    };

    const wrapper = mount(AppSidebar, {
      global: {
        plugins: [pinia],
        mocks: { $route: { path: '/dashboard' } },
        components: { RouterLink: routerLinkStub }
      }
    });

    expect(wrapper.text()).toContain('Mode hybride');
    expect(wrapper.text()).not.toContain('Synchronisation');
    expect(wrapper.text()).not.toContain('Postes processus');
  });
});
