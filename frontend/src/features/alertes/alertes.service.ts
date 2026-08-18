import { assertApiSource } from '@/config/dataSources';
import api from '@/services/api';
import type {
  AlerteDetail,
  AlerteRecalculResult,
  AlertesFilters,
  IgnorerAlerteRequest,
  PagedAlertesResponse,
  TraiterAlerteRequest
} from './alertes.types';

export interface SearchAlertesOptions {
  filters: AlertesFilters;
  page: number;
  pageSize: number;
}

const compactParams = (options: SearchAlertesOptions): Record<string, string | number> => {
  const { filters } = options;
  const params: Record<string, string | number> = {
    page: options.page,
    pageSize: options.pageSize
  };
  for (const [key, value] of Object.entries(filters)) {
    const normalized = value.trim();
    if (normalized) params[key] = normalized;
  }
  return params;
};

export const alertesService = {
  async search(options: SearchAlertesOptions): Promise<PagedAlertesResponse> {
    assertApiSource('alertes');
    const { data } = await api.get<PagedAlertesResponse>('/v1/alertes', {
      params: compactParams(options)
    });
    return data;
  },

  async get(id: string): Promise<AlerteDetail> {
    assertApiSource('alertes');
    const { data } = await api.get<AlerteDetail>(`/v1/alertes/${encodeURIComponent(id)}`);
    return data;
  },

  async recalculer(periode: string): Promise<AlerteRecalculResult> {
    assertApiSource('alertes');
    const { data } = await api.post<AlerteRecalculResult>('/v1/alertes/recalculer', null, {
      params: { periode }
    });
    return data;
  },

  async acquitter(id: string, request: TraiterAlerteRequest): Promise<AlerteDetail> {
    assertApiSource('alertes');
    const { data } = await api.post<AlerteDetail>(`/v1/alertes/${encodeURIComponent(id)}/acquitter`, request);
    return data;
  },

  async resoudre(id: string, request: TraiterAlerteRequest): Promise<AlerteDetail> {
    assertApiSource('alertes');
    const { data } = await api.post<AlerteDetail>(`/v1/alertes/${encodeURIComponent(id)}/resoudre`, request);
    return data;
  },

  async ignorer(id: string, request: IgnorerAlerteRequest): Promise<AlerteDetail> {
    assertApiSource('alertes');
    const { data } = await api.post<AlerteDetail>(`/v1/alertes/${encodeURIComponent(id)}/ignorer`, request);
    return data;
  }
};
