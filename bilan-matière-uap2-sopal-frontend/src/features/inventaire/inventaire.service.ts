import api from '@/services/api';
import { testDataRepository } from '@/services/testDataRepository.service';
import { calculateInventorySummary, toTypeMatiereId } from '@/services/bilanCalculation.service';
import type {
  InventaireConteneur,
  InventaireCategorie,
  InventaireFour,
  InventaireTotalParMatiere
} from './inventaire.types';
import type {
  InventaireFonderieCategorie,
  InventaireFonderieConteneur,
  InventaireFonderieFour
} from '@/mocks/sopalTestDataset';

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

const toConteneur = (item: InventaireFonderieConteneur): InventaireConteneur => {
  const ordre = testDataRepository.getDataset().x3OrdresFabrication.find((of) => of.numeroOF === item.numeroOF);
  return {
    id: item.id,
    dateInventaire: item.dateInventaire,
    mois: Number(item.dateInventaire.slice(5, 7)),
    annee: Number(item.dateInventaire.slice(0, 4)),
    numeroConteneur: item.numeroConteneur,
    ofDestination: item.numeroOF,
    codeArticle: ordre?.codeArticleParent ?? '',
    designationArticle: ordre?.designationArticle,
    typeMatiereId: toTypeMatiereId(item.codeTypeMatiere),
    codeTypeMatiere: item.codeTypeMatiere,
    quantiteReelle: item.poidsReelKg,
    unite: 'KG',
    observation: item.observation,
    statut: item.statut
  };
};

const toCategorie = (item: InventaireFonderieCategorie): InventaireCategorie =>
  normalizeCategorie({
    id: item.id,
    mois: Number(item.dateInventaire.slice(5, 7)),
    annee: Number(item.dateInventaire.slice(0, 4)),
    categorie: item.categorie,
    categorieInventaire: 'CategorieBilan',
    typeMatiereId: toTypeMatiereId(item.codeTypeMatiere),
    codeTypeMatiere: item.codeTypeMatiere,
    quantiteReelle: item.poidsReelKg,
    unite: 'KG',
    commentaire: item.commentaire,
    statut: item.statut,
    inclusDansBilanFonderie: item.inclusBilanFonderie,
    visibleDansRapportInventaire: true
  });

const toFour = (item: InventaireFonderieFour): InventaireFour =>
  normalizeFour({
    id: item.id,
    dateInventaire: item.dateInventaire,
    mois: item.mois,
    annee: item.annee,
    four: item.codeFour,
    codeTypeMatiere: item.codeTypeMatiere,
    quantite: item.quantite,
    unite: item.unite,
    observation: item.observation,
    statut: item.statut === 'Annule' ? 'Brouillon' : item.statut,
    inclusDansBilanFonderie: false,
    visibleDansRapportInventaire: true
  });

const toCentralConteneur = (
  item: Omit<InventaireConteneur, 'id' | 'statut'>,
  id: number
): InventaireFonderieConteneur => ({
  id,
  dateInventaire: item.dateInventaire,
  numeroConteneur: item.numeroConteneur,
  numeroOF: item.ofDestination ?? '',
  codeTypeMatiere: item.codeTypeMatiere as InventaireFonderieConteneur['codeTypeMatiere'],
  poidsReelKg: item.quantiteReelle,
  observation: item.observation ?? '',
  statut: 'Valide',
  creePar: 'Responsable Fonderie',
  creeLe: new Date().toISOString()
});

const toCentralCategorie = (
  item: Omit<InventaireCategorie, 'id' | 'statut'>,
  id: number
): InventaireFonderieCategorie => ({
  id,
  dateInventaire: `${item.annee}-${String(item.mois).padStart(2, '0')}-01`,
  categorie: item.categorie,
  codeTypeMatiere: item.codeTypeMatiere as InventaireFonderieCategorie['codeTypeMatiere'],
  poidsReelKg: item.quantiteReelle,
  commentaire: item.commentaire ?? '',
  inclusBilanFonderie: item.inclusDansBilanFonderie ?? true,
  statut: 'Valide',
  creePar: 'Responsable Fonderie',
  creeLe: new Date().toISOString()
});

export const inventaireService = {
  async getConteneurs(): Promise<InventaireConteneur[]> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.get<InventaireConteneur[]>('/inventaires/conteneurs')).data;
    return testDataRepository.getDataset().inventaireConteneurs.map(toConteneur);
  },

  async createConteneur(data: Omit<InventaireConteneur, 'id' | 'statut'>): Promise<InventaireConteneur> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.post<InventaireConteneur>('/inventaires/conteneurs', data)).data;
    const current = testDataRepository.getDataset().inventaireConteneurs;
    const item = toCentralConteneur(data, Math.max(0, ...current.map((row) => row.id)) + 1);
    testDataRepository.saveInventaireConteneurs([...current, item]);
    return toConteneur(item);
  },

  async updateConteneur(id: number, data: Partial<InventaireConteneur>): Promise<InventaireConteneur> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.put<InventaireConteneur>(`/inventaires/conteneurs/${id}`, data)).data;
    const dataset = testDataRepository.getDataset();
    const current = dataset.inventaireConteneurs;
    const index = current.findIndex((row) => row.id === id);
    if (index < 0) throw new Error('Conteneur non trouvé');
    current[index] = {
      ...current[index],
      ...(data.dateInventaire ? { dateInventaire: data.dateInventaire } : {}),
      ...(data.numeroConteneur ? { numeroConteneur: data.numeroConteneur } : {}),
      ...(data.ofDestination ? { numeroOF: data.ofDestination } : {}),
      ...(data.codeTypeMatiere
        ? { codeTypeMatiere: data.codeTypeMatiere as InventaireFonderieConteneur['codeTypeMatiere'] }
        : {}),
      ...(data.quantiteReelle !== undefined ? { poidsReelKg: data.quantiteReelle } : {}),
      ...(data.observation !== undefined ? { observation: data.observation } : {}),
      ...(data.statut ? { statut: data.statut } : {})
    };
    testDataRepository.saveInventaireConteneurs(current);
    return toConteneur(current[index]);
  },

  async deleteConteneur(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API === 'true') {
      await api.delete(`/inventaires/conteneurs/${id}`);
      return;
    }
    testDataRepository.saveInventaireConteneurs(
      testDataRepository.getDataset().inventaireConteneurs.filter((row) => row.id !== id)
    );
  },

  async validerConteneur(id: number): Promise<InventaireConteneur> {
    return this.updateConteneur(id, { statut: 'Valide' });
  },

  async getCategories(): Promise<InventaireCategorie[]> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.get<InventaireCategorie[]>('/inventaires/categories')).data.map(normalizeCategorie);
    return testDataRepository.getDataset().inventaireCategories.map(toCategorie);
  },

  async createCategorie(data: Omit<InventaireCategorie, 'id' | 'statut'>): Promise<InventaireCategorie> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return normalizeCategorie((await api.post<InventaireCategorie>('/inventaires/categories', data)).data);
    const current = testDataRepository.getDataset().inventaireCategories;
    const item = toCentralCategorie(data, Math.max(100, ...current.map((row) => row.id)) + 1);
    testDataRepository.saveInventaireCategories([...current, item]);
    return toCategorie(item);
  },

  async updateCategorie(id: number, data: Partial<InventaireCategorie>): Promise<InventaireCategorie> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return normalizeCategorie((await api.put<InventaireCategorie>(`/inventaires/categories/${id}`, data)).data);
    const current = testDataRepository.getDataset().inventaireCategories;
    const index = current.findIndex((row) => row.id === id);
    if (index < 0) throw new Error('Catégorie d’inventaire non trouvée');
    current[index] = {
      ...current[index],
      ...(data.quantiteReelle !== undefined ? { poidsReelKg: data.quantiteReelle } : {}),
      ...(data.commentaire !== undefined ? { commentaire: data.commentaire } : {}),
      ...(data.statut ? { statut: data.statut } : {}),
      ...(data.inclusDansBilanFonderie !== undefined ? { inclusBilanFonderie: data.inclusDansBilanFonderie } : {})
    };
    testDataRepository.saveInventaireCategories(current);
    return toCategorie(current[index]);
  },

  async deleteCategorie(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API === 'true') {
      await api.delete(`/inventaires/categories/${id}`);
      return;
    }
    testDataRepository.saveInventaireCategories(
      testDataRepository.getDataset().inventaireCategories.filter((row) => row.id !== id)
    );
  },

  async validerCategorie(id: number): Promise<InventaireCategorie> {
    return this.updateCategorie(id, { statut: 'Valide' });
  },

  async getFours(): Promise<InventaireFour[]> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.get<InventaireFour[]>('/inventaires/fours')).data.map(normalizeFour);
    return testDataRepository.getDataset().inventaireFours.map(toFour);
  },

  async createFour(
    data: Omit<InventaireFour, 'id' | 'inclusDansBilanFonderie' | 'visibleDansRapportInventaire'>
  ): Promise<InventaireFour> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return normalizeFour((await api.post<InventaireFour>('/inventaires/fours', data)).data);
    const current = testDataRepository.getDataset().inventaireFours;
    const item: InventaireFonderieFour = {
      id: Math.max(200, ...current.map((row) => row.id)) + 1,
      ...data,
      codeFour: data.four,
      codeTypeMatiere: data.codeTypeMatiere as InventaireFonderieFour['codeTypeMatiere'],
      unite: 'KG',
      observation: data.observation ?? '',
      inclusBilanFonderie: false,
      visibleRapportInventaire: true
    };
    testDataRepository.saveInventaireFours([...current, item]);
    return toFour(item);
  },

  async updateFour(id: number, data: Partial<InventaireFour>): Promise<InventaireFour> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return normalizeFour((await api.put<InventaireFour>(`/inventaires/fours/${id}`, data)).data);
    const current = testDataRepository.getDataset().inventaireFours;
    const index = current.findIndex((row) => row.id === id);
    if (index < 0) throw new Error('Inventaire four non trouvé');
    current[index] = {
      ...current[index],
      ...(data.dateInventaire ? { dateInventaire: data.dateInventaire } : {}),
      ...(data.four ? { codeFour: data.four } : {}),
      ...(data.codeTypeMatiere
        ? { codeTypeMatiere: data.codeTypeMatiere as InventaireFonderieFour['codeTypeMatiere'] }
        : {}),
      ...(data.quantite !== undefined ? { quantite: data.quantite } : {}),
      ...(data.observation !== undefined ? { observation: data.observation } : {}),
      ...(data.statut ? { statut: data.statut === 'Annule' ? 'Annule' : data.statut } : {})
    };
    testDataRepository.saveInventaireFours(current);
    return toFour(current[index]);
  },

  async deleteFour(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API === 'true') {
      await api.delete(`/inventaires/fours/${id}`);
      return;
    }
    testDataRepository.saveInventaireFours(
      testDataRepository.getDataset().inventaireFours.filter((row) => row.id !== id)
    );
  },

  async validerFour(id: number): Promise<InventaireFour> {
    return this.updateFour(id, { statut: 'Valide' });
  },

  async getTotauxConsolides(): Promise<InventaireTotalParMatiere[]> {
    if (import.meta.env.VITE_USE_REAL_API === 'true')
      return (await api.get<InventaireTotalParMatiere[]>('/inventaires/totaux')).data;
    return calculateInventorySummary(testDataRepository.getDataset()).map((row) => ({
      ...row,
      statut: 'Valide' as const
    }));
  }
};
