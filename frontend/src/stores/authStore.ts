import { create } from 'zustand';
import { User } from '@/types';
import { authApi, LoginParams, RegisterParams } from '@/features/auth/api';
import { queryClient } from '@/lib/queryClient';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  setAuth: (user: User, token: string) => void;
  setToken: (token: string | null) => void;
  setUser: (user: User | null) => void;
  login: (params: LoginParams) => Promise<User>;
  register: (params: RegisterParams) => Promise<{ id: string; email: string; fullName: string }>;
  logout: () => Promise<void>;
  fetchProfile: () => Promise<User | null>;
  checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  token: null,
  isAuthenticated: false,
  isLoading: false,

  setAuth: (user, token) =>
    set({
      user,
      token,
      isAuthenticated: true,
      isLoading: false,
    }),

  setToken: (token) =>
    set((state) => ({
      token,
      isAuthenticated: !!token || !!state.user,
    })),

  setUser: (user) =>
    set((state) => ({
      user,
      isAuthenticated: !!user || !!state.token,
    })),

  login: async (params) => {
    set({ isLoading: true });
    try {
      const authResult = await authApi.login(params);
      set({ token: authResult.accessToken });
      const profile = await authApi.getProfile();
      set({
        user: profile,
        isAuthenticated: true,
        isLoading: false,
      });
      return profile;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  register: async (params) => {
    set({ isLoading: true });
    try {
      const result = await authApi.register(params);
      set({ isLoading: false });
      return result;
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  logout: async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore network / token error on logout
    } finally {
      set({
        user: null,
        token: null,
        isAuthenticated: false,
        isLoading: false,
      });
      queryClient.clear();
    }
  },

  fetchProfile: async () => {
    try {
      const user = await authApi.getProfile();
      set({ user, isAuthenticated: true });
      return user;
    } catch {
      return null;
    }
  },

  checkAuth: async () => {
    if (get().token) {
      await get().fetchProfile();
      return;
    }

    set({ isLoading: true });
    try {
      const refreshResult = await authApi.refresh();
      set({ token: refreshResult.accessToken });
      const user = await authApi.getProfile();
      set({ user, isAuthenticated: true, isLoading: false });
    } catch {
      set({
        user: null,
        token: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  },
}));
