import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { ApiError, apiDownload, apiFetch } from "./client";
import { SESSION_KEY, getSession, setSession } from "./session";

const user = { id: "u1", name: "İK Yöneticisi", email: "ik@hrmaster.local", role: "hr-admin", roleLabel: "İK Admin", initials: "İK", employeeId: null } as never;
const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });

beforeEach(() => {
  localStorage.clear();
  vi.stubGlobal("fetch", vi.fn());
});
afterEach(() => vi.unstubAllGlobals());

test("başarılı istek Bearer başlığı taşır ve JSON döner", async () => {
  setSession({ token: "T1", refreshToken: "R1", user });
  vi.mocked(fetch).mockResolvedValueOnce(json(200, { ok: true }));

  const result = await apiFetch<{ ok: boolean }>("/me");

  expect(result.ok).toBe(true);
  const [url, init] = vi.mocked(fetch).mock.calls[0];
  expect(url).toBe("/api/me");
  expect(new Headers(init?.headers).get("Authorization")).toBe("Bearer T1");
});

test("ProblemDetails hatası ApiError olarak fırlar", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(json(409, { title: "Çakışan kayıt.", status: 409 }));

  const error = await apiFetch("/leaves", { method: "POST" }).catch((e) => e);

  expect(error).toBeInstanceOf(ApiError);
  expect(error).toMatchObject({ status: 409, message: "Çakışan kayıt." });
});

test("401'de refresh denenir ve istek yeni token'la tekrarlanır", async () => {
  setSession({ token: "ESKI", refreshToken: "R1", user });
  vi.mocked(fetch)
    .mockResolvedValueOnce(json(401, { title: "Yetkisiz" }))
    .mockResolvedValueOnce(json(200, { token: "YENI", refreshToken: "R2", expiresAtUtc: "2026-07-13T12:00:00Z", user }))
    .mockResolvedValueOnce(json(200, { ok: true }));

  const result = await apiFetch<{ ok: boolean }>("/me");

  expect(result.ok).toBe(true);
  expect(getSession()?.token).toBe("YENI");
  expect(vi.mocked(fetch).mock.calls[1][0]).toBe("/api/auth/refresh");
  expect(new Headers(vi.mocked(fetch).mock.calls[2][1]?.headers).get("Authorization")).toBe("Bearer YENI");
});

test("refresh de düşerse oturum silinir ve login'e yönlenir", async () => {
  setSession({ token: "ESKI", refreshToken: "R1", user });
  vi.mocked(fetch)
    .mockResolvedValueOnce(json(401, { title: "Yetkisiz" }))
    .mockResolvedValueOnce(json(401, { title: "Refresh geçersiz" }));

  await expect(apiFetch("/me")).rejects.toMatchObject({ status: 401 });
  expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  expect(window.location.hash).toBe("#/login");
});

test("401 sonrası retry'de FormData gövdesi alanlarıyla korunur", async () => {
  setSession({ token: "ESKI", refreshToken: "R1", user });
  vi.mocked(fetch)
    .mockResolvedValueOnce(json(401, { title: "Yetkisiz" }))
    .mockResolvedValueOnce(json(200, { token: "YENI", refreshToken: "R2", expiresAtUtc: "2026-07-13T12:00:00Z", user }))
    .mockResolvedValueOnce(json(200, { ok: true }));

  const form = new FormData();
  form.append("file", new Blob(["içerik"]), "belge.pdf");
  const result = await apiFetch<{ ok: boolean }>("/employees/1/documents", { method: "POST", body: form });

  expect(result.ok).toBe(true);
  // Üçüncü çağrı retry'dir; gövdesi hâlâ FormData ve "file" alanını taşımalı.
  const retryBody = vi.mocked(fetch).mock.calls[2][1]?.body as FormData;
  expect(retryBody).toBeInstanceOf(FormData);
  expect(retryBody.get("file")).toBeInstanceOf(Blob);
});

test("refresh başarılı ama retry de 401 dönerse oturum silinir", async () => {
  setSession({ token: "ESKI", refreshToken: "R1", user });
  vi.mocked(fetch)
    .mockResolvedValueOnce(json(401, { title: "Yetkisiz" }))
    .mockResolvedValueOnce(json(200, { token: "YENI", refreshToken: "R2", expiresAtUtc: "2026-07-13T12:00:00Z", user }))
    .mockResolvedValueOnce(json(401, { title: "Hâlâ yetkisiz" }));

  await expect(apiFetch("/me")).rejects.toMatchObject({ status: 401 });
  expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  expect(window.location.hash).toBe("#/login");
});

test("FormData gövdesinde Content-Type başlığı eklenmez", async () => {
  setSession({ token: "T1", refreshToken: "R1", user });
  vi.mocked(fetch).mockResolvedValueOnce(json(200, { ok: true }));

  const form = new FormData();
  form.append("documentType", "kimlik");
  await apiFetch("/employees/1/documents", { method: "POST", body: form });

  const [, init] = vi.mocked(fetch).mock.calls[0];
  expect(new Headers(init?.headers).has("Content-Type")).toBe(false);
});

test("apiDownload blob ve Content-Disposition dosya adını döner", async () => {
  setSession({ token: "T1", refreshToken: "R1", user });
  vi.mocked(fetch).mockResolvedValueOnce(
    new Response("pdf-icerik", {
      status: 200,
      headers: { "Content-Disposition": 'attachment; filename="ozluk.pdf"' },
    }),
  );

  const result = await apiDownload("/employees/1/documents/9");

  expect(result.fileName).toBe("ozluk.pdf");
  expect(await result.blob.text()).toBe("pdf-icerik");
});

test("apiDownload hata durumunda ApiError fırlatır", async () => {
  vi.mocked(fetch).mockResolvedValueOnce(json(404, { title: "Evrak bulunamadı.", status: 404 }));
  await expect(apiDownload("/employees/1/documents/9")).rejects.toMatchObject({ status: 404 });
});
