import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  __resetDataSourceWarnings,
  getDataSourceMode,
  getGlobalMode,
  isApiSource,
  isDisabledSource,
  isMockSource
} from './dataSources';

const SOURCE_ENV_KEYS = [
  'VITE_AUTH_SOURCE',
  'VITE_ETATS_X3_SOURCE',
  'VITE_TYPES_MATIERE_SOURCE',
  'VITE_DECHETS_FONDERIE_SOURCE',
  'VITE_CONTROLE_POIDS_SOURCE',
  'VITE_RECEPTIONS_U2_SOURCE',
  'VITE_REFERENCES_FABRICATION_SOURCE',
  'VITE_BILAN_U2_SOURCE',
  'VITE_SORTIES_X3_SOURCE',
  'VITE_HISTORIQUE_IMPORTS_SOURCE',
  'VITE_DASHBOARD_SOURCE',
  'VITE_BILAN_FONDERIE_SOURCE',
  'VITE_BILAN_UAP2_SOURCE',
  'VITE_INVENTAIRE_SOURCE',
  'VITE_RAPPORTS_SOURCE',
  'VITE_AUDIT_SOURCE',
  'VITE_ADMINISTRATION_SOURCE',
  'VITE_ALERTES_SOURCE',
  'VITE_SYNCHRONISATION_X3_SOURCE'
] as const;

const setAllSources = (mode: 'api' | 'mock' | 'disabled') => {
  SOURCE_ENV_KEYS.forEach((key) => vi.stubEnv(key, mode));
};

describe('dataSources', () => {
  beforeEach(() => {
    __resetDataSourceWarnings();
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it('reconnaît le mode api', () => {
    vi.stubEnv('VITE_AUTH_SOURCE', 'api');
    expect(getDataSourceMode('auth')).toBe('api');
    expect(isApiSource('auth')).toBe(true);
  });

  it('reconnaît le mode mock', () => {
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');
    expect(isMockSource('dashboard')).toBe(true);
  });

  it('reconnaît le mode disabled', () => {
    vi.stubEnv('VITE_SYNCHRONISATION_X3_SOURCE', 'disabled');
    expect(isDisabledSource('synchronisationX3')).toBe(true);
  });

  it('signale une valeur invalide et utilise le défaut explicite', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'inconnu');

    expect(getDataSourceMode('dashboard')).toBe('mock');
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('VITE_DASHBOARD_SOURCE="inconnu"'));
  });

  it('donne la priorité à la variable spécifique', () => {
    vi.stubEnv('VITE_AUTH_SOURCE', 'api');
    vi.stubEnv('VITE_USE_REAL_API', 'false');
    expect(getDataSourceMode('auth')).toBe('api');
  });

  it('accepte temporairement le fallback VITE_USE_REAL_API', () => {
    vi.stubEnv('VITE_AUTH_SOURCE', '');
    vi.stubEnv('VITE_USE_REAL_API', 'false');
    expect(getDataSourceMode('auth')).toBe('mock');
  });

  it('calcule un mode global API', () => {
    setAllSources('api');
    expect(getGlobalMode()).toBe('api');
  });

  it('calcule un mode global démonstration', () => {
    setAllSources('mock');
    expect(getGlobalMode()).toBe('mock');
  });

  it('calcule un mode global hybride', () => {
    setAllSources('api');
    vi.stubEnv('VITE_DASHBOARD_SOURCE', 'mock');
    vi.stubEnv('VITE_SYNCHRONISATION_X3_SOURCE', 'disabled');
    expect(getGlobalMode()).toBe('hybride');
  });
});
