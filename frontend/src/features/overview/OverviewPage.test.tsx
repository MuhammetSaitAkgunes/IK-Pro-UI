import { screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { OverviewPage } from "./OverviewPage";

const overview = {
  activeEmployees: 42,
  pendingApprovals: 5,
  openPositions: 8,
  newApplications: 32,
  inOfficeToday: 30,
  onLeaveToday: 4,
  pulseScore: 66,
  departmentDistribution: [
    { dept: "Yazılım", count: 18 },
    { dept: "Satış", count: 12 },
  ],
  recruitmentFunnel: { total: 120, new: 64, interview: 45, offer: 12, rejected: 20, hired: 5 },
};

beforeEach(() => stubApi({ "/api/dashboard/overview": overview }));
afterEach(() => vi.unstubAllGlobals());

test("KPI kartları backend verisiyle dolar", async () => {
  renderPage(<OverviewPage />);
  expect(await screen.findByText("42")).toBeInTheDocument();
  expect(screen.getByText("Aktif Personel")).toBeInTheDocument();
  expect(screen.getByText("32 yeni başvuru")).toBeInTheDocument();
  expect(screen.getByText("2 departmanda aktif kadro")).toBeInTheDocument();
});

test("çalışma durumu satırları türetilmiş sayıları gösterir", async () => {
  renderPage(<OverviewPage />);
  expect(await screen.findByText("Ofiste")).toBeInTheDocument();
  expect(screen.getByText("30")).toBeInTheDocument();
  // Kayıt bekleyen = 42 - 30 - 4 = 8; "8" açık pozisyon KPI'ında da geçer, satır bazlı kontrol:
  expect(screen.getByText("Kayıt Bekleyen").closest(".status-item")).toHaveTextContent("8");
  expect(screen.getByText("%66 pozitif")).toBeInTheDocument();
});

test("grafikler render edilir (stub canvas)", async () => {
  renderPage(<OverviewPage />);
  expect(await screen.findByTestId("chart-doughnut")).toBeInTheDocument();
  expect(screen.getByTestId("chart-bar")).toBeInTheDocument();
});
