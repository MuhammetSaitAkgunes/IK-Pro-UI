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
  const loginForm = document.getElementById("auth-login")!;
  expect(loginForm.querySelector("input#login-email")).toHaveValue("ik@hrmaster.local");
  expect(loginForm.querySelector("input#login-password")).toHaveValue("demo123");
  expect(loginForm.querySelector("button[type='submit']")).toHaveTextContent("Giriş yap");
});

test("başarılı girişte oturum saklanır", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify(authBody), { status: 200, headers: { "Content-Type": "application/json" } }),
  );
  renderLogin();
  const submitButton = document.querySelector("form#auth-login button[type='submit']")!;
  await userEvent.click(submitButton);
  expect(getSession()?.token).toBe("T1");
  expect(vi.mocked(fetch).mock.calls[0][0]).toBe("/api/auth/login");
});

test("başarısız girişte hata mesajı görünür, oturum yazılmaz", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify({ title: "E-posta veya şifre hatalı.", status: 401 }), { status: 401 }),
  );
  renderLogin();
  const submitButton = document.querySelector("form#auth-login button[type='submit']")!;
  await userEvent.click(submitButton);
  expect(await screen.findByText("E-posta veya şifre hatalı.")).toBeInTheDocument();
  expect(getSession()).toBeNull();
});

test("sekmeler ve iki form da DOM'da (parite)", () => {
  renderLogin();
  const signupTabButton = document.querySelector("div.auth-tabs button:nth-child(2)")!;
  expect(signupTabButton).toHaveClass("auth-tab");
  expect(document.getElementById("auth-login")).toHaveClass("auth-form", "active");
  expect(document.getElementById("auth-signup")).toHaveClass("auth-form");
  expect(document.getElementById("auth-signup")).not.toHaveClass("active");
});
