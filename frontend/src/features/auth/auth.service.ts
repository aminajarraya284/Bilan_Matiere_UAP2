import api from '@/services/api';
import { AuthUser, LoginResponse } from './auth.types';
import { isMockSource } from '@/config/dataSources';

type ApiErrorResponse = {
  response?: {
    status?: number;
    data?: {
      // ProblemDetails (RFC 7807) renvoye par le backend...
      title?: string;
      detail?: string;
      // ...ou forme simplifiee historique.
      message?: string;
    };
  };
};

export const INVALID_CREDENTIALS_MESSAGE = 'Identifiants incorrects';
export const FORBIDDEN_MESSAGE = "Votre compte n'a pas les permissions requises.";
export const SERVER_UNREACHABLE_MESSAGE = 'Erreur lors de la connexion au serveur.';

/**
 * Traduit une erreur d'appel /auth/login en message utilisateur.
 * Aucun repli sur les comptes mock n'est effectue : en mode API reelle, un echec
 * reste un echec.
 */
const toLoginErrorMessage = (error: unknown): string => {
  const apiError = error as ApiErrorResponse;
  const status = apiError.response?.status;

  // 401 et 403 sont deux situations distinctes : identifiants faux vs droits insuffisants.
  if (status === 401) return INVALID_CREDENTIALS_MESSAGE;
  if (status === 403) return FORBIDDEN_MESSAGE;

  const data = apiError.response?.data;
  return data?.detail || data?.message || data?.title || SERVER_UNREACHABLE_MESSAGE;
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
    // Mode mock : comptes de demonstration locaux, aucun appel reseau.
    if (isMockSource('auth')) {
      return new Promise((resolve, reject) => {
        setTimeout(() => {
          const found = MOCK_USERS.find((u) => u.username === username);
          if (found && password === username) {
            resolve({ token: 'demo_jwt_token_sopal_' + found.role, user: found });
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

    // Mode API reelle : POST /api/auth/login (connexion locale Development cote backend).
    // En cas d'echec on propage une erreur — jamais de bascule silencieuse vers MOCK_USERS.
    try {
      const response = await api.post<LoginResponse>('/auth/login', { username, password });
      return response.data;
    } catch (error: unknown) {
      throw new Error(toLoginErrorMessage(error));
    }
  }
};
