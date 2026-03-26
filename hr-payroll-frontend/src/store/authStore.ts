import { create } from 'zustand';
import type { LoginResponse } from '../types';

interface AuthState {
  token: string | null;
  user: { username: string; role: string } | null;
  isAuthenticated: boolean;
  login: (response: LoginResponse) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: localStorage.getItem('token'),
  user: (() => {
    try {
      const u = localStorage.getItem('user');
      return u ? JSON.parse(u) : null;
    } catch { return null; }
  })(),
  isAuthenticated: !!localStorage.getItem('token'),

  login: (response) => {
    localStorage.setItem('token', response.token);
    const user = { username: response.username, role: response.role };
    localStorage.setItem('user', JSON.stringify(user));
    set({ token: response.token, user, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    set({ token: null, user: null, isAuthenticated: false });
  },
}));
