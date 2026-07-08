import { describe, expect, it } from 'vitest';
import { hasPermission } from './permissions';

describe('route permissions', () => {
  it('reserve les ecrans Admin au role Admin', () => {
    expect(hasPermission('Admin', 'ImportsX3')).toBe(true);
    expect(hasPermission('Admin', 'BilanMatiere')).toBe(true);
    expect(hasPermission('Admin', 'Admin')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'ImportsX3')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'ImportsX3')).toBe(false);
  });

  it('autorise Responsable Fonderie sur dechets et receptions U2 seulement', () => {
    expect(hasPermission('ResponsableFonderie', 'DechetsFonderie')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'ReceptionsUsinage2')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'BilanMatiere')).toBe(false);
  });

  it('autorise Responsable Usinage 2 sur receptions U2 seulement', () => {
    expect(hasPermission('ResponsableUsinage2', 'ReceptionsUsinage2')).toBe(true);
    expect(hasPermission('ResponsableUsinage2', 'DechetsFonderie')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'BilanMatiere')).toBe(false);
  });

  it('refuse une route protegee non declaree', () => {
    expect(hasPermission('Admin', 'RouteInconnue')).toBe(false);
  });
});
