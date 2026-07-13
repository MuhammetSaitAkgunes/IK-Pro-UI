import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import { apiFetch } from "../api/client";
import { clearSession, getSession, setSession, type UserDto } from "../api/session";
import type { components } from "../api/schema";

type AuthResponse = components["schemas"]["AuthResponse"];

type AuthValue = {
  user: UserDto | null;
  login: (email: string, password: string) => Promise<UserDto>;
  register: (name: string, email: string, password: string) => Promise<UserDto>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(() => getSession()?.user ?? null);

  const accept = useCallback((auth: AuthResponse) => {
    setSession({ token: auth.token, refreshToken: auth.refreshToken, user: auth.user });
    setUser(auth.user);
    return auth.user;
  }, []);

  const login = useCallback(
    async (email: string, password: string) =>
      accept(await apiFetch<AuthResponse>("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) })),
    [accept],
  );

  const register = useCallback(
    async (name: string, email: string, password: string) =>
      accept(await apiFetch<AuthResponse>("/auth/register", { method: "POST", body: JSON.stringify({ name, email, password }) })),
    [accept],
  );

  const logout = useCallback(async () => {
    const refreshToken = getSession()?.refreshToken;
    try {
      if (refreshToken) await apiFetch("/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) });
    } catch {
      /* oturum zaten kapanıyor */
    }
    clearSession();
    setUser(null);
  }, []);

  return <AuthContext.Provider value={{ user, login, register, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValue {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth, AuthProvider içinde kullanılmalı.");
  return value;
}
