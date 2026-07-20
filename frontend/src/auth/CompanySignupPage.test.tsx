import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { CompanySignupPage } from "./CompanySignupPage";

beforeEach(() => {
  localStorage.clear();
  vi.stubGlobal("fetch", vi.fn());
});
afterEach(() => vi.unstubAllGlobals());

const renderPage = () =>
  render(
    <MemoryRouter>
      <CompanySignupPage />
    </MemoryRouter>,
  );

const fillForm = async () => {
  await userEvent.type(document.getElementById("company-name")!, "Acme A.Ş.");
  await userEvent.type(document.getElementById("admin-name")!, "Kurucu");
  await userEvent.type(document.getElementById("admin-email")!, "kurucu@acme.local");
};

test("başarılı kayıtta signup çağrılır ve doğrulama ekranı görünür", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify({ slug: "acme", adminEmail: "kurucu@acme.local" }),
      { status: 201, headers: { "Content-Type": "application/json" } }),
  );
  renderPage();
  await fillForm();
  await userEvent.click(screen.getByRole("button", { name: /kaydol/i }));

  expect(vi.mocked(fetch).mock.calls[0][0]).toBe("/api/tenants/signup");
  expect(await screen.findByText(/doğrulama e-postası/i)).toBeInTheDocument();
});

test("çakışan e-postada hata mesajı görünür", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response(JSON.stringify({ title: "'kurucu@acme.local' e-postasıyla kayıtlı bir hesap zaten var.", status: 409 }),
      { status: 409 }),
  );
  renderPage();
  await fillForm();
  await userEvent.click(screen.getByRole("button", { name: /kaydol/i }));

  expect(await screen.findByText(/zaten var/i)).toBeInTheDocument();
});
