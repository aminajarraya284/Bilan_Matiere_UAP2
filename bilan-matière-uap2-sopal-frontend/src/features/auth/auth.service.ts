import api from '@/services/api';
import { AuthUser, LoginResponse } from './auth.types';

type ApiErrorResponse = {
  response?: {
    data?: {
      message?: string;
    };
  };
};

// Mock users for local demonstration only (when backend is not connected)
export const MOCK_USERS: AuthUser[] = [
  {
    id: 1,
    fullName: 'Administrateur SOPAL',
    username: 'admin',
    email: 'admin@sopal.com',
    role: 'Admin',
    status: 'Actif'
  },
  {
    id: 2,
    fullName: 'Responsable Fonderie',
    username: 'fonderie',
    email: 'fonderie@sopal.com',
    role: 'ResponsableFonderie',
    status: 'Actif'
  },
  {
    id: 3,
    fullName: 'Responsable Usinage 2',
    username: 'usinage2',
    email: 'usinage2@sopal.com',
    role: 'ResponsableUsinage2',
    status: 'Actif'
  }
];

export const authService = {
  async login(username: string, password: string): Promise<LoginResponse> {
    try {
      // Mode développement uniquement — à remplacer par l'appel réel ASP.NET Core
      // POST /api/auth/login
      if (import.meta.env.VITE_USE_REAL_API !== 'true') {
        // Mock authorization flow
        return new Promise((resolve, reject) => {
          setTimeout(() => {
            const found = MOCK_USERS.find((u) => u.username === username);
            if (found && password === username) {
              resolve({
                token: 'demo_jwt_token_sopal_' + found.role,
                user: found
              });
            } else {
              reject(
                new Error(
                  'Identifiants de démonstration invalides (utilisez username: "admin", "fonderie" ou "usinage2" avec le mot de passe identique à l\'identifiant)'
                )
              );
            }
          }, 600);
        });
      }

      const response = await api.post<LoginResponse>('/auth/login', { username, password });
      return response.data;
    } catch (error: unknown) {
      const apiError = error as ApiErrorResponse;
      throw new Error(apiError.response?.data?.message || 'Erreur lors de la connexion au serveur.');
    }
  }
};
