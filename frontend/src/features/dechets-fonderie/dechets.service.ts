import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type {
  DechetFonderiePayload,
  DechetFonderieResponse,
  DechetsFonderieFilters,
  PagedDechetsFonderieResponse,
  ResumeDechetsFonderieResponse
} from './dechets.types';
import type { TypeMatiereResponse } from './typesMatiere.types';

export const dechetsService = {
  async getTypesMatiere(): Promise<TypeMatiereResponse[]> {
    assertApiSource('typesMatiere');
    return (await api.get<TypeMatiereResponse[]>('/v1/types-matiere', { params: { actif: true } })).data;
  },

  async getDechetsFonderie(filters: DechetsFonderieFilters): Promise<PagedDechetsFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.get<PagedDechetsFonderieResponse>('/v1/dechets-fonderie', { params: filters })).data;
  },

  async getDechetFonderieById(id: string): Promise<DechetFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.get<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}`)).data;
  },

  async createDechetFonderie(payload: DechetFonderiePayload): Promise<DechetFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.post<DechetFonderieResponse>('/v1/dechets-fonderie', payload)).data;
  },

  async updateDechetFonderie(id: string, payload: DechetFonderiePayload): Promise<DechetFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.put<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}`, payload)).data;
  },

  async validerDechetFonderie(id: string): Promise<DechetFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.post<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}/valider`)).data;
  },

  async annulerDechetFonderie(id: string): Promise<DechetFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (await api.post<DechetFonderieResponse>(`/v1/dechets-fonderie/${id}/annuler`)).data;
  },

  // Alias temporaire pour les consommateurs historiques : aucune suppression
  // n'est effectuee, l'operation appelle le cycle de vie Annule.
  async deleteDechetFonderie(id: string): Promise<void> {
    await this.annulerDechetFonderie(id);
  },

  async getResumeDechetsFonderie(periode: string): Promise<ResumeDechetsFonderieResponse> {
    assertApiSource('dechetsFonderie');
    return (
      await api.get<ResumeDechetsFonderieResponse>('/v1/dechets-fonderie/resume', {
        params: { periode }
      })
    ).data;
  }
};
