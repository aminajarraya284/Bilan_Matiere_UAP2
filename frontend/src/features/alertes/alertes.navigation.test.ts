import { describe, expect, it } from 'vitest';
import { getAlerteSourceRoute } from './alertes.navigation';
import { createAlerteListItem } from './alertes.test.fixtures';

describe('getAlerteSourceRoute', () => {
  it('construit les routes existantes avec la période et le numéro OF', () => {
    expect(getAlerteSourceRoute(createAlerteListItem({ source: 'Bilan Fonderie' }), ['Admin'])).toBe(
      '/bilan-matiere?tab=fonderie&period=2026-01'
    );
    expect(getAlerteSourceRoute(createAlerteListItem(), ['ResponsableUsinage2'])).toBe(
      '/bilan-matiere?tab=usinage2&period=2026-01&numeroOF=OF-001'
    );
    expect(getAlerteSourceRoute(createAlerteListItem({ source: 'Bilan UAP2' }), ['Admin'])).toBe(
      '/bilan-uap2?periode=2026-01&numeroOF=OF-001'
    );
  });

  it('masque une route source hors du rôle courant ou inconnue', () => {
    expect(getAlerteSourceRoute(createAlerteListItem({ source: 'Import X3' }), ['ResponsableUsinage2'])).toBeNull();
    expect(getAlerteSourceRoute(createAlerteListItem({ source: 'Source inconnue' }), ['Admin'])).toBeNull();
  });
});
