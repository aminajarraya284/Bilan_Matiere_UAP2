import { mount } from '@vue/test-utils';
import { afterEach, describe, expect, it, vi } from 'vitest';
import DataSourceBadge from './DataSourceBadge.vue';

describe('DataSourceBadge', () => {
  afterEach(() => vi.unstubAllEnvs());

  it('affiche le mode démonstration pour une page mock', () => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');
    expect(mount(DataSourceBadge, { props: { vertical: 'dashboard' } }).text()).toContain('Mode démonstration');
  });

  it('affiche une fonctionnalité indisponible pour une source disabled', () => {
    vi.stubEnv('VITE_SYNCHRONISATION_X3_SOURCE', 'disabled');
    expect(mount(DataSourceBadge, { props: { vertical: 'synchronisationX3' } }).text()).toContain(
      'Fonctionnalité non disponible'
    );
  });

  it("n'affiche pas de badge local pour une source API", () => {
    vi.stubEnv('VITE_AUTH_SOURCE', 'api');
    expect(mount(DataSourceBadge, { props: { vertical: 'auth' } }).text()).toBe('');
  });
});
