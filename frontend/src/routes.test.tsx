import { render, screen } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { beforeEach, expect, test } from "vitest";
import { buildRouteObjects } from "./routes";
import { AuthProvider } from "./auth/AuthContext";
import { SESSION_KEY } from "./api/session";

const sessionFor = (role: string, name: string) =>
  JSON.stringify({ token: "T", refreshToken: "R", user: { id: "u", name, email: "x@x", role, roleLabel: name, initials: "XX", employeeId: null } });

const renderAt = (path: string) => {
  const router = createMemoryRouter(buildRouteObjects(), { initialEntries: [path] });
  return render(
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>,
  );
};

beforeEach(() => localStorage.clear());

test("oturum yokken korumalı rota login'e yönlenir", () => {
  renderAt("/dashboard");
  // Not: "Giriş yap" hem sekme butonunda hem submit butonunda geçiyor (App.test.tsx'teki
  // getAllByText paritesiyle aynı gerekçe) — bu yüzden getAllByRole kullanılıyor.
  expect(screen.getAllByRole("button", { name: /Giriş yap/ }).length).toBeGreaterThan(0);
});

test("employee, dashboard'da 'Yetki Gerekli' ekranı görür (redirect yok)", () => {
  localStorage.setItem(SESSION_KEY, sessionFor("employee", "Ahmet Yılmaz"));
  renderAt("/dashboard");
  expect(screen.getByText("Bu alan için yetki gerekli")).toBeInTheDocument();
});

test("hr-admin, settings placeholder'ını görür", () => {
  localStorage.setItem(SESSION_KEY, sessionFor("hr-admin", "İK Yöneticisi"));
  renderAt("/settings");
  expect(screen.getByText("Bu alan hazırlanıyor")).toBeInTheDocument();
});
