import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type { ConsommationsImportResult, ImportX3Response, PagedConsommationsComposantsResponse } from './sortiesX3.types';

export interface SortiesX3Query {
  numeroOF?: string;
  signal?: AbortSignal;
}

export const sortiesX3Service = {
  async getMouvements(query: SortiesX3Query = {}): Promise<PagedConsommationsComposantsResponse> {
    assertApiSource('sortiesX3');
    return (await api.get<PagedConsommationsComposantsResponse>('/v1/consommations-composants', {
      params: {
        page: 1,
        pageSize: 100,
        ...(query.numeroOF ? { numeroOF: query.numeroOF } : {})
      },
      signal: query.signal
    })).data;
  },

  async importExcelFiles(mouvementFile: File, encoursFile: File): Promise<ConsommationsImportResult> {
    assertApiSource('sortiesX3');
    const formData = new FormData();
    formData.append('encoursProduct', encoursFile);
    formData.append('mouvementsStock', mouvementFile);
    const response = await api.post<ImportX3Response>('/v1/consommations-composants/imports', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return {
      successCount: response.data.sortiesGenerees,
      mouvementCount: response.data.mouvementsStock.lignesValides,
      encoursCount: response.data.encoursProduct.lignesValides,
      unmatchedCount: response.data.nombreAnomalies,
      consommations: response.data.consommations
    };
  },

  async triggerSynchroniser(): Promise<{ count: number }> {
    assertApiSource('sortiesX3');
    return { count: (await this.getMouvements()).total };
  }
};
