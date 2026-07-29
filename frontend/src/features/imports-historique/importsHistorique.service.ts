import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type { ImportsReelsDiagnostic, ImportSourceFile } from './importsHistorique.types';

// Historique des imports reels sans equivalent mock (donnees serveur uniquement),
// meme principe que les autres services API specialises.
export const importsHistoriqueService = {
  async getDiagnostic(): Promise<ImportsReelsDiagnostic> {
    assertApiSource('historiqueImports');
    const { data } = await api.get<ImportsReelsDiagnostic>('/v1/diagnostics/imports-reels');
    return data;
  },

  async getFiles(importBatchId: string): Promise<ImportSourceFile[]> {
    assertApiSource('historiqueImports');
    const { data } = await api.get<ImportSourceFile[]>(`/v1/imports/${importBatchId}/files`);
    return data;
  },

  async download(importBatchId: string, fileId: string, fileName: string): Promise<void> {
    assertApiSource('historiqueImports');
    const response = await api.get(`/v1/imports/${importBatchId}/files/${fileId}/download`, {
      responseType: 'blob'
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }
};
