import api from '@/services/api';
import type { ConsommationsImportResult, ImportX3Response, PagedConsommationsComposantsResponse } from './sortiesX3.types';

export const sortiesX3Service = {
  async getMouvements(): Promise<PagedConsommationsComposantsResponse> {
    return (await api.get<PagedConsommationsComposantsResponse>('/v1/consommations-composants', {
      params: { page: 1, pageSize: 100 }
    })).data;
  },

  async importExcelFiles(mouvementFile: File, encoursFile: File): Promise<ConsommationsImportResult> {
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
    return { count: (await this.getMouvements()).total };
  }
};
