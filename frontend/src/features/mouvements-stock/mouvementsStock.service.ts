import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type {
  ImportTracking,
  MouvementsStockFilters,
  MouvementsStockPage
} from './mouvementsStock.types';

const compactParams = (filters: MouvementsStockFilters): Record<string, string | number> =>
  Object.fromEntries(
    Object.entries(filters).filter(([, value]) => value !== undefined && value !== null && value !== '')
  ) as Record<string, string | number>;

export const mouvementsStockService = {
  async search(filters: MouvementsStockFilters): Promise<MouvementsStockPage> {
    assertApiSource('etatsX3');
    const { data } = await api.get<MouvementsStockPage>('/v1/mouvements-stock', {
      params: compactParams(filters)
    });
    return data;
  },

  async getImportTracking(): Promise<ImportTracking> {
    assertApiSource('etatsX3');
    const { data } = await api.get<ImportTracking>('/v1/mouvements-stock/imports/statut');
    return data;
  }
};
