import api from '@/services/api';
import { DechetMouvement } from './dechets.types';
import { storage } from '@/utils/storage';

export const dechetsService = {
  async getDechets(): Promise<DechetMouvement[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return storage.getDechets();
    }
    const res = await api.get<DechetMouvement[]>('/dechets-fonderie');
    return res.data;
  },

  async createDechet(data: Omit<DechetMouvement, 'id' | 'statut' | 'utilisateur'>): Promise<DechetMouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getDechets();
      const newId = current.length ? Math.max(...current.map((d) => d.id)) + 1 : 1;
      const newItem: DechetMouvement = {
        ...data,
        id: newId,
        statut: 'Valide', // Set to Valide to count immediately in balance calculations
        utilisateur: 'A. TRABELSI'
      };
      storage.saveDechets([newItem, ...current]);
      return newItem;
    }
    const res = await api.post<DechetMouvement>('/dechets-fonderie', data);
    return res.data;
  },

  async updateDechet(id: number, data: Partial<DechetMouvement>): Promise<DechetMouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getDechets();
      const idx = current.findIndex((d) => d.id === id);
      if (idx !== -1) {
        current[idx] = { ...current[idx], ...data };
        storage.saveDechets(current);
        return current[idx];
      }
      throw new Error('Mouvement déchet non trouvé');
    }
    const res = await api.put<DechetMouvement>(`/dechets-fonderie/${id}`, data);
    return res.data;
  },

  async deleteDechet(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getDechets();
      const updated = current.filter((d) => d.id !== id);
      storage.saveDechets(updated);
      return;
    }
    await api.delete(`/dechets-fonderie/${id}`);
  }
};
