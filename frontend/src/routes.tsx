import type { RouteObject } from "react-router-dom";
import { LoginPage } from "./auth/LoginPage";
import { PublicOnly, RequireAuth, RouteGate } from "./auth/guards";
import { PlaceholderPage } from "./pages/PlaceholderPage";
import { AppShell } from "./layout/AppShell";
import { OverviewPage } from "./features/overview/OverviewPage";
import { RiskCenterPage } from "./features/dashboard/RiskCenterPage";
import { AttritionDetailPage } from "./features/dashboard/AttritionDetailPage";
import { BurnoutDetailPage } from "./features/dashboard/BurnoutDetailPage";
import { ManagerLoadPage } from "./features/dashboard/ManagerLoadPage";
import { EmployeeVoicePage } from "./features/dashboard/EmployeeVoicePage";
import { CompliancePage } from "./features/compliance/CompliancePage";
import { PersonnelPage } from "./features/personnel/PersonnelPage";
import { RecruitmentPage } from "./features/recruitment/RecruitmentPage";
import { LeavesPage } from "./features/leaves/LeavesPage";
import { AttendancePage } from "./features/attendance/AttendancePage";
import { PayrollPeriodPage, PayrollCalculatorPage, PayrollSettingsPage } from "./features/payroll/PayrollPage";

export type Role = "hr-admin" | "manager" | "employee";

export type AppRoute = {
  key: string;
  path: string;
  title: string;
  eyebrow: string;
  navKey: string;
  roles: Role[];
};

const ALL: Role[] = ["hr-admin", "manager", "employee"];
const MGMT: Role[] = ["hr-admin", "manager"];

// routes.js ile birebir (sıra dahil).
export const appRoutes: AppRoute[] = [
  { key: "dashboard", path: "/dashboard", title: "Risk Merkezi", eyebrow: "İK Pro", navKey: "dashboard", roles: MGMT },
  { key: "overview", path: "/overview", title: "Genel Durum", eyebrow: "İK Pro", navKey: "overview", roles: ALL },
  { key: "actions", path: "/actions", title: "Aksiyonlar", eyebrow: "İK Pro", navKey: "actions", roles: ALL },
  { key: "personnel", path: "/personnel", title: "Personel Yönetimi", eyebrow: "İK Pro", navKey: "personnel", roles: MGMT },
  { key: "recruitment", path: "/recruitment", title: "İşe Alım", eyebrow: "İK Pro", navKey: "recruitment", roles: ["hr-admin"] },
  { key: "attendance", path: "/attendance", title: "Mesai & Puantaj", eyebrow: "İK Pro", navKey: "attendance", roles: MGMT },
  { key: "leaves", path: "/leaves", title: "İzinlerim", eyebrow: "İK Pro", navKey: "leaves", roles: ALL },
  { key: "payroll", path: "/payroll", title: "Bordro", eyebrow: "İK Pro", navKey: "payroll", roles: ["hr-admin", "employee"] },
  { key: "payroll-calculator", path: "/payroll/calculator", title: "Bordro", eyebrow: "İK Pro", navKey: "payroll", roles: ["hr-admin"] },
  { key: "payroll-settings", path: "/payroll/settings", title: "Bordro", eyebrow: "İK Pro", navKey: "payroll", roles: ["hr-admin"] },
  { key: "manager", path: "/manager", title: "Yönetici Konsolu", eyebrow: "İK Pro", navKey: "manager", roles: MGMT },
  { key: "settings", path: "/settings", title: "Ayarlar", eyebrow: "İK Pro", navKey: "settings", roles: ["hr-admin"] },
  { key: "attrition-risk", path: "/risk/attrition", title: "Ayrılma Riski Detayı", eyebrow: "Risk Merkezi", navKey: "dashboard", roles: MGMT },
  { key: "burnout-risk", path: "/risk/burnout", title: "Tükenmişlik Sinyali", eyebrow: "Risk Merkezi", navKey: "dashboard", roles: MGMT },
  { key: "manager-load", path: "/risk/manager-load", title: "Yönetici Yükü", eyebrow: "Risk Merkezi", navKey: "dashboard", roles: MGMT },
  { key: "action-center", path: "/risk/action-center", title: "Aksiyonlar", eyebrow: "İK Pro", navKey: "actions", roles: MGMT },
  { key: "employee-voice", path: "/risk/employee-voice", title: "Çalışan Nabzı", eyebrow: "Risk Merkezi", navKey: "dashboard", roles: MGMT },
  { key: "compliance-risk", path: "/risk/compliance", title: "Uyum Risk Merkezi", eyebrow: "Risk Merkezi", navKey: "dashboard", roles: MGMT },
];

export const roleHomeFor = (role: string | null | undefined): string =>
  role === "employee" ? "/overview" : "/dashboard";

// layout.js ile birebir.
export const navIcons: Record<string, string> = {
  dashboard: "fa-shield-heart", overview: "fa-chart-line", actions: "fa-list-check",
  personnel: "fa-user-tie", recruitment: "fa-briefcase", attendance: "fa-clock",
  leaves: "fa-plane-departure", payroll: "fa-money-check-dollar",
  manager: "fa-chart-pie", settings: "fa-gear",
};

export const navGroups = [
  { label: "Ana Sayfa", keys: ["dashboard", "overview", "actions"] },
  { label: "Çekirdek İK", keys: ["personnel", "recruitment", "attendance", "leaves"] },
  { label: "Bordro", keys: ["payroll"] },
  { label: "Yönetim", keys: ["manager", "settings"] },
];

// Sayfa component eşlemesi: sonraki dilimler PlaceholderPage'i gerçek sayfayla değiştirir.
const pageFor: Record<string, () => JSX.Element> = {
  overview: OverviewPage,
  dashboard: RiskCenterPage,
  "attrition-risk": AttritionDetailPage,
  "burnout-risk": BurnoutDetailPage,
  "manager-load": ManagerLoadPage,
  "employee-voice": EmployeeVoicePage,
  "compliance-risk": CompliancePage,
  personnel: PersonnelPage,
  recruitment: RecruitmentPage,
  leaves: LeavesPage,
  attendance: AttendancePage,
  payroll: PayrollPeriodPage,
  "payroll-calculator": PayrollCalculatorPage,
  "payroll-settings": PayrollSettingsPage,
};

// Component JSX olarak render edilir (düz fonksiyon çağrısı hook kurallarını bozar).
function GatedPage({ route }: { route: AppRoute }) {
  const Page = pageFor[route.key] ?? PlaceholderPage;
  return (
    <RouteGate route={route}>
      <Page />
    </RouteGate>
  );
}

export function buildRouteObjects(): RouteObject[] {
  return [
    { path: "/login", element: <PublicOnly><LoginPage mode="login" /></PublicOnly> },
    { path: "/signup", element: <PublicOnly><LoginPage mode="signup" /></PublicOnly> },
    {
      element: <RequireAuth />,
      children: [
        {
          element: <AppShell />,
          children: appRoutes.map((route) => ({
            path: route.path,
            element: <GatedPage route={route} />,
          })),
        },
      ],
    },
    { path: "*", element: <PublicOnly><LoginPage mode="login" /></PublicOnly> },
  ];
}
