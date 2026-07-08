import api from '@/services/api';
import {
  InventaireConteneur,
  InventaireCategorie,
  InventaireFour,
  InventaireTotalParMatiere
} from './inventaire.types';
import { storage } from '@/utils/storage';

const FOUR_NAMES: InventaireFour['four'][] = ['Four CBP 01', 'Four CBP 02', 'Four CDQ', 'Autre'];

const isFourCategory = (value?: string): value is InventaireFour['four'] => {
  return FOUR_NAMES.includes((value || 'Autre') as InventaireFour['four']);
};

const normalizeCategorie = (item: InventaireCategorie): InventaireCategorie => ({
  ...item,
  categorieInventaire: item.categorieInventaire ?? 'CategorieBilan',
  inclusDansBilanFonderie: item.inclusDansBilanFonderie ?? true,
  visibleDansRapportInventaire: item.visibleDansRapportInventaire ?? true
});

const normalizeFour = (item: InventaireFour): InventaireFour => ({
  ...item,
  inclusDansBilanFonderie: false,
  visibleDansRapportInventaire: true
});

const getLegacyFourRowsFromCategories = (): InventaireFour[] => {
  return storage
    .getCategories()
    .filter((item) => isFourCategory(item.categorie))
    .map((item) =>
      normalizeFour({
        id: item.id,
        dateInventaire: `${item.annee}-${String(item.mois).padStart(2, '0')}-01`,
        mois: item.mois,
        annee: item.annee,
        four: item.categorie as InventaireFour['four'],
        codeTypeMatiere: item.codeTypeMatiere,
        quantite: item.quantiteReelle,
        unite: item.unite,
        observation: item.commentaire,
        statut: item.statut === 'Cloture' ? 'Valide' : item.statut === 'AValider' ? 'Brouillon' : item.statut,
        inclusDansBilanFonderie: false,
        visibleDansRapportInventaire: true
      })
    );
};

const getMockCategoriesNormalized = (): InventaireCategorie[] => {
  return storage
    .getCategories()
    .filter((item) => !isFourCategory(item.categorie))
    .map(normalizeCategorie);
};

const getMockFoursNormalized = (): InventaireFour[] => {
  const current = storage.getFours().map(normalizeFour);
  if (current.length > 0) return current;
  return getLegacyFourRowsFromCategories();
};

export const inventaireService = {
  async getConteneurs(): Promise<InventaireConteneur[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return storage.getConteneurs();
    }
    const res = await api.get<InventaireConteneur[]>('/inventaires/conteneurs');
    return res.data;
  },

  async createConteneur(data: Omit<InventaireConteneur, 'id' | 'statut'>): Promise<InventaireConteneur> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getConteneurs();
      const newId = current.length ? Math.max(...current.map((c) => c.id)) + 1 : 1;
      const newItem: InventaireConteneur = { ...data, id: newId, statut: 'Valide' };
      storage.saveConteneurs([...current, newItem]);
      return newItem;
    }
    const res = await api.post<InventaireConteneur>('/inventaires/conteneurs', data);
    return res.data;
  },

  async updateConteneur(id: number, data: Partial<InventaireConteneur>): Promise<InventaireConteneur> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getConteneurs();
      const idx = current.findIndex((c) => c.id === id);
      if (idx !== -1) {
        current[idx] = { ...current[idx], ...data };
        storage.saveConteneurs(current);
        return current[idx];
      }
      throw new Error('Conteneur non trouvé');
    }
    const res = await api.put<InventaireConteneur>(`/inventaires/conteneurs/${id}`, data);
    return res.data;
  },

  async deleteConteneur(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      storage.saveConteneurs(storage.getConteneurs().filter((c) => c.id !== id));
      return;
    }
    await api.delete(`/inventaires/conteneurs/${id}`);
  },

  async validerConteneur(id: number): Promise<InventaireConteneur> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = storage.getConteneurs();
      const idx = current.findIndex((c) => c.id === id);
      if (idx !== -1) {
        current[idx].statut = 'Valide';
        storage.saveConteneurs(current);
        return current[idx];
      }
      throw new Error('Conteneur non trouvé');
    }
    const res = await api.post<InventaireConteneur>(`/inventaires/conteneurs/${id}/valider`);
    return res.data;
  },

  async getCategories(): Promise<InventaireCategorie[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return getMockCategoriesNormalized();
    }
    const res = await api.get<InventaireCategorie[]>('/inventaires/categories');
    return res.data.map(normalizeCategorie);
  },

  async createCategorie(data: Omit<InventaireCategorie, 'id' | 'statut'>): Promise<InventaireCategorie> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockCategoriesNormalized();
      const newId = current.length ? Math.max(...current.map((c) => c.id)) + 1 : 101;
      const newItem: InventaireCategorie = normalizeCategorie({ ...data, id: newId, statut: 'Valide' });
      storage.saveCategories([...current, newItem]);
      return newItem;
    }
    const res = await api.post<InventaireCategorie>('/inventaires/categories', data);
    return normalizeCategorie(res.data);
  },

  async updateCategorie(id: number, data: Partial<InventaireCategorie>): Promise<InventaireCategorie> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockCategoriesNormalized();
      const idx = current.findIndex((c) => c.id === id);
      if (idx !== -1) {
        current[idx] = normalizeCategorie({ ...current[idx], ...data });
        storage.saveCategories(current);
        return current[idx];
      }
      throw new Error("Catégorie d'inventaire non trouvée");
    }
    const res = await api.put<InventaireCategorie>(`/inventaires/categories/${id}`, data);
    return normalizeCategorie(res.data);
  },

  async deleteCategorie(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      storage.saveCategories(getMockCategoriesNormalized().filter((c) => c.id !== id));
      return;
    }
    await api.delete(`/inventaires/categories/${id}`);
  },

  async validerCategorie(id: number): Promise<InventaireCategorie> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockCategoriesNormalized();
      const idx = current.findIndex((c) => c.id === id);
      if (idx !== -1) {
        current[idx].statut = 'Valide';
        storage.saveCategories(current);
        return current[idx];
      }
      throw new Error("Catégorie d'inventaire non trouvée");
    }
    const res = await api.post<InventaireCategorie>(`/inventaires/categories/${id}/valider`);
    return normalizeCategorie(res.data);
  },

  async getFours(): Promise<InventaireFour[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      return getMockFoursNormalized();
    }
    const res = await api.get<InventaireFour[]>('/inventaires/fours');
    return res.data.map(normalizeFour);
  },

  async createFour(data: Omit<InventaireFour, 'id' | 'inclusDansBilanFonderie' | 'visibleDansRapportInventaire'>): Promise<InventaireFour> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockFoursNormalized();
      const newId = current.length ? Math.max(...current.map((item) => item.id)) + 1 : 1;
      const newItem = normalizeFour({
        ...data,
        id: newId,
        inclusDansBilanFonderie: false,
        visibleDansRapportInventaire: true
      });
      storage.saveFours([...current, newItem]);
      return newItem;
    }
    const res = await api.post<InventaireFour>('/inventaires/fours', data);
    return normalizeFour(res.data);
  },

  async updateFour(id: number, data: Partial<InventaireFour>): Promise<InventaireFour> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockFoursNormalized();
      const idx = current.findIndex((item) => item.id === id);
      if (idx !== -1) {
        current[idx] = normalizeFour({ ...current[idx], ...data, inclusDansBilanFonderie: false, visibleDansRapportInventaire: true });
        storage.saveFours(current);
        return current[idx];
      }
      throw new Error("Inventaire four non trouvé");
    }
    const res = await api.put<InventaireFour>(`/inventaires/fours/${id}`, data);
    return normalizeFour(res.data);
  },

  async deleteFour(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      storage.saveFours(getMockFoursNormalized().filter((item) => item.id !== id));
      return;
    }
    await api.delete(`/inventaires/fours/${id}`);
  },

  async validerFour(id: number): Promise<InventaireFour> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const current = getMockFoursNormalized();
      const idx = current.findIndex((item) => item.id === id);
      if (idx !== -1) {
        current[idx].statut = 'Valide';
        storage.saveFours(current);
        return current[idx];
      }
      throw new Error("Inventaire four non trouvé");
    }
    const res = await api.post<InventaireFour>(`/inventaires/fours/${id}/valider`);
    return normalizeFour(res.data);
  },

  async getTotauxConsolides(): Promise<InventaireTotalParMatiere[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const conteneurs = storage.getConteneurs();
      const categories = getMockCategoriesNormalized();
      const fours = getMockFoursNormalized();
      const codes = ['M3010L0', 'M3000L0', 'M3020L0'];

      return codes.map((code) => {
        const totalP1 = conteneurs
          .filter((c) => c.codeTypeMatiere === code && c.statut === 'Valide')
          .reduce((sum, c) => sum + c.quantiteReelle, 0);

        const totalP2 = categories
          .filter((c) => c.codeTypeMatiere === code && c.statut === 'Valide' && c.inclusDansBilanFonderie === true)
          .reduce((sum, c) => sum + c.quantiteReelle, 0);

        const totalP3 = fours
          .filter((item) => item.codeTypeMatiere === code && item.statut === 'Valide')
          .reduce((sum, item) => sum + item.quantite, 0);

        return {
          codeTypeMatiere: code,
          totalPartie1: totalP1,
          totalPartie2: totalP2,
          totalPartie3: totalP3,
          totalInventaireRapport: totalP1 + totalP2 + totalP3,
          totalUtiliseBilan: totalP1 + totalP2,
          statut: 'Valide'
        };
      });
    }
    const res = await api.get<InventaireTotalParMatiere[]>('/inventaires/totaux');
    return res.data;
  }
};
