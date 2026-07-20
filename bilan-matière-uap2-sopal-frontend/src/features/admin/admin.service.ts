import api from '@/services/api';
import { BUSINESS_RULES } from '@/config/businessRules';
import { AdminUser, AdminArticle, AdminSettings } from './admin.types';

const mockUsers: AdminUser[] = [
  { id: 1, fullName: 'Amina Jarraya', role: 'Admin', email: 'amina.jarraya@sopal.com', actif: true },
  { id: 2, fullName: 'Ahmed Trabelsi', role: 'ResponsableFonderie', email: 'ahmed.t@sopal.com', actif: true },
  { id: 3, fullName: 'Khalil Ben Ali', role: 'ResponsableUsinage2', email: 'khalil.ba@sopal.com', actif: true }
];

const mockArticles: AdminArticle[] = [
  {
    id: 1,
    code: '0101A03-1',
    designation: 'CORPS MINI VANNE BS 1/2',
    typeMatiere: 'M3010L0',
    unite: 'KG',
    actif: true
  },
  {
    id: 2,
    code: '0102A03-1',
    designation: 'CORPS MINI VANNE BS 1/2 MF',
    typeMatiere: 'M3000L0',
    unite: 'KG',
    actif: true
  },
  { id: 3, code: '0203203-1', designation: 'CORPS MINI VANNE', typeMatiere: 'M3060L0', unite: 'KG', actif: true }
];

let mockSettings: AdminSettings = {
  seuilAlerteEcart: BUSINESS_RULES.seuilEcartPourcentage,
  seuilCritiqueEcart: BUSINESS_RULES.seuilCritiqueEcartPourcentage,
  toleranceAlertePoids: BUSINESS_RULES.seuilAlertePoidsKg,
  frequenceSyncX3: 'Toutes les 4 heures'
};

export const adminService = {
  // --- USERS ---
  async getUsers(): Promise<AdminUser[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') return [...mockUsers];
    const res = await api.get<AdminUser[]>('/users');
    return res.data;
  },

  async createUser(data: Omit<AdminUser, 'id'>): Promise<AdminUser> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const newId = Math.max(...mockUsers.map((u) => u.id), 0) + 1;
      const newUser = { ...data, id: newId };
      mockUsers.push(newUser);
      return newUser;
    }
    const res = await api.post<AdminUser>('/users', data);
    return res.data;
  },

  async toggleUserStatus(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const u = mockUsers.find((user) => user.id === id);
      if (u) u.actif = !u.actif;
      return;
    }
    await api.put(`/users/${id}/toggle-status`);
  },

  // --- ARTICLES ---
  async getArticles(): Promise<AdminArticle[]> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') return [...mockArticles];
    const res = await api.get<AdminArticle[]>('/articles');
    return res.data;
  },

  async createArticle(data: Omit<AdminArticle, 'id'>): Promise<AdminArticle> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const newId = Math.max(...mockArticles.map((a) => a.id), 0) + 1;
      const newArt = { ...data, id: newId };
      mockArticles.push(newArt);
      return newArt;
    }
    const res = await api.post<AdminArticle>('/articles', data);
    return res.data;
  },

  async toggleArticleStatus(id: number): Promise<void> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      const a = mockArticles.find((art) => art.id === id);
      if (a) a.actif = !a.actif;
      return;
    }
    await api.put(`/articles/${id}/toggle-status`);
  },

  // --- SETTINGS ---
  async getSettings(): Promise<AdminSettings> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') return { ...mockSettings };
    const res = await api.get<AdminSettings>('/settings');
    return res.data;
  },

  async updateSettings(data: AdminSettings): Promise<AdminSettings> {
    if (import.meta.env.VITE_USE_REAL_API !== 'true') {
      mockSettings = { ...data };
      return mockSettings;
    }
    const res = await api.put<AdminSettings>('/settings', data);
    return res.data;
  }
};
