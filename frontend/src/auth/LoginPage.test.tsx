import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";
import { getSession } from "../api/session";

const authBody = {
  token: "T1",
  refreshToken: "R1",
  expiresAtUtc: "2026-07-13T12:00:00Z",
  user: { id: "u1", name: "İK Yöneticisi", email: "ik@hrmaster.local", role: "hr-admin", roleLabel: "İK Admin", initials: "İK", employeeId: null },
};

beforeEach(() => {
  localStorage.clear();
  vi.stubGlobal("fetch", vi.fn());
});
afterEach(() => vi.unstubAllGlobals());

const renderLogin = () =>
  render(
    <MemoryRouter>
      <AuthProvider>
        <LoginPage mode="login" />
      </AuthProvider>
    </MemoryRouter>,
  );

test("login formu öndolu demo bilgileriyle render edilir (parite)", () => {
  renderLogin();
  expect(screen.getByLabelText("E-posta")).toHaveValue("ik@hrmaster.local");
  expect(screen.getByLabelText("Şifre")).toHaveValue("demo123");
  expect(screen.getByRole("button", { name: /Giriş yap/ })).toBeInTheDocument();
});

test("başarılı girişte oturum saklanır", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify(authBody), { status: 200, headers: { "Content-Type": "application/json" } }),
  );
  renderLogin();
  await userEvent.click(screen.getByRole("button", { name: /Giriş yap/ }));
  expect(getSession()?.token).toBe("T1");
  expect(vi.mocked(fetch).mock.calls[0][0]).toBe("/api/auth/login");
});

test("başarısız girişte hata mesajı görünür, oturum yazılmaz", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify({ title: "E-posta veya şifre hatalı.", status: 401 }), { status: 401 }),
  );
  renderLogin();
  await userEvent.click(screen.getByRole("button", { name: /Giriş yap/ }));
  expect(await screen.findByText("E-posta veya şifre hatalı.")).toBeInTheDocument();
  expect(getSession()).toBeNull();
});
