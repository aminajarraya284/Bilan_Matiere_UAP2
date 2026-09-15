import { assertEnabled, isApiSource, isMockSource } from '@/config/dataSources';
import api from '@/services/api';
import type { DashboardResponse } from './dashboard.types';

const PERIOD_PATTERN = /^\d{4}-(0[1-9]|1[0-2])$/;

export const isDashboardPeriod = (value: string): boolean => PERIOD_PATTERN.test(value);

export const validateDashboardPeriod = (periode: string): string => {
  const normalized = periode.trim();
  if (!isDashboardPeriod(normalized)) {
    throw new Error('La période doit respecter le format AAAA-MM.');
  }
  return normalized;
};

export const dashboardService = {
  async getDashboard(periode: string): Promise<DashboardResponse> {
    assertEnabled('dashboard');
    const normalizedPeriod = validateDashboardPeriod(periode);

    if (isApiSource('dashboard')) {
      const response = await api.get<DashboardResponse>('/v1/dashboard', {
        params: { periode: normalizedPeriod }
      });
      return response.data;
    }

    // Le mock est explicitement réservé à Development/test. Cette branche est
    // éliminée du bundle de production par Vite et ne sert jamais de fallback API.
    if (isMockSource('dashboard') && (import.meta.env.DEV || import.meta.env.MODE === 'test')) {
      const { createDashboardMock } = await import('./dashboard.mock');
      return createDashboardMock(normalizedPeriod);
    }

    throw new Error('Le mode démonstration du Dashboard est indisponible dans ce build.');
  }
};
