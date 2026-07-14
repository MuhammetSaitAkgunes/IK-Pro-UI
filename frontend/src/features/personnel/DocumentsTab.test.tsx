import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { DocumentsTab } from "./DocumentsTab";

const documents = [
  { id: 9, documentType: "Kimlik", fileName: "kimlik.pdf", contentType: "application/pdf", sizeBytes: 1024, createdAtUtc: "2026-07-14T10:00:00Z" },
];

beforeEach(() => stubApi({ "/api/employees/1/documents": documents }));
afterEach(() => vi.unstubAllGlobals());

const renderTab = (employeeId: number | null, readOnly = false) =>
  renderPage(
    <ToastProvider>
      <DocumentsTab employeeId={employeeId} readOnly={readOnly} />
    </ToastProvider>,
  );

test("evrak listesi dolar", async () => {
  renderTab(1);
  expect(await screen.findByText("kimlik.pdf")).toBeInTheDocument();
  expect(screen.getByText("Kimlik")).toBeInTheDocument();
});

test("dosya seçilince FormData ile yükleme isteği gider", async () => {
  renderTab(1);
  await screen.findByText("kimlik.pdf");
  const file = new File(["icerik"], "ikametgah.pdf", { type: "application/pdf" });
  await userEvent.upload(screen.getByLabelText("Evrak dosyası seç"), file);
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/employees/1/documents" && i?.method === "POST",
    );
    expect(posted).toBeTruthy();
    expect(posted![1]?.body).toBeInstanceOf(FormData);
  });
});

test("yeni kayıtta bilgi notu görünür", () => {
  renderTab(null);
  expect(screen.getByText(/önce personel kaydını oluşturun/i)).toBeInTheDocument();
});
