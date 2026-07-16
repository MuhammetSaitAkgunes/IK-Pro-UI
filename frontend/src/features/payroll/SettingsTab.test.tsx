import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { SettingsTab } from "./SettingsTab";

const settings = {
  year: 2026, overtimeMultiplier: 1.5, monthlyWorkingHours: 225, defaultWorkedDays: 30,
  sgkEmployeeRate: 14, unemploymentEmployeeRate: 1, sgkEmployerRate: 20.5, unemploymentEmployerRate: 2,
  stampTaxRate: 0.759, sgkBaseMin: 33030, sgkBaseMax: 297270,
  monthlyMinWageIncomeTaxExemption: 4211, monthlyMinWageStampTaxExemption: 250.7,
  minWageGross: 33030, taxBrackets: [],
};

beforeEach(() => stubApi({ "/api/payroll/settings": settings }));
afterEach(() => vi.unstubAllGlobals());

const renderTab = () =>
  renderPage(
    <ToastProvider>
      <SettingsTab />
    </ToastProvider>,
  );

test("alanlar backend ayarlarıyla dolar", async () => {
  renderTab();
  expect(await screen.findByLabelText("Fazla mesai çarpanı")).toHaveValue(1.5);
  expect(screen.getByLabelText("SGK PEK alt sınırı")).toHaveValue(33030);
});

test("kaydet PUT settings ucuna tam komutla gider", async () => {
  renderTab();
  const multiplier = await screen.findByLabelText("Fazla mesai çarpanı");
  await userEvent.clear(multiplier);
  await userEvent.type(multiplier, "2");
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  await waitFor(() => {
    const put = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/payroll/settings" && i?.method === "PUT",
    );
    expect(put).toBeTruthy();
    const body = JSON.parse(String(put![1]?.body));
    expect(body).toMatchObject({ year: 2026, overtimeMultiplier: 2, sgkBaseMin: 33030, minWageGross: 33030 });
    expect(body.taxBrackets).toBeUndefined();
  });
  expect(await screen.findByText(/Ayarlar kaydedildi/)).toBeInTheDocument();
});

test("varsayılana dön formu sorgu değerlerine geri alır", async () => {
  renderTab();
  const multiplier = await screen.findByLabelText("Fazla mesai çarpanı");
  await userEvent.clear(multiplier);
  await userEvent.type(multiplier, "3");
  await userEvent.click(screen.getByRole("button", { name: /Varsayılana dön/ }));
  expect(screen.getByLabelText("Fazla mesai çarpanı")).toHaveValue(1.5);
});
