import type { ApplicationRole } from '@/features/auth/auth.types';
import type { AlerteDetail, AlerteListItem } from './alertes.types';

type AlerteSource = Pick<AlerteListItem, 'source' | 'periode' | 'numeroOF'> | AlerteDetail;

const hasRole = (roles: readonly ApplicationRole[], ...allowed: ApplicationRole[]): boolean =>
  roles.some((role) => allowed.includes(role));

const query = (values: Record<string, string | null>): string => {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value) params.set(key, value);
  }
  const serialized = params.toString();
  return serialized ? `?${serialized}` : '';
};

export const getAlerteSourceRoute = (alerte: AlerteSource, roles: readonly ApplicationRole[]): string | null => {
  if (alerte.source === 'Bilan Fonderie' && hasRole(roles, 'Admin', 'ResponsableFonderie')) {
    return `/bilan-matiere${query({ tab: 'fonderie', period: alerte.periode })}`;
  }
  if (alerte.source === 'Bilan Usinage 2' && hasRole(roles, 'Admin', 'ResponsableUsinage2')) {
    return `/bilan-matiere${query({ tab: 'usinage2', period: alerte.periode, numeroOF: alerte.numeroOF })}`;
  }
  if (alerte.source === 'Bilan UAP2' && hasRole(roles, 'Admin', 'ResponsableUsinage2')) {
    return `/bilan-uap2${query({ periode: alerte.periode, numeroOF: alerte.numeroOF })}`;
  }
  if (alerte.source === 'Import X3' && hasRole(roles, 'Admin')) {
    return '/imports-historique';
  }
  return null;
};
