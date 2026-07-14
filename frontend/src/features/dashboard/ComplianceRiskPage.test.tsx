import { screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ComplianceRiskPage } from "./ComplianceRiskPage";

const compliance = {
  documentComplianceScore: 82, missingDocuments: 11, upcomingDocuments: 18,
  auditReadinessRisk: "Orta", auditReadinessScore: 74,
  records: [
    { id: 1, employee: "Burak Demir", dept: "Satış", document: "KVKK açık rıza eki", owner: "İK Operasyon", dueDate: "Bugün", status: "Eksik", level: "high" },
    { id: 2, employee: "Ayşe Vural", dept: "İK", document: "Personel dosyası kontrolü", owner: "İK Operasyon", dueDate: "Tamamlandı", status: "Tamamlandı", level: "low" },
  ],
  deadlines: [
    { title: "KVKK açık rıza eki", count: 4, dueDate: "Bugün", owner: "İK Operasyon", level: "high" },
  ],
};

beforeEach(() => stubApi({ "/api/dashboard/compliance": compliance }));
afterEach(() => vi.unstubAllGlobals());

test("uyum tablosu durum pill'lerini doğru sınıfla basar", async () => {
  renderPage(<ComplianceRiskPage />);
  expect(await screen.findByText("Uyum, Evrak ve Denetim Risk Merkezi")).toBeInTheDocument();
  expect(screen.getByText("Eksik")).toHaveClass("status-pill", "rejected");
  expect(screen.getByText("Tamamlandı", { selector: ".status-pill" })).toHaveClass("approved");
});

test("yaklaşan son tarihler aside'ı dolar", async () => {
  renderPage(<ComplianceRiskPage />);
  expect(await screen.findByText("Yaklaşan Son Tarihler")).toBeInTheDocument();
  expect(screen.getByText("4 kayıt · İK Operasyon")).toBeInTheDocument();
});
