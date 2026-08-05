import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  route: { query: {} as Record<string, string> },
  msalLogin: vi.fn(),
  msalInitialize: vi.fn(),
  msalUser: vi.fn()
}));

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mocks.push }),
  useRoute: () => mocks.route
}));
vi.mock('@/services/api', () => ({ default: { post: vi.fn() }, api: { post: vi.fn() } }));
vi.mock('./msal.service', () => ({
  getMsalService: () => ({
    initialize: mocks.msalInitialize,
    login: mocks.msalLogin,
    logout: vi.fn(),
    acquireAccessToken: vi.fn(),
    getAuthenticatedUser: mocks.msalUser
  })
}));

import LoginView from './LoginView.vue';
import { useAuthStore } from './auth.store';

const mountView = () => mount(LoginView, { global: { plugins: [createPinia()] } });

describe('LoginView auth modes', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mocks.route.query = {};
    mocks.msalInitialize.mockResolvedValue(undefined);
    mocks.msalLogin.mockResolvedValue(undefined);
    mocks.msalUser.mockReturnValue(null);
  });

  afterEach(() => vi.unstubAllEnvs());

  it('affiche le formulaire local en Development', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    const wrapper = mountView();
    await Promise.resolve();
    expect(wrapper.find('input[type="text"]').exists()).toBe(true);
    expect(wrapper.find('input[type="password"]').exists()).toBe(true);
    expect(wrapper.text()).not.toContain('Se connecter avec Microsoft');
  });

  it('affiche uniquement le bouton Microsoft en mode Entra', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'entra');
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'client');
    vi.stubEnv('VITE_ENTRA_TENANT_ID', 'tenant');
    vi.stubEnv('VITE_ENTRA_API_SCOPE', 'api://backend/access_as_user');
    const wrapper = mountView();
    await Promise.resolve();
    expect(wrapper.text()).toContain('Se connecter avec Microsoft');
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
  });

  it('appelle le store et non MSAL directement depuis le composant', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'entra');
    vi.stubEnv('VITE_ENTRA_CLIENT_ID', 'client');
    vi.stubEnv('VITE_ENTRA_TENANT_ID', 'tenant');
    vi.stubEnv('VITE_ENTRA_API_SCOPE', 'api://backend/access_as_user');
    const wrapper = mountView();
    await flushPromises();
    await wrapper.get('button').trigger('click');
    await flushPromises();
    expect(mocks.msalLogin).toHaveBeenCalledTimes(1);
  });

  it('affiche les états loading et erreur du store', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'development');
    const pinia = createPinia();
    setActivePinia(pinia);
    const store = useAuthStore();
    store.initialized = true;
    store.loading = true;
    store.error = 'Configuration Microsoft Entra ID incomplète.';
    const wrapper = mount(LoginView, { global: { plugins: [pinia] } });
    expect(wrapper.get('[role="alert"]').text()).toContain('Configuration Microsoft Entra ID incomplète');
    expect(wrapper.get('button[type="submit"]').attributes('disabled')).toBeDefined();
  });

  it('affiche l’erreur de configuration sans fallback vers le formulaire local', async () => {
    vi.stubEnv('VITE_AUTH_MODE', 'mode-inconnu');
    const wrapper = mountView();
    await flushPromises();
    expect(wrapper.get('[role="alert"]').text()).toContain('VITE_AUTH_MODE');
    expect(wrapper.find('input[type="password"]').exists()).toBe(false);
    expect(wrapper.find('button').exists()).toBe(false);
  });
});
