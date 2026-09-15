import api from '@/services/api';
import { assertApiSource } from '@/config/dataSources';
import type {
  AdminDevelopmentAccount,
  AdminMaterialType,
  AdminParameter,
  AdminUserProfile,
  AdminUsersQuery,
  PagedAdminUsers
} from './admin.types';

export const adminService = {
  async getParameters(): Promise<AdminParameter[]> {
    assertApiSource('administration');
    return (await api.get<AdminParameter[]>('/v1/parametres')).data;
  },

  async updateParameter(cle: string, valeur: string): Promise<AdminParameter> {
    assertApiSource('administration');
    return (
      await api.put<AdminParameter>('/v1/parametres/' + encodeURIComponent(cle), { valeur })
    ).data;
  },

  async getMaterialTypes(): Promise<AdminMaterialType[]> {
    assertApiSource('typesMatiere');
    return (await api.get<AdminMaterialType[]>('/v1/types-matiere')).data;
  },

  /** Profils Entra provisionnés automatiquement. Pagination serveur. */
  async getUsers(query: AdminUsersQuery = {}): Promise<PagedAdminUsers> {
    assertApiSource('administration');
    return (
      await api.get<PagedAdminUsers>('/v1/admin/utilisateurs', {
        params: {
          page: query.page ?? 1,
          pageSize: query.pageSize ?? 25,
          ...(query.recherche ? { recherche: query.recherche } : {}),
          ...(query.estActif === undefined ? {} : { estActif: query.estActif })
        }
      })
    ).data;
  },

  /** Seule mutation disponible : activer ou couper l'accès local. */
  async setUserActivation(id: number, estActif: boolean): Promise<AdminUserProfile> {
    assertApiSource('administration');
    return (
      await api.put<AdminUserProfile>(`/v1/admin/utilisateurs/${id}/activation`, { estActif })
    ).data;
  },

  /**
   * Comptes de connexion locale. Répond 404 hors Development : l'appelant traite
   * ce cas comme « aucun compte de développement », pas comme une erreur.
   */
  async getDevelopmentAccounts(): Promise<AdminDevelopmentAccount[]> {
    assertApiSource('administration');
    return (
      await api.get<AdminDevelopmentAccount[]>('/v1/admin/utilisateurs/developpement')
    ).data;
  }
};
