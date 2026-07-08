import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

// Base URL configured for future ASP.NET Core API (default to localhost:7001)
const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7001/api';

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
    if (error.response && error.response.status === 401) {
      // Clear authentication credentials and redirect to login
      localStorage.removeItem('sopal_auth_token');
      localStorage.removeItem('sopal_auth_user');

      // Lazy load router to avoid initialization issues in test environment
      const { default: router } = await import('@/router');
      router.push({
        name: 'Login',
        query: { error: 'session_expired' }
      });
    }
    return Promise.reject(error);
  }
);

export default api;
