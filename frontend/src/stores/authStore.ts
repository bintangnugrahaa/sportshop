import { create } from 'zustand';
import { User } from '@/types';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  setAuth: (user: User, token: string) => void;
  setToken: (token: string | null) => void;
  setUser: (user: User | null) => void;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
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
  logout: async () => {
    set({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
    });
  },
}));
