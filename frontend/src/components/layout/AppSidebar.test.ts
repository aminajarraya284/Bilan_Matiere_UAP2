import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useAuthStore } from '@/features/auth/auth.store';
import AppSidebar from './AppSidebar.vue';

describe('AppSidebar', () => {
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
      id: 1,
      fullName: 'Administrateur SOPAL',
      username: 'admin',
      email: 'admin@sopal.com',
      role: 'Admin',
      status: 'Actif'
    };

    const wrapper = mount(AppSidebar, {
      global: {
        plugins: [pinia],
        mocks: { $route: { path: '/dashboard' } },
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>'
          }
        }
      }
    });

    expect(wrapper.text()).toContain('Mode hybride');
    expect(wrapper.text()).not.toContain('Synchronisation');
    expect(wrapper.text()).not.toContain('Postes processus');
  });
});
