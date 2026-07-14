import { vi } from "vitest";

/** Anahtar tam path'tir ("/api/dashboard/metrics"); query string yok sayılır. */
export function stubApi(routes: Record<string, unknown>): void {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL) => {
      const path = String(input).split("?")[0];
      if (!(path in routes)) {
        return new Response(JSON.stringify({ title: `Stub tanımlı değil: ${path}` }), { status: 404 });
      }
      return new Response(JSON.stringify(routes[path]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }),
  );
}
