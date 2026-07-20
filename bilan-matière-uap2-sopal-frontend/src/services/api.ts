import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import router from '@/router';

// Keep the fallback aligned with .env.example and the documented ASP.NET Core URL.
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Request Interceptor: Attach JWT Token if present
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('sopal_auth_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: Handle 401 Unauthorized errors
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const isLoginRequest = error.config?.url?.includes('/auth/login') === true;
    if (error.response?.status === 401 && !isLoginRequest) {
      // Clear authentication credentials and redirect to login
      localStorage.removeItem('sopal_auth_token');
      localStorage.removeItem('sopal_auth_user');

      if (router.currentRoute.value.name !== 'Login') {
        await router.push({
          name: 'Login',
          query: { error: 'session_expired' }
        });
      }
    }
    return Promise.reject(error);
  }
);

export default api;
