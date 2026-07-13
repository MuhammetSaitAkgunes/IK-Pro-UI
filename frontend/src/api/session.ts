import type { components } from "./schema";

export type UserDto = components["schemas"]["UserDto"];
export type Session = { token: string; refreshToken: string; user: UserDto };

export const SESSION_KEY = "ikpro-session";

export const getSession = (): Session | null => {
  try {
    return JSON.parse(localStorage.getItem(SESSION_KEY) || "null");
  } catch {
    return null;
  }
};

export const setSession = (session: Session) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));

export const clearSession = () => localStorage.removeItem(SESSION_KEY);
