import http from '@/lib/http';
import { User, AuthResponse } from '@/types';

export interface LoginParams {
  email: string;
  password: string;
}

export interface RegisterParams {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string;
}

export interface ForgotPasswordParams {
  email: string;
}

export interface ResetPasswordParams {
  email: string;
  token: string;
  newPassword: string;
}

export interface UserProfileDto {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  roles: string[];
  createdAt: string;
}

export const authApi = {
  login: async (params: LoginParams): Promise<AuthResponse> => {
    const res = await http.post<AuthResponse>('/auth/login', params);
    return res.data;
  },

  register: async (params: RegisterParams): Promise<{ id: string; email: string; fullName: string }> => {
    const res = await http.post<{ id: string; email: string; fullName: string }>('/auth/register', params);
    return res.data;
  },

  refresh: async (): Promise<AuthResponse> => {
    const res = await http.post<AuthResponse>('/auth/refresh', {});
    return res.data;
  },

  logout: async (): Promise<{ message: string }> => {
    const res = await http.post<{ message: string }>('/auth/logout');
    return res.data;
  },

  forgotPassword: async (params: ForgotPasswordParams): Promise<{ message?: string }> => {
    const res = await http.post<{ message?: string }>('/auth/forgot-password', params);
    return res.data;
  },

  resetPassword: async (params: ResetPasswordParams): Promise<{ message?: string }> => {
    const res = await http.post<{ message?: string }>('/auth/reset-password', params);
    return res.data;
  },

  getProfile: async (): Promise<User> => {
    const res = await http.get<UserProfileDto>('/me');
    const data = res.data;
    const role: 'Admin' | 'Customer' = data.roles && data.roles.includes('Admin') ? 'Admin' : 'Customer';
    return {
      id: data.id,
      email: data.email,
      fullName: data.fullName,
      phoneNumber: data.phoneNumber,
      role,
    };
  },

  updateProfile: async (params: { fullName: string; phoneNumber?: string }): Promise<User> => {
    const res = await http.put<UserProfileDto>('/me', params);
    const data = res.data;
    const role: 'Admin' | 'Customer' = data.roles && data.roles.includes('Admin') ? 'Admin' : 'Customer';
    return {
      id: data.id,
      email: data.email,
      fullName: data.fullName,
      phoneNumber: data.phoneNumber,
      role,
    };
  },

  changePassword: async (params: { currentPassword: string; newPassword: string }): Promise<{ message: string }> => {
    const res = await http.post<{ message: string }>('/me/change-password', params);
    return res.data;
  },
};
