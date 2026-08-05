import { createContext, useCallback, useContext, useSyncExternalStore, type ReactNode } from "react";
import { apiFetch, toSession, type AuthResponse } from "../api/client";
import { clearSession, getSession, setSession, subscribeSession, type UserDto } from "../api/session";

type AuthValue = {
  user: UserDto | null;
  login: (email: string, password: string) => Promise<UserDto>;
  register: (name: string, email: string, password: string) => Promise<UserDto>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Kaynak localStorage oturumu: api/client.ts 401'de oturumu sildiğinde
  // (React dışından) bu abonelik sayesinde user anında null'a düşer.
  const user = useSyncExternalStore(subscribeSession, getSession)?.user ?? null;

  const accept = useCallback((auth: AuthResponse) => {
    const session = toSession(auth);
    setSession(session);
    return session.user;
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
  }, []);

  return <AuthContext.Provider value={{ user, login, register, logout }}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValue {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth, AuthProvider içinde kullanılmalı.");
  return value;
}
