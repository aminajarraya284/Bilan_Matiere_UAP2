import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import router from '@/router';
import { AUTH_TOKEN_STORAGE_KEY, expireAuthSession } from '@/features/auth/auth.session';

// Keep the fallback aligned with .env.example and the documented ASP.NET Core URL.
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

let sessionExpirationPromise: Promise<void> | null = null;

export const expireSessionAndRedirectToLogin = (): Promise<void> => {
  if (!sessionExpirationPromise) {
    // Keep Pinia and localStorage in sync before navigation guards run.
    expireAuthSession();
    sessionExpirationPromise = (async () => {
      if (router.currentRoute.value.name !== 'Login') {
        await router.replace({
          name: 'Login',
          query: { error: 'session_expired' }
        });
      }
    })().finally(() => {
      sessionExpirationPromise = null;
    });
  }

  return sessionExpirationPromise;
};

// Request Interceptor: Attach JWT Token if present
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: 401 et 403 sont traites separement.
//  - 401 : le token est absent, invalide ou expire -> la session n'est plus exploitable,
//          on la purge et on renvoie vers /login.
//  - 403 : le token est valide mais le role est insuffisant -> la session reste valide.
//          On ne deconnecte pas l'utilisateur ; l'appelant decide de l'affichage.
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const isLoginRequest = error.config?.url?.includes('/auth/login') === true;

    if (status === 401 && !isLoginRequest) {
      await expireSessionAndRedirectToLogin();
    } else if (status === 403) {
      console.warn(`Accès refusé (403) sur ${error.config?.url ?? 'une ressource'} : permissions insuffisantes.`);
    }

    return Promise.reject(error);
  }
);

export default api;
