import { screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { EmployeeVoicePage } from "./EmployeeVoicePage";
import { ManagerLoadPage } from "./ManagerLoadPage";

const managerLoad = {
  managerLoadIndex: 71, criticalManagerCount: 1, pendingApprovals: 5, openActions: 14,
  managers: [
    { employeeId: 2, name: "Ece Arslan", team: 18, approvals: 5, actions: 7, overtime: 68, pulse: 59, load: 78 },
    { employeeId: 3, name: "Can Uslu", team: 9, approvals: 1, actions: 2, overtime: 34, pulse: 76, load: 38 },
  ],
};
const voice = {
  pulseScore: 64, eNps: 8, participationRate: 76, decliningTeams: 2,
  sentimentTrend: "Son nabız ölçümünde 2 ekipte bağlılık geriledi",
  departments: [
    { departmentId: 1, dept: "Yazılım", pulse: 58, eNps: 2, participation: 82, mood: "Baskı yüksek", driver: "Teslim takvimi", level: "high" },
    { departmentId: 2, dept: "İK", pulse: 78, eNps: 24, participation: 88, mood: "Pozitif", driver: "Net iletişim", level: "low" },
  ],
  signals: ["Yazılım ekibinde teslim takvimi kaynaklı sinyal izleniyor."],
  recommendedActions: ["Yönetici ile 1:1 görüşme başlat"],
};

beforeEach(() => stubApi({ "/api/dashboard/manager-load": managerLoad, "/api/dashboard/employee-voice": voice }));
afterEach(() => vi.unstubAllGlobals());

test("yönetici yükü tablosu kritik takibi işaretler", async () => {
  renderPage(<ManagerLoadPage />);
  expect(await screen.findByText("Ece Arslan")).toBeInTheDocument();
  expect(screen.getByText("Kritik takip")).toBeInTheDocument();
  expect(screen.getByText("Aksiyon devri ve kapasite görüşmesi")).toBeInTheDocument();
  expect(screen.getByText("Haftalık takip yeterli")).toBeInTheDocument();
});

test("nabız sayfası departman tablosu + türetilmiş riskli ekipleri gösterir", async () => {
  renderPage(<EmployeeVoicePage />);
  expect(await screen.findByText("Çalışan Sesleri / Nabız Analitiği")).toBeInTheDocument();
  expect(screen.getByText("Baskı yüksek", { selector: "td" })).toBeInTheDocument();
  // Riskli Ekipler aside yalnız level != low içerir: Yazılım var, İK yok
  const aside = screen.getByText("Riskli Ekipler").closest("aside")!;
  expect(aside).toHaveTextContent("Yazılım");
  expect(aside).not.toHaveTextContent("İK");
});
