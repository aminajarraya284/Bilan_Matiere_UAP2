import { assertApiSource } from '@/config/dataSources';
import api from '@/services/api';
import type { AuditDetail, AuditFilters, PagedAuditResponse } from './audit.types';

export interface SearchAuditOptions {
  filters: AuditFilters;
  page: number;
  pageSize: number;
}

const compactParams = (options: SearchAuditOptions): Record<string, string | number> => {
  const params: Record<string, string | number> = {
    page: options.page,
    pageSize: options.pageSize
  };
  for (const [key, value] of Object.entries(options.filters)) {
    const normalized = value.trim();
    if (normalized) params[key] = normalized;
  }
  return params;
};

export const auditService = {
  async search(options: SearchAuditOptions): Promise<PagedAuditResponse> {
    assertApiSource('audit');
    const { data } = await api.get<PagedAuditResponse>('/v1/audit', {
      params: compactParams(options)
    });
    return data;
  },

  async getDetail(id: string): Promise<AuditDetail> {
    assertApiSource('audit');
    const { data } = await api.get<AuditDetail>(`/v1/audit/${encodeURIComponent(id)}`);
    return data;
  }
};
