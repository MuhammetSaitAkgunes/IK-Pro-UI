import { render, screen } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, expect, test, vi } from "vitest";
import { buildRouteObjects } from "../routes";
import { AuthProvider } from "../auth/AuthContext";
import { ToastProvider } from "./ToastProvider";
import { SESSION_KEY } from "../api/session";

const renderShellAt = (path: string, role: string, name: string) => {
  localStorage.setItem(
    SESSION_KEY,
    JSON.stringify({ token: "T", refreshToken: "R", user: { id: "u", name, email: "x@x", role, roleLabel: name, initials: "XX", employeeId: null } }),
  );
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ openCount: 5 }), { status: 200, headers: { "Content-Type": "application/json" } }),
  ));
  const router = createMemoryRouter(buildRouteObjects(), { initialEntries: [path] });
  return render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <AuthProvider>
        <ToastProvider>
          <RouterProvider router={router} />
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );
};

beforeEach(() => localStorage.clear());

test("hr-admin tüm menü gruplarını görür, başlık rotadan gelir", async () => {
  renderShellAt("/dashboard", "hr-admin", "İK Yöneticisi");
  expect(await screen.findByText("Risk Merkezi", { selector: ".header-title" })).toBeInTheDocument();
  expect(screen.getByText("Ayarlar")).toBeInTheDocument();
  expect(await screen.findByText("5", { selector: ".header-action-button span" })).toBeInTheDocument();
});

test("employee menüsünde yalnız yetkili modüller görünür", () => {
  renderShellAt("/overview", "employee", "Ahmet Yılmaz");
  expect(screen.queryByText("Ayarlar")).not.toBeInTheDocument();
  expect(screen.queryByText("Personel Yönetimi")).not.toBeInTheDocument();
  expect(screen.getByText("İzinlerim")).toBeInTheDocument();
});
