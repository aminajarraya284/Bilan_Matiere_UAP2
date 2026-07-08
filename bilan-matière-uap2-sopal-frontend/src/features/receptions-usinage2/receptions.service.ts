import api from '@/services/api';
import {
  ControleNonReception,
  ReceptionUsinage2Mouvement,
  VerificationControleNonReceptionPayload
} from './receptions.types';
import { storage } from '@/utils/storage';

const mockControleNonReception: ControleNonReception[] = [
  {
    id: 1,
    codeArticle: 'ART-00912',
    designationArticle: 'Corps Robinet 1/2',
    codeTypeMatiere: 'M3010L0',
    quantiteEmrX3: 28550.0,
    quantiteReceptionneeU2Application: 28495.0,
    quantiteNonReceptionnee: 55.0,
    nombrePieces: 67880,
    ecartParPiece: 55.0 / 67880,
    statutControle: 'AVerifier',
    observation: 'Écart de réception à vérifier par le responsable',
    justification: '',
    dateSynchronisationX3: '2026-07-07T08:00:00',
    of: 'OF-9831',
    sourceEmr: 'EtatX3Synchronise'
  },
  {
    id: 2,
    codeArticle: 'ART-00984',
    designationArticle: 'Raccord Coudé M2',
    codeTypeMatiere: 'M3000L0',
    quantiteEmrX3: 12050.0,
    quantiteReceptionneeU2Application: 11937.0,
    quantiteNonReceptionnee: 113.0,
    nombrePieces: 66322,
    ecartParPiece: 113.0 / 66322,
    statutControle: 'Anomalie',
    observation: 'Décalage important constaté',
    justification: '',
    dateSynchronisationX3: '2026-07-07T09:30:00',
    of: 'OF-9832',
    sourceEmr: 'RapportJournalierMachine'
  },
  {
    id: 3,
    codeArticle: 'ART-01005',
    designationArticle: "Robinet d'arrêt DN20",
    codeTypeMatiere: 'M3010L0',
    quantiteEmrX3: 900.0,
    quantiteReceptionneeU2Application: 900.0,
    quantiteNonReceptionnee: 0.0,
    nombrePieces: 1380,
    ecartParPiece: 0.0,
    statutControle: 'OK',
    observation: 'Quantités parfaitement alignées',
    justification: '',
    dateSynchronisationX3: '2026-07-07T10:15:00',
    of: 'OF-9835',
    sourceEmr: 'EtatX3Synchronise'
  }
];

const cloneControle = (item: ControleNonReception): ControleNonReception => ({ ...item });

const updateMockControle = (
  id: number,
  payload: VerificationControleNonReceptionPayload
): ControleNonReception => {
  const index = mockControleNonReception.findIndex((item) => item.id === id);
  if (index === -1) {
    throw new Error('Controle non reception introuvable');
  }

  mockControleNonReception[index] = {
    ...mockControleNonReception[index],
    statutControle: payload.statutControle,
    observation: payload.commentaireControle || payload.observation || mockControleNonReception[index].observation,
    justification: payload.justification || mockControleNonReception[index].justification
  };

  return cloneControle(mockControleNonReception[index]);
};

export const receptionsService = {
  async getReceptions(): Promise<ReceptionUsinage2Mouvement[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return storage.getReceptions();
    }
    const res = await api.get<ReceptionUsinage2Mouvement[]>('/receptions-usinage-2');
    return res.data;
  },

  async createReception(
    data: Omit<ReceptionUsinage2Mouvement, 'id' | 'source' | 'utilisateur'>
  ): Promise<ReceptionUsinage2Mouvement> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getReceptions();
      const newId = current.length ? Math.max(...current.map((r) => r.id)) + 1 : 1;
      const newItem: ReceptionUsinage2Mouvement = {
        ...data,
        id: newId,
        source: 'Saisie Manuelle',
        utilisateur: 'K. BEN ALI'
      };
      storage.saveReceptions([newItem, ...current]);
      return newItem;
    }
    const res = await api.post<ReceptionUsinage2Mouvement>('/receptions-usinage-2', data);
    return res.data;
  },

  async deleteReception(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getReceptions();
      const updated = current.filter((r) => r.id !== id);
      storage.saveReceptions(updated);
      return;
    }
    await api.delete(`/receptions-usinage-2/${id}`);
  },

  async triggerSynchroniser(): Promise<{ count: number }> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return new Promise((resolve) => {
        setTimeout(() => {
          resolve({ count: 4 });
        }, 1000);
      });
    }
    const res = await api.post<{ count: number }>('/receptions-usinage-2/synchroniser');
    return res.data;
  },

  async getControleNonReception(): Promise<ControleNonReception[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return mockControleNonReception.map(cloneControle);
    }
    const res = await api.get<ControleNonReception[]>('/receptions-usinage-2/controle-non-reception');
    return res.data;
  },

  async verifierControleNonReception(
    id: number,
    payload: VerificationControleNonReceptionPayload
  ): Promise<ControleNonReception> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return updateMockControle(id, payload);
    }
    const res = await api.post<ControleNonReception>(
      `/receptions-usinage-2/controle-non-reception/${id}/verifier`,
      payload
    );
    return res.data;
  },

  async justifierControleNonReception(
    id: number,
    payload: VerificationControleNonReceptionPayload
  ): Promise<ControleNonReception> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return updateMockControle(id, payload);
    }
    const res = await api.post<ControleNonReception>(
      `/receptions-usinage-2/controle-non-reception/${id}/justifier`,
      payload
    );
    return res.data;
  }
};
