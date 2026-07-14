import { screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { AuthProvider } from "../../auth/AuthContext";
import { ToastProvider } from "../../layout/ToastProvider";
import { SESSION_KEY } from "../../api/session";
import { PayrollPage } from "./PayrollPage";

const periods = [
  { id: 3, name: "Temmuz 2026", year: 2026, month: 7, status: "draft", employeeCount: 4 },
  { id: 2, name: "Haziran 2026", year: 2026, month: 6, status: "approved", employeeCount: 4 },
];

const setRole = (role: string) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify({
    token: "T", refreshToken: "R",
    user: { id: "u", name: "X", email: "x@x", role, roleLabel: "X", initials: "XX", employeeId: 5 },
  }));

beforeEach(() => {
  localStorage.clear();
  stubApi({ "/api/payroll/periods": periods, "/api/payroll/my": [] });
});
afterEach(() => vi.unstubAllGlobals());

const renderShell = () =>
  renderPage(
    <AuthProvider>
      <ToastProvider>
        <PayrollPage tab="period" />
      </ToastProvider>
    </AuthProvider>,
  );

test("hr-admin sekmeleri ve dönem seçicisini görür, ilk dönem seçilir", async () => {
  setRole("hr-admin");
  renderShell();
  expect(await screen.findByText("Dönem Bordrosu")).toBeInTheDocument();
  expect(screen.getByText("Tekil Hesaplama")).toBeInTheDocument();
  expect(screen.getByText("Bordro Ayarları")).toBeInTheDocument();
  expect(screen.getByLabelText("Bordro dönemi")).toHaveValue("3");
});

test("çalışan sekme kabuğu yerine Bordrolarım görünümünü alır", async () => {
  setRole("employee");
  renderShell();
  expect(await screen.findByText("Henüz bordro kaydınız yok.")).toBeInTheDocument();
  expect(screen.queryByText("Dönem Bordrosu")).not.toBeInTheDocument();
});
