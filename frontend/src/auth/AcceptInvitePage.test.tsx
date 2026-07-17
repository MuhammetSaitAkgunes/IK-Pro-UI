import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { AcceptInvitePage } from "./AcceptInvitePage";
import { getSession } from "../api/session";

const authBody = {
  token: "T1",
  refreshToken: "R1",
  expiresAtUtc: "2026-07-13T12:00:00Z",
  user: { id: "u1", name: "Yeni Yönetici", email: "yeni@acme.local", role: "hr-admin", roleLabel: "İK Admin", initials: "YY", employeeId: null },
};

beforeEach(() => {
  localStorage.clear();
  vi.stubGlobal("fetch", vi.fn());
});
afterEach(() => vi.unstubAllGlobals());

const renderInvite = (query: string) =>
  render(
    <MemoryRouter initialEntries={[`/accept-invite${query}`]}>
      <AuthProvider>
        <AcceptInvitePage />
      </AuthProvider>
    </MemoryRouter>,
  );

test("token eksikse hata gösterir, şifre alanı çıkmaz", () => {
  renderInvite("");
  expect(screen.getByRole("alert")).toHaveTextContent("geçersiz veya eksik");
  expect(document.getElementById("invite-password")).toBeNull();
});

test("geçerli davette şifre belirlenir, accept-invite sonra login çağrılır ve oturum saklanır", async () => {
  vi.mocked(fetch)
    .mockResolvedValueOnce(new Response(null, { status: 204 })) // accept-invite
    .mockResolvedValueOnce(
      new Response(JSON.stringify(authBody), { status: 200, headers: { "Content-Type": "application/json" } }),
    ); // login

  renderInvite("?email=yeni%40acme.local&token=ABC-123");
  await userEvent.type(document.getElementById("invite-password")!, "yenisifre1");
  await userEvent.type(document.getElementById("invite-confirm")!, "yenisifre1");
  await userEvent.click(screen.getByRole("button"));

  expect(vi.mocked(fetch).mock.calls[0][0]).toBe("/api/auth/accept-invite");
  expect(vi.mocked(fetch).mock.calls[1][0]).toBe("/api/auth/login");
  expect(getSession()?.token).toBe("T1");
});

test("şifreler eşleşmezse istek atılmaz", async () => {
  renderInvite("?email=yeni%40acme.local&token=ABC-123");
  await userEvent.type(document.getElementById("invite-password")!, "yenisifre1");
  await userEvent.type(document.getElementById("invite-confirm")!, "baskasifre2");
  await userEvent.click(screen.getByRole("button"));

  expect(await screen.findByText("Şifreler eşleşmiyor.")).toBeInTheDocument();
  expect(vi.mocked(fetch)).not.toHaveBeenCalled();
});
