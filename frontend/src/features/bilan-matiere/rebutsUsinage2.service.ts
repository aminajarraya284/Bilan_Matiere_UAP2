import api from '@/services/api';
import { assertApiSource, isApiSource } from '@/config/dataSources';
import type {
  PagedRebutsUsinage2,
  RebutUsinage2,
  RebutUsinage2Payload,
  RebutsUsinage2Filters
} from './rebutsUsinage2.types';

/**
 * Rebuts Usinage 2. Le backend les expose sous la meme policy que les receptions U2
 * (`ReadReceptionsUsinage2` / `ManageReceptionsUsinage2`) : la verticale de routage
 * `receptionsU2` est donc reutilisee, sans introduire de source supplementaire.
 *
 * Aucun calcul metier ici : le Bilan U2 est recalcule par le backend a partir des
 * rebuts VALIDES de la periode.
 */
export const rebutsUsinage2Service = {
  isAvailable(): boolean {
    return isApiSource('receptionsU2');
  },

  async getRebuts(filters: RebutsUsinage2Filters): Promise<PagedRebutsUsinage2> {
    assertApiSource('receptionsU2');
    return (await api.get<PagedRebutsUsinage2>('/v1/rebuts-usinage2', { params: filters })).data;
  },

  async createRebut(payload: RebutUsinage2Payload): Promise<RebutUsinage2> {
    assertApiSource('receptionsU2');
    return (await api.post<RebutUsinage2>('/v1/rebuts-usinage2', payload)).data;
  },

  async updateRebut(id: string, payload: RebutUsinage2Payload): Promise<RebutUsinage2> {
    assertApiSource('receptionsU2');
    return (await api.put<RebutUsinage2>(`/v1/rebuts-usinage2/${id}`, payload)).data;
  },

  async validerRebut(id: string): Promise<RebutUsinage2> {
    assertApiSource('receptionsU2');
    return (await api.post<RebutUsinage2>(`/v1/rebuts-usinage2/${id}/valider`)).data;
  },

  async annulerRebut(id: string): Promise<RebutUsinage2> {
    assertApiSource('receptionsU2');
    return (await api.post<RebutUsinage2>(`/v1/rebuts-usinage2/${id}/annuler`)).data;
  }
};
