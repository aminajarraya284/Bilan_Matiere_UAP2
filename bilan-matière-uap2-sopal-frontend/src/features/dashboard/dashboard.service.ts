import api from '@/services/api';
import { calculateDashboard } from '@/services/bilanCalculation.service';
import { testDataRepository } from '@/services/testDataRepository.service';
import type {
  DashboardAlert,
  DashboardBilanRow,
  DashboardDataSourceStatus,
  DashboardFilterOptions,
  DashboardStats
} from './dashboard.types';

export type DashboardData = {
  stats: DashboardStats;
  alerts: DashboardAlert[];
  rows: DashboardBilanRow[];
  sources?: DashboardDataSourceStatus[];
  filterOptions?: DashboardFilterOptions;
};

export const dashboardService = {
  async getDashboardData(periode: string, dateDebut?: string, dateFin?: string): Promise<DashboardData> {
    if (import.meta.env.VITE_USE_REAL_API === 'true') {
      const [stats, alerts, rows] = await Promise.all([
        api.get<DashboardStats>('/dashboard/summary', { params: { periode, dateDebut, dateFin } }),
        api.get<DashboardAlert[]>('/dashboard/alerts', { params: { periode, dateDebut, dateFin } }),
        api.get<DashboardBilanRow[]>('/dashboard/bilan-par-matiere', { params: { periode, dateDebut, dateFin } })
      ]);
      return { stats: stats.data, alerts: alerts.data, rows: rows.data };
    }
    return calculateDashboard(testDataRepository.getDataset(), periode);
  },

  async getStats(periode: string, dateDebut?: string, dateFin?: string): Promise<DashboardStats> {
    return (await this.getDashboardData(periode, dateDebut, dateFin)).stats;
  },

  async getAlerts(periode: string, dateDebut?: string, dateFin?: string): Promise<DashboardAlert[]> {
    return (await this.getDashboardData(periode, dateDebut, dateFin)).alerts;
  },

  async getBilanParMatiere(periode: string, dateDebut?: string, dateFin?: string): Promise<DashboardBilanRow[]> {
    return (await this.getDashboardData(periode, dateDebut, dateFin)).rows;
  }
};
