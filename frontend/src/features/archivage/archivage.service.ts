import api from '@/services/api';
import { reportExporter } from '@/features/rapports/exporter';
import type { ArchiveDetail, PagedArchives } from './archivage.types';

export const archivageService = {
  async enregistrer(periode: string): Promise<ArchiveDetail> {
    return (await api.post<ArchiveDetail>(`/v1/periodes-bilan/${encodeURIComponent(periode)}/enregistrer`)).data;
  },

  async rechercher(params: { page?: number; pageSize?: number; periode?: string; statut?: string } = {}): Promise<PagedArchives> {
    return (await api.get<PagedArchives>('/v1/periodes-bilan', { params })).data;
  },

  async get(periode: string): Promise<ArchiveDetail> {
    return (await api.get<ArchiveDetail>(`/v1/periodes-bilan/${encodeURIComponent(periode)}`)).data;
  },

  async telechargerRapport(periode: string): Promise<void> {
    const response = await api.get<Blob>(`/v1/periodes-bilan/${encodeURIComponent(periode)}/rapport`, {
      responseType: 'blob'
    });
    reportExporter.download({ blob: response.data, fileName: `rapport-bilan-matiere-${periode}.md` });
  }
};

