import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { CandidateModal } from "./CandidateModal";

beforeEach(() =>
  stubApi({
    "/api/candidates": { id: 7, name: "Zeynep Aksoy", appliedRole: "QA Engineer", status: "Yeni", score: 70, initials: "ZA" },
  }),
);
afterEach(() => vi.unstubAllGlobals());

test("form doldurulup kaydedilince POST candidates tam gövdeyle gider", async () => {
  const onCreated = vi.fn();
  renderPage(
    <ToastProvider>
      <CandidateModal onClose={() => {}} onCreated={onCreated} />
    </ToastProvider>,
  );
  await userEvent.type(screen.getByLabelText("Ad Soyad"), "Zeynep Aksoy");
  await userEvent.type(screen.getByLabelText("Başvurulan pozisyon"), "QA Engineer");
  await userEvent.clear(screen.getByLabelText("AI puanı (0-100)"));
  await userEvent.type(screen.getByLabelText("AI puanı (0-100)"), "70");
  await userEvent.type(screen.getByLabelText("Yetenekler (virgülle)"), "Playwright, API testi");
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/candidates" && i?.method === "POST",
    );
    expect(posted).toBeTruthy();
    expect(JSON.parse(String(posted![1]?.body))).toMatchObject({
      name: "Zeynep Aksoy", appliedRole: "QA Engineer", score: 70,
      skills: ["Playwright", "API testi"],
    });
    expect(onCreated).toHaveBeenCalledWith(7);
  });
});

test("ad boşsa form hatası gösterilir, istek atılmaz", async () => {
  renderPage(
    <ToastProvider>
      <CandidateModal onClose={() => {}} onCreated={() => {}} />
    </ToastProvider>,
  );
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Ad Soyad ve pozisyon zorunludur.");
  expect(vi.mocked(fetch).mock.calls.some(([, i]) => i?.method === "POST")).toBe(false);
});
