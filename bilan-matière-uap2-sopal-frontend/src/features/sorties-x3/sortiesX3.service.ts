import api from '@/services/api';
import { SortieX3Mouvement } from './sortiesX3.types';
import { storage } from '@/utils/storage';
import { x3ReferenceService } from '@/features/reference/x3Reference.service';

export const sortiesX3Service = {
  async getMouvements(): Promise<SortieX3Mouvement[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return storage.getSortiesX3();
    }
    const res = await api.get<SortieX3Mouvement[]>('/sorties-x3');
    return res.data;
  },

  async triggerSynchroniser(): Promise<{ count: number; status: string }> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return new Promise((resolve) => {
        setTimeout(() => {
          resolve({ count: 2, status: 'Success' });
        }, 1000);
      });
    }
    const res = await api.post<{ count: number; status: string }>('/sorties-x3/synchroniser');
    return res.data;
  },

  async importExcel(file: File): Promise<{ successCount: number }> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return new Promise((resolve) => {
        setTimeout(() => {
          resolve({ successCount: 15 });
        }, 1200);
      });
    }
    const formData = new FormData();
    formData.append('file', file);
    const res = await api.post<{ successCount: number }>('/sorties-x3/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return res.data;
  },

  async chargerReferenceIntegree(): Promise<{ successCount: number }> {
    if (import.meta.env.VITE_USE_REAL_API === 'true') {
      throw new Error("La référence intégrée est disponible uniquement en mode mock local.");
    }

    const reference = await x3ReferenceService.getReference();
    const materialLabels = new Map(reference.materials.map((material) => [material.code, material.designation]));
    const today = new Date().toISOString().slice(0, 10);

    const mouvements: SortieX3Mouvement[] = reference.sortiesX3.map((row, index) => {
      const quantiteUtilisee = Math.abs(row.qteConsommee || row.qteBesoin || row.qtePrevue || 0);
      return {
        id: index + 1,
        articleParent: row.of,
        numeroOf: row.of,
        composant: row.composant,
        qtePrevue: row.qtePrevue,
        qteRealisee: row.qteRealisee,
        qteRestante: row.qteRestante,
        qteBesoin: row.qteBesoin,
        qteConsommee: row.qteConsommee,
        dateImputation: today,
        codeMatiere: row.composant,
        designation: materialLabels.get(row.composant) || row.composant,
        dateCreation: today,
        heureCreation: '00:00:00',
        utilisateur: 'Référence ListeOFX3',
        quantiteImportee: -quantiteUtilisee,
        quantiteUtilisee,
        unite: 'KG',
        transaction: row.of,
        typePiece: 'OF',
        lot: row.of,
        pieceOrigine: row.of,
        designationOrigine: 'ListeOFX3.xlsx',
        emplacement: 'X3',
        statut: 'Synchronisé',
        source: 'Fichier Excel'
      };
    });

    storage.saveSortiesX3(mouvements);
    return { successCount: mouvements.length };
  },

  async createMouvement(
    data: Omit<SortieX3Mouvement, 'id' | 'statut' | 'quantiteUtilisee'>
  ): Promise<SortieX3Mouvement> {
    const qteUtilisee = Math.abs(data.qteConsommee ?? data.quantiteImportee);
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getSortiesX3();
      const newId = current.length ? Math.max(...current.map((m) => m.id)) + 1 : 1;
      const newItem: SortieX3Mouvement = {
        ...data,
        id: newId,
        quantiteUtilisee: qteUtilisee,
        statut: 'Synchronisé'
      };
      storage.saveSortiesX3([newItem, ...current]);
      return newItem;
    }
    const res = await api.post<SortieX3Mouvement>('/sorties-x3', { ...data, quantiteUtilisee: qteUtilisee });
    return res.data;
  }
};
