import { screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { RiskCenterPage } from "./RiskCenterPage";

const metrics = {
  riskScore: 62,
  managerLoadIndex: 71,
  attritionHigh: 3,
  burnoutRisk: 2,
  criticalActions: 7,
  pulseScore: 64,
  riskTrend: [48, 51, 49, 54, 57, 59, 62, 64, 61, 67, 69, 62],
  departmentRisk: [
    { departmentId: 1, dept: "Yazılım", risk: 74, employeeCount: 18, highAttritionCount: 2, highBurnoutCount: 1 },
  ],
  talentCapacity: [
    { label: "İşe Alım Sağlığı", value: 72, meta: "8 açık pozisyon", tone: "medium" },
    { label: "Kritik Rol Riski", value: 3, meta: "Yedekleme planı yok", tone: "high" },
  ],
  employees: [],
};
const managerLoad = { managerLoadIndex: 71, criticalManagerCount: 2, pendingApprovals: 5, openActions: 14, managers: [] };
const voice = { pulseScore: 64, eNps: 8, participationRate: 76, decliningTeams: 2, sentimentTrend: "Son nabız ölçümünde 2 ekipte bağlılık geriledi", departments: [], signals: [], recommendedActions: [] };
const compliance = { documentComplianceScore: 82, missingDocuments: 11, upcomingDocuments: 18, auditReadinessRisk: "Orta", auditReadinessScore: 74, records: [], deadlines: [] };
const actions = [
  { id: 1, title: "Tükenmişlik sinyali", source: "Risk Merkezi", sourceRoute: "burnout-risk", owner: "Ece Arslan", due: "Bugün", priority: "high", status: "open", action: "Kapasite görüşmesi planla." },
  { id: 2, title: "İzlenen konu", source: "İşe Alım", sourceRoute: "recruitment", owner: "İşe Alım", due: null, priority: "low", status: "open", action: null },
  { id: 3, title: "Kapanan iş", source: "Bordro", sourceRoute: null, owner: "İK", due: null, priority: "high", status: "done", action: null },
];

beforeEach(() =>
  stubApi({
    "/api/dashboard/metrics": metrics,
    "/api/dashboard/manager-load": managerLoad,
    "/api/dashboard/employee-voice": voice,
    "/api/dashboard/compliance": compliance,
    "/api/actions": actions,
  }),
);
afterEach(() => vi.unstubAllGlobals());

test("KPI kartları ve türetilmiş alt metinler dolar", async () => {
  renderPage(<RiskCenterPage />);
  expect(await screen.findByText("İK Risk Skoru")).toBeInTheDocument();
  expect(screen.getByText("5 onay, 14 açık aksiyon, yoğun ekip")).toBeInTheDocument();
  // 62 - 48 = +14 puan pill
  expect(screen.getByText("+14 puan")).toBeInTheDocument();
});

test("ısı haritası satırı backend sayılarından türetilir", async () => {
  renderPage(<RiskCenterPage />);
  expect(await screen.findByText("18 çalışan · 2 yüksek ayrılma · 1 tükenmişlik sinyali")).toBeInTheDocument();
});

test("en acil aksiyonlar done hariç önceliğe göre listelenir", async () => {
  renderPage(<RiskCenterPage />);
  expect(await screen.findByText("Tükenmişlik sinyali")).toBeInTheDocument();
  expect(screen.queryByText("Kapanan iş")).not.toBeInTheDocument();
  expect(screen.getByText("Tümünü aç (2)")).toBeInTheDocument();
});
