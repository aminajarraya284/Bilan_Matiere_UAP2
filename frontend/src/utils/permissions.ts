import { UserRole } from '@/types/common.types';

export const routePermissions: Record<string, UserRole[]> = {
  Dashboard: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'],
  BilanMatiere: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'],
  BilanUap2: ['Admin', 'ResponsableUsinage2'],
  Inventaire: ['Admin'],
  ControlePoidsFonderie: ['Admin', 'ResponsableFonderie'],
  SortiesX3: ['Admin'],
  EtatsX3: ['Admin'],
  ReferencesFabrication: ['Admin'],
  DechetsFonderie: ['Admin', 'ResponsableFonderie'],
  ReceptionsUsinage2: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'],
  Rapports: ['Admin'],
  ImportsHistorique: ['Admin'],
  Alertes: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'],
  Admin: ['Admin'],
  Audit: ['Admin'],
  HistoriquePeriodes: ['Admin'],
  PeriodeArchivee: ['Admin']
};

export const hasPermission = (userRole: UserRole | undefined, routeName: string): boolean => {
  if (!userRole) return false;
  const allowedRoles = routePermissions[routeName];
  if (!allowedRoles) return false;
  return allowedRoles.includes(userRole);
};

export const hasAnyPermission = (userRoles: readonly UserRole[], routeName: string): boolean => {
  const allowedRoles = routePermissions[routeName];
  if (!allowedRoles) return false;
  return userRoles.some((role) => allowedRoles.includes(role));
};
