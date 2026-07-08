import { UserRole } from '@/types/common.types';

export const routePermissions: Record<string, UserRole[]> = {
  Dashboard: ['Admin'],
  BilanMatiere: ['Admin'],
  Inventaire: ['Admin'],
  SortiesX3: ['Admin'],
  ImportsX3: ['Admin'],
  DechetsFonderie: ['Admin', 'ResponsableFonderie'],
  ReceptionsUsinage2: ['Admin', 'ResponsableFonderie', 'ResponsableUsinage2'],
  Rapports: ['Admin'],
  Alertes: ['Admin'],
  Admin: ['Admin'],
  Audit: ['Admin']
};

export const hasPermission = (userRole: UserRole | undefined, routeName: string): boolean => {
  if (!userRole) return false;
  const allowedRoles = routePermissions[routeName];
  if (!allowedRoles) return false;
  return allowedRoles.includes(userRole);
};
