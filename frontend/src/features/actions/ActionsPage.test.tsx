import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { AuthProvider } from "../../auth/AuthContext";
import { ToastProvider } from "../../layout/ToastProvider";
import { SESSION_KEY } from "../../api/session";
import { ActionsPage } from "./ActionsPage";

const actions = [
  { id: 1, title: "SGK matrah kontrolü", source: "Bordro", sourceRoute: "payroll", owner: "İK Operasyon", due: "Bugün", priority: "high", status: "open", action: "Tavana yaklaşan kayıtları incele." },
  { id: 2, title: "Eksik evrak takibi", source: "Uyum", sourceRoute: "compliance-risk", owner: "Ece Arslan", due: "3 gün", priority: "medium", status: "week", action: "KVKK eklerini tamamlat." },
  { id: 3, title: "Anket sonuç raporu", source: "Nabız", sourceRoute: null, owner: "İK Operasyon", due: "Gecikti", priority: "low", status: "done", action: "Raporu arşivle." },
];
const audit = [
  { id: 9, actor: "İK Yöneticisi", action: "Bordro dönemi onaylandı", module: "Bordro", detail: "Temmuz 2026 dönemi onaylandı.", entityName: "PayrollPeriod", entityId: "3", createdAtUtc: "2026-07-16T09:00:00Z" },
];

const setRole = (role: string) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify({
    token: "T", refreshToken: "R",
    user: { id: "u", name: "X", email: "x@x", role, roleLabel: "X", initials: "XX", employeeId: 5 },
  }));

beforeEach(() => {
  localStorage.clear();
  setRole("hr-admin");
  stubApi({
    "/api/actions": actions,
    "/api/actions/1/status": { ...actions[0], status: "week" },
    "/api/actions/1": {},
    "/api/audit-logs": audit,
  });
});
afterEach(() => vi.unstubAllGlobals());

const renderShell = () =>
  renderPage(
    <AuthProvider>
      <ToastProvider>
        <ActionsPage />
      </ToastProvider>
    </AuthProvider>,
  );

test("KPI'lar listeden türetilir, açık sekme kartları gösterir", async () => {
  renderShell();
  expect(await screen.findByText("Global Aksiyon Merkezi")).toBeInTheDocument();
  expect(screen.getByText("SGK matrah kontrolü")).toBeInTheDocument();
  expect(screen.queryByText("Eksik evrak takibi")).not.toBeInTheDocument();
  expect(screen.getByText("Tavana yaklaşan kayıtları incele.")).toBeInTheDocument();
});

test("öncelik filtresi server-side sorgu atar", async () => {
  renderShell();
  await screen.findByText("SGK matrah kontrolü");
  await userEvent.selectOptions(screen.getByLabelText("Öncelik filtresi"), "high");
  await waitFor(() => {
    const hit = vi.mocked(fetch).mock.calls.some(([u]) => String(u) === "/api/actions?priority=high");
    expect(hit).toBe(true);
  });
});

test("Bu Hafta sekmesi week kartlarını gösterir", async () => {
  renderShell();
  await screen.findByText("SGK matrah kontrolü");
  await userEvent.click(screen.getByRole("button", { name: "Bu Hafta" }));
  expect(screen.getByText("Eksik evrak takibi")).toBeInTheDocument();
  expect(screen.queryByText("SGK matrah kontrolü")).not.toBeInTheDocument();
});

test("Denetim İzi sekmesi gerçek audit kayıtlarını gösterir", async () => {
  renderShell();
  await screen.findByText("SGK matrah kontrolü");
  await userEvent.click(screen.getByRole("button", { name: "Denetim İzi" }));
  expect(await screen.findByText("Bordro dönemi onaylandı")).toBeInTheDocument();
  expect(screen.getByText("İK Yöneticisi · Bordro")).toBeInTheDocument();
});

test("employee rolünde Denetim İzi sekmesi görünmez", async () => {
  setRole("employee");
  renderShell();
  await screen.findByText("SGK matrah kontrolü");
  expect(screen.queryByRole("button", { name: "Denetim İzi" })).not.toBeInTheDocument();
});
