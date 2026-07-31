import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type {
  ImportTracking,
  RapportMachineFilters,
  RapportMachinePage
} from './rapportMachine.types';

const compactParams = (filters: RapportMachineFilters): Record<string, string | number> =>
  Object.fromEntries(
    Object.entries(filters).filter(([, value]) => value !== undefined && value !== null && value !== '')
  ) as Record<string, string | number>;

export const rapportMachineService = {
  async search(filters: RapportMachineFilters): Promise<RapportMachinePage> {
    assertApiSource('etatsX3');
    const { data } = await api.get<RapportMachinePage>('/v1/rapports-machine', {
      params: compactParams(filters)
    });
    return data;
  },

  async getImportTracking(): Promise<ImportTracking> {
    assertApiSource('etatsX3');
    const { data } = await api.get<ImportTracking>('/v1/rapports-machine/imports/statut');
    return data;
  }
};
