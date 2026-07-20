import api from '@/services/api';
import { DechetMouvement } from './dechets.types';
import { testDataRepository } from '@/services/testDataRepository.service';
import type { DechetFonderieTest } from '@/mocks/sopalTestDataset';

const toView = (item: DechetFonderieTest): DechetMouvement => ({
  id: item.id,
  dateDeclaration: item.dateDeclaration,
  semaine: item.semaine,
  codeTypeMatiere: item.codeTypeMatiere,
  quantiteKg: item.poidsDechetKg,
  unite: 'KG',
  typeDechet: item.typeDechet,
  commentaire: item.observation,
  statut: item.statut,
  utilisateur: item.declarantNom
});

export const dechetsService = {
  async getDechets(): Promise<DechetMouvement[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return testDataRepository.getDataset().dechetsFonderie.map(toView);
    }
    const res = await api.get<DechetMouvement[]>('/dechets-fonderie');
    return res.data;
  },

  async createDechet(data: Omit<DechetMouvement, 'id' | 'statut' | 'utilisateur'>): Promise<DechetMouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = testDataRepository.getDataset().dechetsFonderie;
      const newId = current.length ? Math.max(...current.map((d) => d.id)) + 1 : 1;
      const newItem: DechetFonderieTest = {
        id: newId,
        dateDeclaration: data.dateDeclaration,
        semaine: data.semaine,
        codeTypeMatiere: data.codeTypeMatiere as DechetFonderieTest['codeTypeMatiere'],
        typeDechet: data.typeDechet,
        poidsDechetKg: data.quantiteKg,
        observation: data.commentaire || '',
        declarantId: 'U-FONDERIE',
        declarantNom: 'Responsable Fonderie',
        creeLe: new Date().toISOString(),
        statut: 'Valide' // Set to Valide to count immediately in balance calculations
      };
      testDataRepository.saveDechets([newItem, ...current]);
      return toView(newItem);
    }
    const res = await api.post<DechetMouvement>('/dechets-fonderie', data);
    return res.data;
  },

  async updateDechet(id: number, data: Partial<DechetMouvement>): Promise<DechetMouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = testDataRepository.getDataset().dechetsFonderie;
      const idx = current.findIndex((d) => d.id === id);
      if (idx !== -1) {
        const previous = current[idx];
        current[idx] = {
          ...previous,
          ...(data.dateDeclaration ? { dateDeclaration: data.dateDeclaration } : {}),
          ...(data.semaine !== undefined ? { semaine: data.semaine } : {}),
          ...(data.codeTypeMatiere
            ? { codeTypeMatiere: data.codeTypeMatiere as DechetFonderieTest['codeTypeMatiere'] }
            : {}),
          ...(data.quantiteKg !== undefined ? { poidsDechetKg: data.quantiteKg } : {}),
          ...(data.typeDechet ? { typeDechet: data.typeDechet } : {}),
          ...(data.commentaire !== undefined ? { observation: data.commentaire } : {}),
          ...(data.statut ? { statut: data.statut } : {})
        };
        testDataRepository.saveDechets(current);
        return toView(current[idx]);
      }
      throw new Error('Mouvement déchet non trouvé');
    }
    const res = await api.put<DechetMouvement>(`/dechets-fonderie/${id}`, data);
    return res.data;
  },

  async deleteDechet(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = testDataRepository.getDataset().dechetsFonderie;
      const updated = current.filter((d) => d.id !== id);
      testDataRepository.saveDechets(updated);
      return;
    }
    await api.delete(`/dechets-fonderie/${id}`);
  }
};
