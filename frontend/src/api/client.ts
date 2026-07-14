import type { components } from "./schema";
import { clearSession, getSession, setSession, type Session } from "./session";

type ProblemDetails = { title?: string; status?: number; errors?: Record<string, string[]> };
export type AuthResponse = components["schemas"]["AuthResponse"];

export class ApiError extends Error {
  status: number;
  problem?: ProblemDetails;
  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.status = status;
    this.problem = problem;
  }
}

export const toSession = (auth: AuthResponse): Session => {
  if (!auth.token || !auth.refreshToken || !auth.user) {
    throw new ApiError(500, "Kimlik yanıtı eksik alan içeriyor.");
  }
  return { token: auth.token, refreshToken: auth.refreshToken, user: auth.user };
};

const API_BASE = "/api";
let refreshInFlight: Promise<boolean> | null = null;

const rawFetch = async (path: string, init: RequestInit = {}): Promise<Response> => {
  const headers = new Headers(init.headers);
  if (!headers.has("Content-Type") && init.body && !(init.body instanceof FormData))
    headers.set("Content-Type", "application/json");
  const token = getSession()?.token;
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return fetch(`${API_BASE}${path}`, { ...init, headers });
};

// Tek-uçuş refresh: eşzamanlı 401'ler aynı refresh isteğini paylaşır.
const tryRefresh = (): Promise<boolean> =>
  (refreshInFlight ??= (async () => {
    try {
      const session = getSession();
      if (!session?.refreshToken) return false;
      const response = await fetch(`${API_BASE}/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: session.refreshToken }),
      });
      if (!response.ok) return false;
      const auth = (await response.json()) as AuthResponse;
      setSession(toSession(auth));
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })());

const toError = async (response: Response): Promise<ApiError> => {
  let problem: ProblemDetails | undefined;
  try {
    problem = await response.json();
  } catch {
    /* gövdesiz hata */
  }
  return new ApiError(response.status, problem?.title || `İstek başarısız (${response.status})`, problem);
};

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response = await rawFetch(path, init);

  if (response.status === 401 && !path.startsWith("/auth/")) {
    if (await tryRefresh()) {
      response = await rawFetch(path, init);
    } else {
      clearSession();
      window.location.hash = "/login";
    }
  }

  if (!response.ok) throw await toError(response);
  if (response.status === 204) return null as T;
  return (await response.json()) as T;
}

const fileNameFrom = (disposition: string | null): string | null => {
  if (!disposition) return null;
  const utf8 = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8) return decodeURIComponent(utf8[1]);
  const plain = disposition.match(/filename="?([^";]+)"?/i);
  return plain ? plain[1] : null;
};

/** İkili indirme (evrak/pusula): Bearer + 401-refresh apiFetch ile aynı. */
export async function apiDownload(path: string): Promise<{ blob: Blob; fileName: string | null }> {
  let response = await rawFetch(path);

  if (response.status === 401 && !path.startsWith("/auth/")) {
    if (await tryRefresh()) {
      response = await rawFetch(path);
    } else {
      clearSession();
      window.location.hash = "/login";
    }
  }

  if (!response.ok) throw await toError(response);
  return { blob: await response.blob(), fileName: fileNameFrom(response.headers.get("Content-Disposition")) };
}
