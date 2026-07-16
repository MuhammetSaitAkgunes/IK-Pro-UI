import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { ManagerPage } from "./ManagerPage";

const overview = {
  activeEmployees: 12, pendingApprovals: 2, openPositions: 1, newApplications: 3,
  inOfficeToday: 9, onLeaveToday: 3, pulseScore: 74, departmentDistribution: [], recruitmentFunnel: {},
};
const pending = [
  { id: 4, employeeId: 3, employeeName: "Ahmet Yılmaz", leaveTypeId: 1, leaveTypeName: "Yıllık İzin", startDate: "2026-08-12", endDate: "2026-08-17", days: 5, status: "pending", description: null },
];

beforeEach(() =>
  stubApi({
    "/api/dashboard/overview": overview,
    "/api/leaves/pending": pending,
    "/api/leaves/4/approve": { ...pending[0], status: "approved" },
  }),
);
afterEach(() => vi.unstubAllGlobals());

const renderShell = () =>
  renderPage(
    <ToastProvider>
      <ManagerPage />
    </ToastProvider>,
  );

test("KPI'lar gerçek veriyle dolar, onay paneli talepleri listeler", async () => {
  renderShell();
  expect(await screen.findByText("Yönetici Konsolu")).toBeInTheDocument();
  expect(screen.getByText("3 kişi")).toBeInTheDocument();
  expect(screen.getByText("1 talep")).toBeInTheDocument();
  expect(screen.getByText("Ahmet Yılmaz")).toBeInTheDocument();
  expect(screen.getByText("Yıllık İzin")).toBeInTheDocument();
});

test("Onayla butonu approve ucuna gider", async () => {
  renderShell();
  await screen.findByText("Ahmet Yılmaz");
  await userEvent.click(screen.getByRole("button", { name: "Onayla" }));
  await waitFor(() => {
    const hit = vi.mocked(fetch).mock.calls.some(
      ([u, i]) => String(u) === "/api/leaves/4/approve" && i?.method === "POST",
    );
    expect(hit).toBe(true);
  });
});

test("bekleyen yoksa panel boş durumu gösterir", async () => {
  stubApi({ "/api/dashboard/overview": overview, "/api/leaves/pending": [] });
  renderShell();
  expect(await screen.findByText("Bekleyen talep yok.")).toBeInTheDocument();
});
