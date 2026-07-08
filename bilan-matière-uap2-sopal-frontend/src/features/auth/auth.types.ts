import { UserRole } from '@/types/common.types';

export interface AuthUser {
  id: number;
  fullName: string;
  username: string;
  email: string;
  role: UserRole;
  status: 'Actif' | 'Inactif';
}

export interface LoginResponse {
  token: string;
  user: AuthUser;
}
