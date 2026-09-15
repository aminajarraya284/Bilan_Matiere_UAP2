import api from '@/services/api';
import { fetchAllPages } from '@/services/pagination';
import { assertApiSource } from '@/config/dataSources';
import type {
  ControlePoidsFonderieApi,
  CreateControlePoidsFonderieRequest,
  UpdateControlePoidsFonderieRequest
} from './controlePoidsFonderie.types';

// Ecran d'administration relie a l'API reelle (le mode mock continue d'utiliser
// controlePoidsFonderie.storage.ts, inchange).
export const controlePoidsFonderieApiService = {
  async getAll(): Promise<ControlePoidsFonderieApi[]> {
    assertApiSource('controlePoids');
    return fetchAllPages<ControlePoidsFonderieApi>('/v1/controles-poids-fonderie', 50);
  },

  async create(request: CreateControlePoidsFonderieRequest): Promise<ControlePoidsFonderieApi> {
    assertApiSource('controlePoids');
    const { data } = await api.post<ControlePoidsFonderieApi>('/v1/controles-poids-fonderie', request);
    return data;
  },

  async update(id: string, request: UpdateControlePoidsFonderieRequest): Promise<ControlePoidsFonderieApi> {
    assertApiSource('controlePoids');
    const { data } = await api.put<ControlePoidsFonderieApi>(`/v1/controles-poids-fonderie/${id}`, request);
    return data;
  },

  async valider(id: string): Promise<ControlePoidsFonderieApi> {
    assertApiSource('controlePoids');
    const { data } = await api.post<ControlePoidsFonderieApi>(`/v1/controles-poids-fonderie/${id}/valider`);
    return data;
  },

  async annuler(id: string): Promise<ControlePoidsFonderieApi> {
    assertApiSource('controlePoids');
    const { data } = await api.post<ControlePoidsFonderieApi>(`/v1/controles-poids-fonderie/${id}/annuler`);
    return data;
  }
};
