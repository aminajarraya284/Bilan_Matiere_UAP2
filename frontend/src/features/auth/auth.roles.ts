import type { UserRole } from '@/types/common.types';

export type ApplicationRole = UserRole;

export const APPLICATION_ROLES: readonly ApplicationRole[] = [
  'Admin',
  'ResponsableFonderie',
  'ResponsableUsinage2'
] as const;

export const normalizeApplicationRoles = (value: unknown): ApplicationRole[] => {
  const rawRoles = Array.isArray(value) ? value : typeof value === 'string' ? [value] : [];
  const roles: ApplicationRole[] = [];

  for (const rawRole of rawRoles) {
    if (typeof rawRole !== 'string') continue;
    if ((APPLICATION_ROLES as readonly string[]).includes(rawRole)) {
      const role = rawRole as ApplicationRole;
      if (!roles.includes(role)) roles.push(role);
    } else if (import.meta.env.DEV) {
      console.warn(`Rôle Entra ID inconnu ignoré : ${rawRole}`);
    }
  }

  return roles;
};

export const getDefaultRouteForRoles = (roles: readonly ApplicationRole[]): string => {
  if (roles.includes('Admin')) return '/dashboard';
  if (roles.includes('ResponsableFonderie')) return '/dechets-fonderie';
  if (roles.includes('ResponsableUsinage2')) return '/receptions-usinage-2';
  return '/forbidden';
};

export const getPrimaryRole = (roles: readonly ApplicationRole[]): ApplicationRole | undefined => {
  if (roles.includes('Admin')) return 'Admin';
  if (roles.includes('ResponsableFonderie')) return 'ResponsableFonderie';
  return roles.includes('ResponsableUsinage2') ? 'ResponsableUsinage2' : undefined;
};
