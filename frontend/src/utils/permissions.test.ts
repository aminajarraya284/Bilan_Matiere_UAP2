import { describe, expect, it } from 'vitest';
import { hasPermission } from './permissions';

describe('route permissions', () => {
  it('réserve les rapports et les archives au rôle Admin', () => {
    expect(hasPermission('Admin', 'Rapports')).toBe(true);
    expect(hasPermission('Admin', 'HistoriquePeriodes')).toBe(true);
    expect(hasPermission('Admin', 'PeriodeArchivee')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'Rapports')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'Rapports')).toBe(false);
    expect(hasPermission('ResponsableFonderie', 'HistoriquePeriodes')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'PeriodeArchivee')).toBe(false);
  });
  it('reserve les ecrans Admin au role Admin', () => {
    expect(hasPermission('Admin', 'ImportsX3')).toBe(false);
    expect(hasPermission('Admin', 'Dashboard')).toBe(true);
    expect(hasPermission('Admin', 'BilanMatiere')).toBe(true);
    expect(hasPermission('Admin', 'Admin')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'ImportsX3')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'ImportsX3')).toBe(false);
  });

  it('autorise Responsable Fonderie sur ses operations', () => {
    expect(hasPermission('ResponsableFonderie', 'ControlePoidsFonderie')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'DechetsFonderie')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'ReceptionsUsinage2')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'Dashboard')).toBe(true);
    expect(hasPermission('ResponsableFonderie', 'BilanMatiere')).toBe(true);
  });

  it('autorise Responsable Usinage 2 sur le dashboard et ses bilans', () => {
    expect(hasPermission('ResponsableUsinage2', 'ReceptionsUsinage2')).toBe(true);
    expect(hasPermission('ResponsableUsinage2', 'DechetsFonderie')).toBe(false);
    expect(hasPermission('ResponsableUsinage2', 'Dashboard')).toBe(true);
    expect(hasPermission('ResponsableUsinage2', 'BilanMatiere')).toBe(true);
  });

  it('refuse une route protegee non declaree', () => {
    expect(hasPermission('Admin', 'RouteInconnue')).toBe(false);
  });
});
