import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { ImportModal } from "./ImportModal";

const rapor = {
  toplamSatir: 3,
  gecerliSatir: 1,
  hataliSatir: 2,
  mukerrerSatir: 0,
  bilinmeyenDepartmanlar: ["Olmayan Departman"],
  sorunlar: [{ satirNo: 3, alan: "Ad", mesaj: "Zorunlu alan." }],
};

beforeEach(() => stubApi({ "/api/employees/import/preview": rapor }));
afterEach(() => vi.unstubAllGlobals());

const render = () =>
  renderPage(
    <ToastProvider>
      <ImportModal open onClose={() => {}} />
    </ToastProvider>,
  );

const dosyaSec = async () => {
  const input = screen.getByLabelText(/Excel dosyası/i);
  await userEvent.upload(
    input,
    new File(["x"], "personel.xlsx", {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    }),
  );
};

test("dosya seçilince önizleme raporu gösterilir", async () => {
  render();
  await dosyaSec();

  // Satır numarası, alan ve mesaj birlikte görünmeli ki kullanıcı dosyada bulabilsin.
  expect(await screen.findByText("Zorunlu alan.")).toBeInTheDocument();
  expect(screen.getByText("Olmayan Departman")).toBeInTheDocument();
});

test("geçerli satır yoksa Aktar düğmesi pasiftir", async () => {
  stubApi({ "/api/employees/import/preview": { ...rapor, gecerliSatir: 0 } });
  render();
  await dosyaSec();

  await waitFor(() => expect(screen.getByRole("button", { name: /^Aktar$/ })).toBeDisabled());
});

test("dosya seçilmeden Aktar pasiftir", () => {
  render();
  expect(screen.getByRole("button", { name: /^Aktar$/ })).toBeDisabled();
});
