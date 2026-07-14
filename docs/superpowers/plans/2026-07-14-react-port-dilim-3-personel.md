# React Port — Dilim 3: Personel + Departman — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Personel Yönetimi ekranını (`/personnel`) gerçek API'ye bağlı olarak portla: directory (server-side arama/filtre), toplu seçim + pasife alma, CSV dışa aktarma, 6 sekmeli tam personel kartı (gerçek CRUD), foto ve özlük evrakı yükleme/indirme.

**Architecture:** `src/features/personnel/` altında sayfa + modal + queries. Liste sorguları server-side (`/api/employees?search=&departmentId=&status=`); departmanlar `/api/departments`'tan gelir (ayrı ekran yok — eski uygulamada da yoktu). Dosya işlemleri için `apiFetch`'e FormData desteği ve `apiDownload` eklenir. Mutasyonlar backend'de hr-admin policy'sinde; UI'da mutasyon eylemleri yalnız hr-admin'e gösterilir, manager kartı salt-okur açar.

**Tech Stack:** Dilim 1–2 stack'i (yeni bağımlılık yok).

## Global Constraints

- Dilim 1–2 planlarındaki tüm kısıtlar geçerli (CSS'e dokunma, aynı class/DOM, Türkçe metinler birebir, her görev sonunda `cd frontend && npm test -- --run` yeşil).
- Rol dizeleri: `hr-admin` | `manager` (route MGMT); durum dizeleri backend ile birebir: `active` | `passive`.
- Testlerde gerçek ağ yok (`stubApi`); tipler `schema.d.ts`'ten.

### Veri/davranış eşleme kararları (mock ↔ backend farkları)

| Eski mock davranışı | Karar |
|---|---|
| TC Kimlik sütunu düz metin | Liste DTO'su `nationalIdMasked` döner → maskeli gösterilir (detayda `nationalId` açık) |
| Client-side filtre (`filterPersonnel`) | Server-side: `search` (300ms debounce), `departmentId`, `status` query paramları; boş sonuçta aynı `personnel-empty` bloğu |
| Sayfalama yok | `pageSize=50` ile çekilir; pager UI bu dilimde yok (seed küçük; gerekirse Dilim 8'de) |
| Tüm MGMT rolleri her eylemi görür | Mutasyon eylemleri (Yeni Personel, Pasife al, Düzenle kalemi, Kaydet, foto/evrak yükleme) yalnız `hr-admin`; manager karta salt-okur bakar (inputlar `disabled`) |
| "Yakınlık / Telefon" tek input | Değer `emergencyContactPhone`'a yazılır; `emergencyContactRelation` bu dilimde boş kalır (DOM paritesi korunur) |
| Foto önizleme her zaman ikon | Backend foto servis ucu sunmuyor (yalnız upload) → önizleme ikonu kalır; yükleme yalnız düzenleme modunda (`POST /employees/{id}/photo`), yeni kayıtta "önce kaydedin" toast'ı |
| Evrak sekmesi yalnız dropzone | Gerçek liste (`GET /{id}/documents`) + tür seçip yükleme + indirme eklenir; dropzone görseli korunur, tıklayınca dosya seçtirir; yeni kayıtta sekme "önce kaydedin" notu gösterir |
| `savePerson` sahte toast | Gerçek `POST /api/employees` / `PUT /api/employees/{id}`; ProblemDetails hatası modal başlığının altında `form-error` ile gösterilir |
| Durum rozetini elle değiştirme (bulk) | `POST /api/employees/bulk-deactivate { ids }` + liste invalidation |
| Departman verisi personel listesinden türetilirdi | `GET /api/departments` tek kaynak (filtre + kart formu); departman CRUD ekranı yok (eskide de yoktu — YAGNI) |
| Excel dışa aktarma DOM tablosundan | Aynı yaklaşım portlanır: `csv.ts` — `csv-skip` sütunları atlanır, `;` ayraç, UTF-8 BOM |

---

### Task 1: apiFetch dosya desteği — FormData + apiDownload (TDD)

**Files:**
- Modify: `frontend/src/api/client.ts`
- Test: `frontend/src/api/client.test.ts` (mevcut dosyaya test ekle)

**Interfaces:**
- Produces:
  - `apiFetch`: gövde `FormData` ise `Content-Type` başlığı **eklenmez** (tarayıcı boundary'yi kendisi yazar); mevcut davranış diğer gövdelerde değişmez.
  - `apiDownload(path: string): Promise<{ blob: Blob; fileName: string | null }>` — Bearer + 401-refresh akışını aynen kullanır, `Content-Disposition`'dan dosya adını çözer (`filename="..."` veya `filename*=UTF-8''...`), hata durumunda `ApiError` fırlatır.

- [x] **Step 1: Başarısız testleri ekle** — `client.test.ts` sonuna:

```ts
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
    new Response(new Blob(["pdf-icerik"]), {
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
```

`import { ApiError, apiDownload, apiFetch } from "./client";` olarak güncelle.

Run: `npm test -- --run src/api` → Beklenen: FAIL (apiDownload yok; Content-Type testi düşer).

- [x] **Step 2: `client.ts`'i güncelle**

`rawFetch` içindeki Content-Type satırını değiştir:

```ts
  if (!headers.has("Content-Type") && init.body && !(init.body instanceof FormData))
    headers.set("Content-Type", "application/json");
```

Dosyanın sonuna ekle:

```ts
const fileNameFrom = (disposition: string | null): string | null => {
  if (!disposition) return null;
  const utf8 = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8) return decodeURIComponent(utf8[1]);
  const plain = disposition.match(/filename="?([^";]+)"?/i);
  return plain ? plain[1] : null;
};

/** İkili indirme (evrak/pusula): Bearer + 401-refresh apiFetch ile aynı. */
export async function apiDownload(path: string): Promise<{ blob: Blob; fileName: string | null }> {
  let response = await rawFetch(path);

  if (response.status === 401 && !path.startsWith("/auth/")) {
    if (await tryRefresh()) {
      response = await rawFetch(path);
    } else {
      clearSession();
      window.location.hash = "/login";
    }
  }

  if (!response.ok) throw await toError(response);
  return { blob: await response.blob(), fileName: fileNameFrom(response.headers.get("Content-Disposition")) };
}
```

- [x] **Step 3: Testleri doğrula** — Run: `npm test -- --run src/api` → 7 test PASS.

- [x] **Step 4: Commit**

```bash
git add frontend/src/api/
git commit -m "feat(frontend): apiFetch FormData desteği + apiDownload (Content-Disposition çözümü)"
```

---

### Task 2: CSV yardımcıları + personel queries

**Files:**
- Create: `frontend/src/features/personnel/csv.ts`, `frontend/src/features/personnel/queries.ts`
- Test: `frontend/src/features/personnel/csv.test.ts`

**Interfaces:**
- Produces:
  - `csv.ts`: `tableToCsvLines(table: HTMLTableElement, rowFilter?: (row: HTMLTableRowElement) => boolean): string[]` (`.csv-skip` hücreleri atlar, `;` ayraç, `"` kaçışı, `display:none` satırları atlar), `downloadCsv(lines: string[], fileName: string): void` (UTF-8 BOM + a.click).
  - `queries.ts`:
    - `type EmployeeListItemDto`, `EmployeeDetailDto`, `EmployeeUpsertModel`, `DepartmentDto`, `EmployeeDocumentDto` (schema'dan re-export)
    - `type EmployeeFilters = { search: string; departmentId: string; status: string }`
    - `useEmployees(filters: EmployeeFilters)` → `GET /employees?search=&departmentId=&status=&pageSize=50` (boş paramlar gönderilmez)
    - `useDepartments()` → `GET /departments`
    - `useEmployee(id: number | null)` → `GET /employees/{id}` (`enabled: id !== null`)
    - `useEmployeeDocuments(id: number | null)` → `GET /employees/{id}/documents` (`enabled`)
    - `useSaveEmployee()` → mutation `{ id: number | null; model: EmployeeUpsertModel }`: id varsa PUT, yoksa POST; başarıda `["employees"]` invalidation
    - `useBulkDeactivate()` → mutation `number[]`: `POST /employees/bulk-deactivate` `{ ids }`; başarıda `["employees"]` invalidation
    - `useUploadPhoto()` → mutation `{ id: number; file: File }`: FormData `file`
    - `useUploadDocument()` → mutation `{ id: number; file: File; documentType: string }`; başarıda `["employees", id, "documents"]` invalidation

- [x] **Step 1: Başarısız testleri yaz** — `csv.test.ts`:

```ts
import { expect, test } from "vitest";
import { tableToCsvLines } from "./csv";

const buildTable = (html: string): HTMLTableElement => {
  const table = document.createElement("table");
  table.innerHTML = html;
  return table;
};

test("csv-skip hücreleri atlanır, değerler tırnaklanır", () => {
  const table = buildTable(`
    <thead><tr><th class="csv-skip">Seç</th><th>Ad</th><th>Departman</th></tr></thead>
    <tbody><tr><td class="csv-skip">x</td><td>Ahmet "Usta" Yılmaz</td><td>Yazılım</td></tr></tbody>
  `);
  expect(tableToCsvLines(table)).toEqual(['"Ad";"Departman"', '"Ahmet ""Usta"" Yılmaz";"Yazılım"']);
});

test("rowFilter false dönen satırları atlar", () => {
  const table = buildTable(`
    <tbody><tr data-keep="1"><td>Bir</td></tr><tr><td>İki</td></tr></tbody>
  `);
  const lines = tableToCsvLines(table, (row) => row.dataset.keep === "1");
  expect(lines).toEqual(['"Bir"']);
});
```

Run: `npm test -- --run src/features/personnel` → FAIL.

- [x] **Step 2: `csv.ts` yaz** (eski `exportTableToCSV` portu, iki parçaya bölünmüş)

```ts
export const tableToCsvLines = (
  table: HTMLTableElement,
  rowFilter?: (row: HTMLTableRowElement) => boolean,
): string[] => {
  const lines: string[] = [];
  table.querySelectorAll("tr").forEach((row) => {
    if (row.style.display === "none") return;
    if (rowFilter && !rowFilter(row)) return;

    const cells = Array.from(row.querySelectorAll("th, td"))
      .filter((cell) => !cell.classList.contains("csv-skip"))
      .map((cell) => `"${(cell as HTMLElement).innerText.replace(/\s+/g, " ").trim().replace(/"/g, '""')}"`);
    if (cells.length) lines.push(cells.join(";"));
  });
  return lines;
};

export const downloadCsv = (lines: string[], fileName: string): void => {
  // BOM: Excel'in Türkçe karakterleri UTF-8 olarak açması için.
  const blob = new Blob(["﻿" + lines.join("\n")], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `${fileName}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(link.href);
};
```

- [x] **Step 3: `queries.ts` yaz**

```ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../../api/client";
import type { components } from "../../api/schema";

export type EmployeeListItemDto = components["schemas"]["EmployeeListItemDto"];
export type EmployeePagedResult = components["schemas"]["EmployeeListItemDtoPagedResult"];
export type EmployeeDetailDto = components["schemas"]["EmployeeDetailDto"];
export type EmployeeUpsertModel = components["schemas"]["EmployeeUpsertModel"];
export type DepartmentDto = components["schemas"]["DepartmentDto"];
export type EmployeeDocumentDto = components["schemas"]["EmployeeDocumentDto"];

export type EmployeeFilters = { search: string; departmentId: string; status: string };

const employeesPath = (filters: EmployeeFilters): string => {
  const params = new URLSearchParams({ pageSize: "50" });
  if (filters.search.trim()) params.set("search", filters.search.trim());
  if (filters.departmentId) params.set("departmentId", filters.departmentId);
  if (filters.status) params.set("status", filters.status);
  return `/employees?${params.toString()}`;
};

export const useEmployees = (filters: EmployeeFilters) =>
  useQuery({
    queryKey: ["employees", filters],
    queryFn: () => apiFetch<EmployeePagedResult>(employeesPath(filters)),
  });

export const useDepartments = () =>
  useQuery({ queryKey: ["departments"], queryFn: () => apiFetch<DepartmentDto[]>("/departments") });

export const useEmployee = (id: number | null) =>
  useQuery({
    queryKey: ["employees", id],
    queryFn: () => apiFetch<EmployeeDetailDto>(`/employees/${id}`),
    enabled: id !== null,
  });

export const useEmployeeDocuments = (id: number | null) =>
  useQuery({
    queryKey: ["employees", id, "documents"],
    queryFn: () => apiFetch<EmployeeDocumentDto[]>(`/employees/${id}/documents`),
    enabled: id !== null,
  });

export const useSaveEmployee = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, model }: { id: number | null; model: EmployeeUpsertModel }) =>
      id === null
        ? apiFetch<EmployeeDetailDto>("/employees", { method: "POST", body: JSON.stringify(model) })
        : apiFetch<EmployeeDetailDto>(`/employees/${id}`, { method: "PUT", body: JSON.stringify(model) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["employees"] }),
  });
};

export const useBulkDeactivate = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids: number[]) =>
      apiFetch<{ deactivated: number }>("/employees/bulk-deactivate", {
        method: "POST",
        body: JSON.stringify({ ids }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["employees"] }),
  });
};

export const useUploadPhoto = () =>
  useMutation({
    mutationFn: ({ id, file }: { id: number; file: File }) => {
      const form = new FormData();
      form.append("file", file);
      return apiFetch<{ photoPath: string }>(`/employees/${id}/photo`, { method: "POST", body: form });
    },
  });

export const useUploadDocument = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file, documentType }: { id: number; file: File; documentType: string }) => {
      const form = new FormData();
      form.append("file", file);
      form.append("documentType", documentType);
      return apiFetch<EmployeeDocumentDto>(`/employees/${id}/documents`, { method: "POST", body: form });
    },
    onSuccess: (_data, { id }) =>
      queryClient.invalidateQueries({ queryKey: ["employees", id, "documents"] }),
  });
};
```

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run src/features/personnel` → 2 test PASS.

- [x] **Step 5: Commit**

```bash
git add frontend/src/features/personnel/
git commit -m "feat(frontend): personel CSV yardımcıları ve TanStack Query hook'ları"
```

---

### Task 3: Personel listesi sayfası (filtre + toplu seçim + CSV)

**Files:**
- Create: `frontend/src/features/personnel/PersonnelPage.tsx`
- Modify: `frontend/src/routes.tsx` (pageFor: personnel)
- Test: `frontend/src/features/personnel/PersonnelPage.test.tsx`

**Interfaces:**
- Consumes: Task 2 hook'ları, `useToast`, `useAuth`, `PageLoading/PageError`.
- Produces: `PersonnelPage()`; modal Task 4'te eklenecek — bu görevde "Yeni Personel"/"Görüntüle"/"Düzenle" butonları `onOpenCard(id | null)` iç state'ini set eder, modal placeholder olarak `null` render eder (Task 4 dolduracak).

- [x] **Step 1: Başarısız testleri yaz** — `PersonnelPage.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { AuthProvider } from "../../auth/AuthContext";
import { ToastProvider } from "../../layout/ToastProvider";
import { SESSION_KEY } from "../../api/session";
import { PersonnelPage } from "./PersonnelPage";

const employees = {
  items: [
    { id: 1, name: "Ahmet Yılmaz", title: "Senior Developer", nationalIdMasked: "123*****901", departmentId: 1, department: "Yazılım", status: "active", initials: "AY", hireDate: "2021-03-12" },
    { id: 2, name: "Selin Koç", title: "UI Designer", nationalIdMasked: "234*****012", departmentId: 2, department: "Tasarım", status: "passive", initials: "SK", hireDate: "2022-05-01" },
  ],
  total: 2, page: 1, pageSize: 50, totalPages: 1,
};
const departments = [
  { id: 1, name: "Yazılım", code: "YZL", employeeCount: 1 },
  { id: 2, name: "Tasarım", code: "TSR", employeeCount: 1 },
];

const setRole = (role: string) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify({
    token: "T", refreshToken: "R",
    user: { id: "u", name: "X", email: "x@x", role, roleLabel: "X", initials: "XX", employeeId: null },
  }));

const renderPersonnel = () =>
  renderPage(
    <AuthProvider>
      <ToastProvider>
        <PersonnelPage />
      </ToastProvider>
    </AuthProvider>,
  );

beforeEach(() => {
  localStorage.clear();
  stubApi({ "/api/employees": employees, "/api/departments": departments, "/api/employees/bulk-deactivate": { deactivated: 1 } });
});
afterEach(() => vi.unstubAllGlobals());

test("liste maskeli TC ve durum rozetiyle dolar", async () => {
  setRole("hr-admin");
  renderPersonnel();
  expect(await screen.findByText("Ahmet Yılmaz")).toBeInTheDocument();
  expect(screen.getByText("123*****901")).toBeInTheDocument();
  expect(screen.getByText("Pasif")).toHaveClass("badge-passive");
});

test("satır seçilince bulk bar görünür, pasife al isteği gider", async () => {
  setRole("hr-admin");
  renderPersonnel();
  await screen.findByText("Ahmet Yılmaz");
  await userEvent.click(screen.getByLabelText("Ahmet Yılmaz kaydını seç"));
  expect(screen.getByText("1 kişi seçildi")).toBeInTheDocument();
  await userEvent.click(screen.getByRole("button", { name: /Pasife al/ }));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(([u]) => String(u).includes("bulk-deactivate"));
    expect(posted).toBeTruthy();
    expect(JSON.parse(String(posted![1]?.body))).toEqual({ ids: [1] });
  });
});

test("manager mutasyon eylemlerini görmez", async () => {
  setRole("manager");
  renderPersonnel();
  await screen.findByText("Ahmet Yılmaz");
  expect(screen.queryByRole("button", { name: /Yeni Personel/ })).not.toBeInTheDocument();
  expect(screen.queryByTitle("Düzenle")).not.toBeInTheDocument();
  expect(screen.getByTitle("Görüntüle")).toBeInTheDocument();
});

test("arama filtresi query paramına yansır", async () => {
  setRole("hr-admin");
  renderPersonnel();
  await screen.findByText("Ahmet Yılmaz");
  await userEvent.type(screen.getByLabelText("Personel ara"), "ahmet");
  await waitFor(() => {
    const searched = vi.mocked(fetch).mock.calls.some(([u]) => String(u).includes("search=ahmet"));
    expect(searched).toBe(true);
  }, { timeout: 2000 });
});
```

Run: `npm test -- --run src/features/personnel/PersonnelPage.test.tsx` → FAIL.

- [x] **Step 2: `PersonnelPage.tsx` yaz** (eski Personnel() liste yarısının DOM paritesi)

```tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../layout/ToastProvider";
import { PageError, PageLoading } from "../shared/PageState";
import { downloadCsv, tableToCsvLines } from "./csv";
import { useBulkDeactivate, useDepartments, useEmployees, type EmployeeFilters } from "./queries";

const formatDate = (value?: string): string =>
  value ? new Date(value).toLocaleDateString("tr-TR") : "";

export function PersonnelPage() {
  const { user } = useAuth();
  const { showToast } = useToast();
  const isHrAdmin = user?.role === "hr-admin";

  const [searchInput, setSearchInput] = useState("");
  const [filters, setFilters] = useState<EmployeeFilters>({ search: "", departmentId: "", status: "" });
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [cardId, setCardId] = useState<number | null | undefined>(undefined); // undefined: kapalı, null: yeni kayıt
  const tableRef = useRef<HTMLTableElement>(null);

  // Arama debounce: 300ms sonra server-side filtreye yansır.
  useEffect(() => {
    const timer = setTimeout(
      () => setFilters((current) => ({ ...current, search: searchInput })),
      300,
    );
    return () => clearTimeout(timer);
  }, [searchInput]);

  const employeesQ = useEmployees(filters);
  const departmentsQ = useDepartments();
  const bulkDeactivate = useBulkDeactivate();

  const items = useMemo(() => employeesQ.data?.items ?? [], [employeesQ.data]);

  if (employeesQ.isPending || departmentsQ.isPending) return <PageLoading />;
  if (employeesQ.isError) return <PageError error={employeesQ.error} />;
  if (departmentsQ.isError) return <PageError error={departmentsQ.error} />;

  const departments = departmentsQ.data;
  const allVisibleSelected = items.length > 0 && items.every((e) => selected.has(e.id ?? -1));

  const toggleAll = (checked: boolean) =>
    setSelected(checked ? new Set(items.map((e) => e.id ?? -1)) : new Set());

  const toggleOne = (id: number, checked: boolean) =>
    setSelected((current) => {
      const next = new Set(current);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });

  const exportCsv = () => {
    if (!tableRef.current) return;
    const hasSelection = selected.size > 0;
    const lines = tableToCsvLines(tableRef.current, (row) => {
      if (row.closest("thead")) return true;
      if (!hasSelection) return true;
      return selected.has(Number(row.dataset.id));
    });
    downloadCsv(lines, hasSelection ? "personel-secili" : "personel-listesi");
    showToast("CSV raporu indirildi.", "success");
  };

  const deactivateSelected = async () => {
    const count = selected.size;
    try {
      await bulkDeactivate.mutateAsync([...selected]);
      showToast(`${count} personel pasife alındı.`, "success");
      setSelected(new Set());
    } catch {
      showToast("Pasife alma başarısız oldu.", "error");
    }
  };

  return (
    <div id="personnel-screen">
      <div id="list-screen">
        <div className="page-header">
          <div>
            <h2>Personel Yönetimi</h2>
            <p>Sicil, özlük, iletişim ve kurumsal bilgileri tek ekrandan yönetin.</p>
          </div>
          {isHrAdmin && (
            <button className="btn btn-primary" onClick={() => setCardId(null)}>
              <i aria-hidden="true" className="fa-solid fa-plus" /> Yeni Personel
            </button>
          )}
        </div>

        <div className="filter-bar surface">
          <div className="search-wrapper">
            <i aria-hidden="true" className="fa-solid fa-magnifying-glass" />
            <label className="sr-only" htmlFor="personnel-search">Personel ara</label>
            <input
              id="personnel-search"
              type="text"
              className="search-input"
              placeholder="Ad, departman veya görev ara"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <label className="sr-only" htmlFor="personnel-dept-filter">Departman filtresi</label>
          <select
            id="personnel-dept-filter"
            className="filter-select"
            value={filters.departmentId}
            onChange={(e) => setFilters((c) => ({ ...c, departmentId: e.target.value }))}
          >
            <option value="">Departman: Tümü</option>
            {departments.map((dept) => (
              <option key={dept.id} value={dept.id}>{dept.name}</option>
            ))}
          </select>
          <label className="sr-only" htmlFor="personnel-status-filter">Durum filtresi</label>
          <select
            id="personnel-status-filter"
            className="filter-select"
            value={filters.status}
            onChange={(e) => setFilters((c) => ({ ...c, status: e.target.value }))}
          >
            <option value="">Durum: Tümü</option>
            <option value="active">Aktif</option>
            <option value="passive">Pasif</option>
          </select>
          <button className="btn btn-secondary" onClick={exportCsv}>
            <i aria-hidden="true" className="fa-solid fa-file-excel" /> Dışa Aktar
          </button>
        </div>

        <div id="personnel-bulk-bar" className="bulk-bar surface" hidden={selected.size === 0}>
          <strong id="personnel-bulk-count">{selected.size} kişi seçildi</strong>
          <div className="toolbar-actions">
            <button className="btn btn-secondary btn-sm" onClick={exportCsv}>
              <i aria-hidden="true" className="fa-solid fa-file-excel" /> Seçilenleri dışa aktar
            </button>
            {isHrAdmin && (
              <button className="btn btn-secondary btn-sm" onClick={deactivateSelected}>
                <i aria-hidden="true" className="fa-solid fa-user-slash" /> Pasife al
              </button>
            )}
            <button className="btn btn-ghost btn-sm" onClick={() => setSelected(new Set())}>Seçimi temizle</button>
          </div>
        </div>

        <div className="table-container">
          <table className="pro-table" id="personnel-table" ref={tableRef}>
            <thead>
              <tr>
                <th className="csv-skip check-col">
                  <input
                    type="checkbox"
                    aria-label="Tüm personeli seç"
                    checked={allVisibleSelected}
                    onChange={(e) => toggleAll(e.target.checked)}
                  />
                </th>
                <th>Personel</th>
                <th>Departman</th>
                <th>TC Kimlik</th>
                <th>İşe Giriş</th>
                <th>Durum</th>
                <th style={{ textAlign: "right" }} className="csv-skip">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {items.map((employee) => (
                <tr key={employee.id} data-id={employee.id}>
                  <td className="csv-skip check-col">
                    <input
                      type="checkbox"
                      className="personnel-row-check"
                      aria-label={`${employee.name} kaydını seç`}
                      checked={selected.has(employee.id ?? -1)}
                      onChange={(e) => toggleOne(employee.id ?? -1, e.target.checked)}
                    />
                  </td>
                  <td>
                    <div className="user-meta">
                      <div className="avatar-sm" aria-hidden="true">{employee.initials}</div>
                      <div className="meta-info">
                        <strong>{employee.name}</strong>
                        <small>{employee.title}</small>
                      </div>
                    </div>
                  </td>
                  <td><strong>{employee.department}</strong></td>
                  <td className="mono">{employee.nationalIdMasked}</td>
                  <td>{formatDate(employee.hireDate)}</td>
                  <td>
                    <span className={`badge badge-${employee.status}`}>
                      {employee.status === "active" ? "Aktif" : "Pasif"}
                    </span>
                  </td>
                  <td style={{ textAlign: "right" }} className="csv-skip">
                    <div className="row-actions">
                      <button className="btn-icon-sm" title="Görüntüle" aria-label={`${employee.name} kartını görüntüle`} onClick={() => setCardId(employee.id ?? -1)}>
                        <i aria-hidden="true" className="fa-regular fa-eye" />
                      </button>
                      {isHrAdmin && (
                        <button className="btn-icon-sm" title="Düzenle" aria-label={`${employee.name} kaydını düzenle`} onClick={() => setCardId(employee.id ?? -1)}>
                          <i aria-hidden="true" className="fa-solid fa-pen" />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div id="personnel-empty" className="empty-state" hidden={items.length !== 0}>
            <i aria-hidden="true" className="fa-solid fa-user-slash" />
            <h3>Eşleşen personel bulunamadı</h3>
            <p>Arama veya filtre ölçütlerini değiştirerek yeniden deneyin.</p>
          </div>
        </div>
      </div>

      {/* Personel kartı modalı Task 4'te: cardId === undefined → kapalı */}
      {cardId !== undefined && null}
    </div>
  );
}
```

- [x] **Step 3: `routes.tsx` `pageFor`'a ekle** (`personnel: PersonnelPage` + import)

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS.

- [x] **Step 5: Commit**

```bash
git add frontend/src/
git commit -m "feat(frontend): personel listesi — server-side filtre, toplu seçim, CSV, rol bazlı eylemler"
```

---

### Task 4: Personel kartı modalı — 6 sekme + gerçek CRUD

**Files:**
- Create: `frontend/src/features/personnel/PersonnelModal.tsx`
- Modify: `frontend/src/features/personnel/PersonnelPage.tsx` (modal bağla)
- Test: `frontend/src/features/personnel/PersonnelModal.test.tsx`

**Interfaces:**
- Consumes: `useEmployee`, `useSaveEmployee`, `useUploadPhoto`, `useDepartments`, `useToast`, `ApiError`.
- Produces: `PersonnelModal({ employeeId, readOnly, onClose }: { employeeId: number | null; readOnly: boolean; onClose: () => void })` — `employeeId === null` yeni kayıt; `readOnly` manager görünümü (inputlar disabled, Kaydet yok). Evrak sekmesi Task 5'te doldurulacak (bu görevde sekme + dropzone görseli + "önce kaydedin" notu).

Form state tek nesnede tutulur (`FormState`), alan eşlemesi:

| Sekme | Alan → model |
|---|---|
| Kimlik | nationalId*, birthDate, firstName, lastName, gender (Erkek/Kadın), maritalStatus (Evli/Bekar), bloodType (0 Rh+/A Rh+/B Rh+) → `profile.*`, kök `nationalId/firstName/lastName` |
| İletişim | mobilePhone, personalEmail, homeAddress, emergencyContactName, emergencyContactPhone ("Yakınlık / Telefon" inputu) |
| İş | departmentId*, title, hireDate*, employmentType (Tam Zamanlı/Yarı Zamanlı/Uzaktan), rehireEligibility (Değerlendirilmedi/Çalışılabilir/Kararsız/Çalışılmaz), exitCode (Yok/Kod-03 (İstifa)) |
| Mali | iban, bankName, salaryType (Net Maaş/Brüt Maaş), pensionStatus (Otomatik Katılım/İptal/Muaf), mealCard |
| Özlük | tshirtSize (S/M/L/XL), pantsSize (30/32/34), coatSize (M/L), shoeSize, canWorkAtHeight, canWorkNightShift, canLiftHeavyLoads, healthNotes |

- [x] **Step 1: Başarısız testleri yaz** — `PersonnelModal.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { PersonnelModal } from "./PersonnelModal";

const departments = [{ id: 1, name: "Yazılım", code: "YZL", employeeCount: 1 }];
const detail = {
  id: 1, firstName: "Ahmet", lastName: "Yılmaz", name: "Ahmet Yılmaz", initials: "AY",
  title: "Senior Developer", nationalId: "12345678901", status: "active", hireDate: "2021-03-12",
  departmentId: 1, department: "Yazılım", managerId: null, managerName: null,
  profile: { birthDate: "1990-01-01", gender: "Erkek", maritalStatus: "Evli", bloodType: "0 Rh+", mobilePhone: "(532) 111 11 11", personalEmail: "a@a", homeAddress: "İstanbul", emergencyContactName: "Ayşe Yılmaz", emergencyContactRelation: null, emergencyContactPhone: "0533", employmentType: "Tam Zamanlı", rehireEligibility: null, exitCode: null, iban: "TR11", bankName: "Banka", salaryType: "Net Maaş", pensionStatus: "Otomatik Katılım", mealCard: "Multinet", tshirtSize: "L", pantsSize: "32", coatSize: "L", shoeSize: "42", canWorkAtHeight: true, canWorkNightShift: false, canLiftHeavyLoads: false, healthNotes: "" },
  documents: [],
};

beforeEach(() => {
  localStorage.clear();
  stubApi({ "/api/departments": departments, "/api/employees/1": detail, "/api/employees": detail });
});
afterEach(() => vi.unstubAllGlobals());

const renderModal = (employeeId: number | null, readOnly = false) =>
  renderPage(
    <ToastProvider>
      <PersonnelModal employeeId={employeeId} readOnly={readOnly} onClose={() => {}} />
    </ToastProvider>,
  );

test("düzenleme modunda alanlar mevcut kayıtla dolar", async () => {
  renderModal(1);
  expect(await screen.findByText("Personel Kartı — Ahmet Yılmaz")).toBeInTheDocument();
  expect(screen.getByLabelText("TC Kimlik No *")).toHaveValue("12345678901");
  expect(screen.getByLabelText("Adı")).toHaveValue("Ahmet");
});

test("sekme değişimi içerik bölümünü değiştirir", async () => {
  renderModal(1);
  await screen.findByText("Personel Kartı — Ahmet Yılmaz");
  await userEvent.click(screen.getByRole("button", { name: /Mali Bilgiler/ }));
  expect(screen.getByLabelText("IBAN Numarası")).toBeVisible();
});

test("yeni kayıt kaydedilince POST body doğru kurulur", async () => {
  renderModal(null);
  await screen.findByText("Yeni Personel Kartı");
  await userEvent.type(screen.getByLabelText("TC Kimlik No *"), "98765432109");
  await userEvent.type(screen.getByLabelText("Adı"), "Yeni");
  await userEvent.type(screen.getByLabelText("Soyadı"), "Kişi");
  await userEvent.click(screen.getByRole("button", { name: /İş & Kurumsal/ }));
  await userEvent.type(screen.getByLabelText("Ünvan / Görev"), "Uzman");
  const hire = screen.getByLabelText("İşe Giriş Tarihi");
  await userEvent.clear(hire);
  await userEvent.type(hire, "2026-07-14");
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/employees" && i?.method === "POST",
    );
    expect(posted).toBeTruthy();
    const body = JSON.parse(String(posted![1]?.body));
    expect(body.nationalId).toBe("98765432109");
    expect(body.firstName).toBe("Yeni");
    expect(body.profile.employmentType).toBe("Tam Zamanlı");
  });
});

test("salt-okur modda inputlar disabled, Kaydet yok", async () => {
  renderModal(1, true);
  await screen.findByText("Personel Kartı — Ahmet Yılmaz");
  expect(screen.getByLabelText("Adı")).toBeDisabled();
  expect(screen.queryByRole("button", { name: /Kaydet/ })).not.toBeInTheDocument();
});
```

Run: `npm test -- --run src/features/personnel/PersonnelModal.test.tsx` → FAIL.

- [x] **Step 2: `PersonnelModal.tsx` yaz**

Eski modal markup'ı birebir (fullscreen-modal, modal-sidebar nav-btn'ler, content-section'lar, form-grid col-* sınıfları). Kontrollü inputlar; her `input-label`e `htmlFor` ve input'a `id` eklenir (`pm-` öneki) — eski DOM'da label-for yoktu, erişilebilirlik eklemesi görsel farksızdır.

```tsx
import { useEffect, useRef, useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { PageError, PageLoading } from "../shared/PageState";
import { useDepartments, useEmployee, useSaveEmployee, useUploadPhoto, type EmployeeDetailDto, type EmployeeUpsertModel } from "./queries";

type TabId = "tab-kimlik" | "tab-iletisim" | "tab-is" | "tab-mali" | "tab-ozluk" | "tab-evrak";

const TABS: { id: TabId; icon: string; label: string }[] = [
  { id: "tab-kimlik", icon: "fa-regular fa-id-card", label: "Kimlik Bilgileri" },
  { id: "tab-iletisim", icon: "fa-solid fa-phone", label: "İletişim & Adres" },
  { id: "tab-is", icon: "fa-solid fa-briefcase", label: "İş & Kurumsal" },
  { id: "tab-mali", icon: "fa-solid fa-wallet", label: "Mali Bilgiler" },
  { id: "tab-ozluk", icon: "fa-solid fa-shield-heart", label: "Özlük & Sağlık" },
  { id: "tab-evrak", icon: "fa-solid fa-folder-tree", label: "Evraklar" },
];

type FormState = {
  nationalId: string; birthDate: string; firstName: string; lastName: string;
  gender: string; maritalStatus: string; bloodType: string;
  mobilePhone: string; personalEmail: string; homeAddress: string;
  emergencyContactName: string; emergencyContactPhone: string;
  departmentId: string; title: string; hireDate: string; employmentType: string;
  rehireEligibility: string; exitCode: string;
  iban: string; bankName: string; salaryType: string; pensionStatus: string; mealCard: string;
  tshirtSize: string; pantsSize: string; coatSize: string; shoeSize: string;
  canWorkAtHeight: boolean; canWorkNightShift: boolean; canLiftHeavyLoads: boolean; healthNotes: string;
};

const emptyForm: FormState = {
  nationalId: "", birthDate: "", firstName: "", lastName: "",
  gender: "Erkek", maritalStatus: "Evli", bloodType: "0 Rh+",
  mobilePhone: "", personalEmail: "", homeAddress: "",
  emergencyContactName: "", emergencyContactPhone: "",
  departmentId: "", title: "", hireDate: "", employmentType: "Tam Zamanlı",
  rehireEligibility: "Değerlendirilmedi", exitCode: "Yok",
  iban: "", bankName: "", salaryType: "Net Maaş", pensionStatus: "Otomatik Katılım", mealCard: "",
  tshirtSize: "M", pantsSize: "32", coatSize: "M", shoeSize: "42",
  canWorkAtHeight: false, canWorkNightShift: false, canLiftHeavyLoads: false, healthNotes: "",
};

const formFrom = (detail: EmployeeDetailDto): FormState => ({
  ...emptyForm,
  nationalId: detail.nationalId ?? "",
  firstName: detail.firstName ?? "",
  lastName: detail.lastName ?? "",
  departmentId: String(detail.departmentId ?? ""),
  title: detail.title ?? "",
  hireDate: detail.hireDate ?? "",
  birthDate: detail.profile?.birthDate ?? "",
  gender: detail.profile?.gender ?? emptyForm.gender,
  maritalStatus: detail.profile?.maritalStatus ?? emptyForm.maritalStatus,
  bloodType: detail.profile?.bloodType ?? emptyForm.bloodType,
  mobilePhone: detail.profile?.mobilePhone ?? "",
  personalEmail: detail.profile?.personalEmail ?? "",
  homeAddress: detail.profile?.homeAddress ?? "",
  emergencyContactName: detail.profile?.emergencyContactName ?? "",
  emergencyContactPhone: detail.profile?.emergencyContactPhone ?? "",
  employmentType: detail.profile?.employmentType ?? emptyForm.employmentType,
  rehireEligibility: detail.profile?.rehireEligibility ?? emptyForm.rehireEligibility,
  exitCode: detail.profile?.exitCode ?? emptyForm.exitCode,
  iban: detail.profile?.iban ?? "",
  bankName: detail.profile?.bankName ?? "",
  salaryType: detail.profile?.salaryType ?? emptyForm.salaryType,
  pensionStatus: detail.profile?.pensionStatus ?? emptyForm.pensionStatus,
  mealCard: detail.profile?.mealCard ?? "",
  tshirtSize: detail.profile?.tshirtSize ?? emptyForm.tshirtSize,
  pantsSize: detail.profile?.pantsSize ?? emptyForm.pantsSize,
  coatSize: detail.profile?.coatSize ?? emptyForm.coatSize,
  shoeSize: detail.profile?.shoeSize ?? emptyForm.shoeSize,
  canWorkAtHeight: detail.profile?.canWorkAtHeight ?? false,
  canWorkNightShift: detail.profile?.canWorkNightShift ?? false,
  canLiftHeavyLoads: detail.profile?.canLiftHeavyLoads ?? false,
  healthNotes: detail.profile?.healthNotes ?? "",
});

const modelFrom = (form: FormState, existing: EmployeeDetailDto | undefined): EmployeeUpsertModel => ({
  firstName: form.firstName,
  lastName: form.lastName,
  title: form.title,
  departmentId: Number(form.departmentId) || undefined,
  hireDate: form.hireDate || undefined,
  nationalId: form.nationalId,
  managerId: existing?.managerId ?? null,
  status: existing?.status ?? "active",
  profile: {
    birthDate: form.birthDate || null,
    gender: form.gender, maritalStatus: form.maritalStatus, bloodType: form.bloodType,
    mobilePhone: form.mobilePhone || null, personalEmail: form.personalEmail || null,
    homeAddress: form.homeAddress || null,
    emergencyContactName: form.emergencyContactName || null,
    emergencyContactRelation: null,
    emergencyContactPhone: form.emergencyContactPhone || null,
    employmentType: form.employmentType,
    rehireEligibility: form.rehireEligibility, exitCode: form.exitCode,
    iban: form.iban || null, bankName: form.bankName || null,
    salaryType: form.salaryType, pensionStatus: form.pensionStatus,
    mealCard: form.mealCard || null,
    tshirtSize: form.tshirtSize, pantsSize: form.pantsSize, coatSize: form.coatSize,
    shoeSize: form.shoeSize,
    canWorkAtHeight: form.canWorkAtHeight, canWorkNightShift: form.canWorkNightShift,
    canLiftHeavyLoads: form.canLiftHeavyLoads,
    healthNotes: form.healthNotes || null,
  },
});

export function PersonnelModal({ employeeId, readOnly, onClose }:
  { employeeId: number | null; readOnly: boolean; onClose: () => void }) {
  const { showToast } = useToast();
  const [activeTab, setActiveTab] = useState<TabId>("tab-kimlik");
  const [form, setForm] = useState<FormState>(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const photoInputRef = useRef<HTMLInputElement>(null);

  const detailQ = useEmployee(employeeId);
  const departmentsQ = useDepartments();
  const save = useSaveEmployee();
  const uploadPhoto = useUploadPhoto();

  useEffect(() => {
    if (detailQ.data) setForm(formFrom(detailQ.data));
  }, [detailQ.data]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((current) => ({ ...current, [key]: value }));

  const isEdit = employeeId !== null;
  const title = isEdit && detailQ.data ? `Personel Kartı — ${detailQ.data.name}` : "Yeni Personel Kartı";
  const description = isEdit
    ? "Kayıtlı özlük bilgilerini görüntüleyin ve güncelleyin."
    : "Gerekli alanları tamamlayarak özlük kaydını oluşturun.";

  const handleSave = async () => {
    setError(null);
    try {
      await save.mutateAsync({ id: employeeId, model: modelFrom(form, detailQ.data) });
      showToast(isEdit ? "Personel kaydı güncellendi." : "Personel kaydı başarıyla oluşturuldu.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
    }
  };

  const handlePhoto = async (file: File | undefined) => {
    if (!file || employeeId === null) return;
    try {
      await uploadPhoto.mutateAsync({ id: employeeId, file });
      showToast("Fotoğraf yüklendi.", "success");
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Fotoğraf yüklenemedi.", "error");
    }
  };

  if (isEdit && detailQ.isPending) return <div className="fullscreen-modal" style={{ display: "flex" }}><PageLoading /></div>;
  if (isEdit && detailQ.isError) return <div className="fullscreen-modal" style={{ display: "flex" }}><PageError error={detailQ.error} /></div>;

  return (
    <div id="personnel-modal" className="fullscreen-modal" style={{ display: "flex" }}>
      <div className="modal-header">
        <div>
          <h2>{title}</h2>
          <p>{description}</p>
          {error && <p className="form-error" role="alert">{error}</p>}
        </div>
        <div className="modal-actions">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          {!readOnly && (
            <button className="btn btn-primary" onClick={handleSave} disabled={save.isPending}>
              <i aria-hidden="true" className="fa-solid fa-check" /> Kaydet
            </button>
          )}
        </div>
      </div>

      <div className="modal-body">
        <aside className="modal-sidebar">
          {TABS.map((tab) => (
            <button key={tab.id} className={`nav-btn ${activeTab === tab.id ? "active" : ""}`} onClick={() => setActiveTab(tab.id)}>
              <i aria-hidden="true" className={tab.icon} /> {tab.label}
            </button>
          ))}
        </aside>

        <main className="modal-content-area">
          <div id="tab-kimlik" className={`content-section ${activeTab === "tab-kimlik" ? "active" : ""}`}>
            <div className="section-head">
              <div>
                <h3>Kimlik & Kişisel Bilgiler</h3>
                <span>Nüfus bilgilerini resmi evraklarla uyumlu girin.</span>
              </div>
            </div>
            <div className="form-grid">
              <div className="photo-upload col-12">
                <div className="photo-preview"><i aria-hidden="true" className="fa-solid fa-user" /></div>
                <div>
                  <button
                    type="button"
                    className="upload-btn"
                    onClick={() => {
                      if (readOnly) return;
                      if (employeeId === null) { showToast("Fotoğraf için önce kaydı oluşturun.", "info"); return; }
                      photoInputRef.current?.click();
                    }}
                  >
                    Fotoğraf Yükle
                  </button>
                  <small>JPG/PNG, maksimum 2 MB</small>
                  <input ref={photoInputRef} type="file" accept="image/jpeg,image/png" hidden onChange={(e) => handlePhoto(e.target.files?.[0])} />
                </div>
              </div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-tc">TC Kimlik No *</label>
                <input id="pm-tc" type="text" className="input-control" maxLength={11} placeholder="11 haneli numara" value={form.nationalId} disabled={readOnly} onChange={(e) => set("nationalId", e.target.value)} />
              </div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-birth">Doğum Tarihi</label>
                <input id="pm-birth" type="date" className="input-control" value={form.birthDate} disabled={readOnly} onChange={(e) => set("birthDate", e.target.value)} />
              </div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-first">Adı</label>
                <input id="pm-first" type="text" className="input-control" value={form.firstName} disabled={readOnly} onChange={(e) => set("firstName", e.target.value)} />
              </div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-last">Soyadı</label>
                <input id="pm-last" type="text" className="input-control" value={form.lastName} disabled={readOnly} onChange={(e) => set("lastName", e.target.value)} />
              </div>
              <div className="input-group col-4">
                <label className="input-label" htmlFor="pm-gender">Cinsiyet</label>
                <select id="pm-gender" className="input-control" value={form.gender} disabled={readOnly} onChange={(e) => set("gender", e.target.value)}>
                  <option>Erkek</option><option>Kadın</option>
                </select>
              </div>
              <div className="input-group col-4">
                <label className="input-label" htmlFor="pm-marital">Medeni Durum</label>
                <select id="pm-marital" className="input-control" value={form.maritalStatus} disabled={readOnly} onChange={(e) => set("maritalStatus", e.target.value)}>
                  <option>Evli</option><option>Bekar</option>
                </select>
              </div>
              <div className="input-group col-4">
                <label className="input-label" htmlFor="pm-blood">Kan Grubu</label>
                <select id="pm-blood" className="input-control" value={form.bloodType} disabled={readOnly} onChange={(e) => set("bloodType", e.target.value)}>
                  <option>0 Rh+</option><option>A Rh+</option><option>B Rh+</option>
                </select>
              </div>
            </div>
          </div>

          <div id="tab-iletisim" className={`content-section ${activeTab === "tab-iletisim" ? "active" : ""}`}>
            <div className="section-head"><div><h3>İletişim Bilgileri</h3><span>Personelin ulaşılabilir iletişim kanalları.</span></div></div>
            <div className="form-grid">
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-phone">Cep Telefonu</label><input id="pm-phone" type="tel" className="input-control" placeholder="(5XX) ..." value={form.mobilePhone} disabled={readOnly} onChange={(e) => set("mobilePhone", e.target.value)} /></div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-email">Kişisel E-Posta</label><input id="pm-email" type="email" className="input-control" value={form.personalEmail} disabled={readOnly} onChange={(e) => set("personalEmail", e.target.value)} /></div>
              <div className="input-group col-12"><label className="input-label" htmlFor="pm-address">Ev Adresi</label><textarea id="pm-address" className="input-control" rows={3} value={form.homeAddress} disabled={readOnly} onChange={(e) => set("homeAddress", e.target.value)} /></div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-emg-name">Acil Durum Kişisi</label><input id="pm-emg-name" type="text" className="input-control" placeholder="Ad Soyad" value={form.emergencyContactName} disabled={readOnly} onChange={(e) => set("emergencyContactName", e.target.value)} /></div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-emg-phone">Yakınlık / Telefon</label><input id="pm-emg-phone" type="text" className="input-control" placeholder="Örn: Eşi - 0532..." value={form.emergencyContactPhone} disabled={readOnly} onChange={(e) => set("emergencyContactPhone", e.target.value)} /></div>
            </div>
          </div>

          <div id="tab-is" className={`content-section ${activeTab === "tab-is" ? "active" : ""}`}>
            <div className="section-head"><div><h3>Kurumsal Bilgiler</h3><span>Pozisyon, çalışma şekli ve organizasyon bilgileri.</span></div></div>
            <div className="form-grid">
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-dept">Departman</label>
                <select id="pm-dept" className="input-control" value={form.departmentId} disabled={readOnly} onChange={(e) => set("departmentId", e.target.value)}>
                  <option value="">Seçin</option>
                  {(departmentsQ.data ?? []).map((dept) => <option key={dept.id} value={dept.id}>{dept.name}</option>)}
                </select>
              </div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-title">Ünvan / Görev</label><input id="pm-title" type="text" className="input-control" value={form.title} disabled={readOnly} onChange={(e) => set("title", e.target.value)} /></div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-hire">İşe Giriş Tarihi</label><input id="pm-hire" type="date" className="input-control" value={form.hireDate} disabled={readOnly} onChange={(e) => set("hireDate", e.target.value)} /></div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-employment">Çalışma Şekli</label>
                <select id="pm-employment" className="input-control" value={form.employmentType} disabled={readOnly} onChange={(e) => set("employmentType", e.target.value)}>
                  <option>Tam Zamanlı</option><option>Yarı Zamanlı</option><option>Uzaktan</option>
                </select>
              </div>
              <div className="notice-card col-12">
                <strong>Önceki çalışma geçmişi</strong>
                <p>Yeniden işe alım kararları için değerlendirme notu bırakın.</p>
                <div className="form-grid-2">
                  <div className="input-group">
                    <label className="input-label" htmlFor="pm-rehire">Tekrar Çalışma Durumu</label>
                    <select id="pm-rehire" className="input-control" value={form.rehireEligibility} disabled={readOnly} onChange={(e) => set("rehireEligibility", e.target.value)}>
                      <option>Değerlendirilmedi</option><option>Çalışılabilir</option><option>Kararsız</option><option>Çalışılmaz</option>
                    </select>
                  </div>
                  <div className="input-group">
                    <label className="input-label" htmlFor="pm-exit">Eski Çıkış Kodu</label>
                    <select id="pm-exit" className="input-control" value={form.exitCode} disabled={readOnly} onChange={(e) => set("exitCode", e.target.value)}>
                      <option>Yok</option><option>Kod-03 (İstifa)</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div id="tab-mali" className={`content-section ${activeTab === "tab-mali" ? "active" : ""}`}>
            <div className="section-head"><div><h3>Mali & Yan Haklar</h3><span>Banka, BES ve yan hak tanımlamaları.</span></div></div>
            <div className="form-grid">
              <div className="input-group col-12"><label className="input-label" htmlFor="pm-iban">IBAN Numarası</label><input id="pm-iban" type="text" className="input-control mono" placeholder="TR..." value={form.iban} disabled={readOnly} onChange={(e) => set("iban", e.target.value)} /></div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-bank">Banka Adı</label><input id="pm-bank" type="text" className="input-control" value={form.bankName} disabled={readOnly} onChange={(e) => set("bankName", e.target.value)} /></div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-salary">Maaş Tipi</label>
                <select id="pm-salary" className="input-control" value={form.salaryType} disabled={readOnly} onChange={(e) => set("salaryType", e.target.value)}>
                  <option>Net Maaş</option><option>Brüt Maaş</option>
                </select>
              </div>
              <div className="input-group col-6">
                <label className="input-label" htmlFor="pm-pension">BES Durumu</label>
                <select id="pm-pension" className="input-control" value={form.pensionStatus} disabled={readOnly} onChange={(e) => set("pensionStatus", e.target.value)}>
                  <option>Otomatik Katılım</option><option>İptal</option><option>Muaf</option>
                </select>
              </div>
              <div className="input-group col-6"><label className="input-label" htmlFor="pm-meal">Yemek Kartı</label><input id="pm-meal" type="text" className="input-control" value={form.mealCard} disabled={readOnly} onChange={(e) => set("mealCard", e.target.value)} /></div>
            </div>
          </div>

          <div id="tab-ozluk" className={`content-section ${activeTab === "tab-ozluk" ? "active" : ""}`}>
            <div className="section-head"><div><h3>Özlük & Sağlık</h3><span>Zimmet ve İSG süreçleri için ek bilgiler.</span></div></div>
            <div className="form-grid">
              <div className="input-group col-3">
                <label className="input-label" htmlFor="pm-tshirt">T-Shirt</label>
                <select id="pm-tshirt" className="input-control" value={form.tshirtSize} disabled={readOnly} onChange={(e) => set("tshirtSize", e.target.value)}>
                  <option>S</option><option>M</option><option>L</option><option>XL</option>
                </select>
              </div>
              <div className="input-group col-3">
                <label className="input-label" htmlFor="pm-pants">Pantolon</label>
                <select id="pm-pants" className="input-control" value={form.pantsSize} disabled={readOnly} onChange={(e) => set("pantsSize", e.target.value)}>
                  <option>30</option><option>32</option><option>34</option>
                </select>
              </div>
              <div className="input-group col-3">
                <label className="input-label" htmlFor="pm-coat">Mont</label>
                <select id="pm-coat" className="input-control" value={form.coatSize} disabled={readOnly} onChange={(e) => set("coatSize", e.target.value)}>
                  <option>M</option><option>L</option>
                </select>
              </div>
              <div className="input-group col-3"><label className="input-label" htmlFor="pm-shoe">Ayakkabı</label><input id="pm-shoe" type="number" className="input-control" value={form.shoeSize} disabled={readOnly} onChange={(e) => set("shoeSize", e.target.value)} /></div>
              <label className="check-card col-4"><input type="checkbox" checked={form.canWorkAtHeight} disabled={readOnly} onChange={(e) => set("canWorkAtHeight", e.target.checked)} /> <span>Yüksekte çalışabilir</span></label>
              <label className="check-card col-4"><input type="checkbox" checked={form.canWorkNightShift} disabled={readOnly} onChange={(e) => set("canWorkNightShift", e.target.checked)} /> <span>Gece vardiyası</span></label>
              <label className="check-card col-4"><input type="checkbox" checked={form.canLiftHeavyLoads} disabled={readOnly} onChange={(e) => set("canLiftHeavyLoads", e.target.checked)} /> <span>Ağır yük taşıma</span></label>
              <div className="input-group col-12"><label className="input-label" htmlFor="pm-health">Bilinen Hastalık / Notlar</label><textarea id="pm-health" className="input-control" rows={2} value={form.healthNotes} disabled={readOnly} onChange={(e) => set("healthNotes", e.target.value)} /></div>
            </div>
          </div>

          <div id="tab-evrak" className={`content-section ${activeTab === "tab-evrak" ? "active" : ""}`}>
            {/* Task 5: gerçek evrak listesi + yükleme/indirme */}
            <div className="upload-drop">
              <i aria-hidden="true" className="fa-solid fa-cloud-arrow-up" />
              <h4>Dosyaları sürükleyip bırakın</h4>
              <p>Nüfus cüzdanı, ikametgah, adli sicil ve diğer özlük evrakları.</p>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
```

- [x] **Step 3: `PersonnelPage.tsx`'e modalı bağla** — `{cardId !== undefined && null}` satırını değiştir:

```tsx
      {cardId !== undefined && (
        <PersonnelModal
          employeeId={cardId}
          readOnly={!isHrAdmin}
          onClose={() => setCardId(undefined)}
        />
      )}
```

`import { PersonnelModal } from "./PersonnelModal";` ekle.

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS.

- [x] **Step 5: Commit**

```bash
git add frontend/src/features/personnel/
git commit -m "feat(frontend): personel kartı modalı — 6 sekme, gerçek CRUD, foto yükleme"
```

---

### Task 5: Evrak sekmesi — liste + yükleme + indirme

**Files:**
- Create: `frontend/src/features/personnel/DocumentsTab.tsx`
- Modify: `frontend/src/features/personnel/PersonnelModal.tsx` (tab-evrak içeriği)
- Test: `frontend/src/features/personnel/DocumentsTab.test.tsx`

**Interfaces:**
- Consumes: `useEmployeeDocuments`, `useUploadDocument`, `apiDownload`, `useToast`.
- Produces: `DocumentsTab({ employeeId, readOnly }: { employeeId: number | null; readOnly: boolean })`.

- [x] **Step 1: Başarısız testleri yaz** — `DocumentsTab.test.tsx`:

```tsx
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
```

Run: `npm test -- --run src/features/personnel/DocumentsTab.test.tsx` → FAIL.

- [x] **Step 2: `DocumentsTab.tsx` yaz**

```tsx
import { useRef, useState } from "react";
import { ApiError, apiDownload } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { useEmployeeDocuments, useUploadDocument } from "./queries";

const formatSize = (bytes?: number): string =>
  bytes === undefined ? "" : bytes >= 1024 * 1024 ? `${(bytes / (1024 * 1024)).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;

export function DocumentsTab({ employeeId, readOnly }: { employeeId: number | null; readOnly: boolean }) {
  const { showToast } = useToast();
  const documentsQ = useEmployeeDocuments(employeeId);
  const upload = useUploadDocument();
  const [documentType, setDocumentType] = useState("Özlük Evrakı");
  const fileInputRef = useRef<HTMLInputElement>(null);

  if (employeeId === null) {
    return (
      <div className="upload-drop">
        <i aria-hidden="true" className="fa-solid fa-cloud-arrow-up" />
        <h4>Dosyaları sürükleyip bırakın</h4>
        <p>Evrak yüklemek için önce personel kaydını oluşturun.</p>
      </div>
    );
  }

  const handleUpload = async (file: File | undefined) => {
    if (!file) return;
    try {
      await upload.mutateAsync({ id: employeeId, file, documentType });
      showToast("Evrak yüklendi.", "success");
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Evrak yüklenemedi.", "error");
    }
  };

  const handleDownload = async (documentId: number, fallbackName: string) => {
    try {
      const { blob, fileName } = await apiDownload(`/employees/${employeeId}/documents/${documentId}`);
      const link = document.createElement("a");
      link.href = URL.createObjectURL(blob);
      link.download = fileName ?? fallbackName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(link.href);
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Evrak indirilemedi.", "error");
    }
  };

  return (
    <>
      {!readOnly && (
        <div
          className="upload-drop"
          onClick={() => fileInputRef.current?.click()}
          onDragOver={(e) => e.preventDefault()}
          onDrop={(e) => {
            e.preventDefault();
            handleUpload(e.dataTransfer.files?.[0]);
          }}
        >
          <i aria-hidden="true" className="fa-solid fa-cloud-arrow-up" />
          <h4>Dosyaları sürükleyip bırakın</h4>
          <p>Nüfus cüzdanı, ikametgah, adli sicil ve diğer özlük evrakları.</p>
        </div>
      )}
      {!readOnly && (
        <div className="form-grid">
          <div className="input-group col-6">
            <label className="input-label" htmlFor="pm-doc-type">Evrak Türü</label>
            <input id="pm-doc-type" type="text" className="input-control" value={documentType} onChange={(e) => setDocumentType(e.target.value)} />
          </div>
          <div className="input-group col-6">
            <label className="input-label" htmlFor="pm-doc-file">Evrak dosyası seç</label>
            <input id="pm-doc-file" ref={fileInputRef} type="file" className="input-control" aria-label="Evrak dosyası seç" onChange={(e) => handleUpload(e.target.files?.[0])} />
          </div>
        </div>
      )}

      <div className="table-container">
        <table className="detail-table data-table">
          <thead>
            <tr><th>Evrak Türü</th><th>Dosya</th><th>Boyut</th><th>Yüklenme</th><th style={{ textAlign: "right" }}>İşlem</th></tr>
          </thead>
          <tbody>
            {(documentsQ.data ?? []).map((doc) => (
              <tr key={doc.id}>
                <td><strong>{doc.documentType}</strong></td>
                <td>{doc.fileName}</td>
                <td>{formatSize(doc.sizeBytes)}</td>
                <td>{doc.createdAtUtc ? new Date(doc.createdAtUtc).toLocaleDateString("tr-TR") : ""}</td>
                <td style={{ textAlign: "right" }}>
                  <button className="btn-icon-sm" title="İndir" aria-label={`${doc.fileName} dosyasını indir`} onClick={() => handleDownload(doc.id ?? 0, doc.fileName ?? "evrak")}>
                    <i aria-hidden="true" className="fa-solid fa-download" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {(documentsQ.data ?? []).length === 0 && (
          <div className="empty-state">
            <i aria-hidden="true" className="fa-regular fa-folder-open" />
            <h3>Henüz evrak yok</h3>
            <p>Bu personel için yüklenmiş özlük evrakı bulunmuyor.</p>
          </div>
        )}
      </div>
    </>
  );
}
```

- [x] **Step 3: `PersonnelModal.tsx` tab-evrak içeriğini değiştir**

```tsx
          <div id="tab-evrak" className={`content-section ${activeTab === "tab-evrak" ? "active" : ""}`}>
            <DocumentsTab employeeId={employeeId} readOnly={readOnly} />
          </div>
```

`import { DocumentsTab } from "./DocumentsTab";` ekle; eski dropzone bloğunu kaldır.

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS. `npm run build` → hatasız.

- [x] **Step 5: Commit**

```bash
git add frontend/src/features/personnel/
git commit -m "feat(frontend): özlük evrakları — liste, yükleme (FormData) ve indirme"
```

---

### Task 6: Uçtan uca duman testi + parite + günlük + kapanış

**Files:**
- Modify: `docs/gelistirme-gunlugu.md`; gerekirse küçük düzeltmeler.

- [x] **Step 1: Backend + frontend'i başlat** (dilim 1–2 ile aynı komutlar)

- [x] **Step 2: Duman testi (hr-admin)**

1. `#/personnel` → liste gerçek seed'le dolar (maskeli TC, rozetler).
2. Arama kutusuna bir personel adı yaz → liste daralır; departman/durum filtreleri çalışır; eşleşme yoksa boş durum bloğu.
3. Satır seç → bulk bar; "Pasife al" → rozet Pasif olur (liste yenilenir), toast.
4. "Dışa Aktar" → CSV iner (`;` ayraçlı, Türkçe karakterler doğru).
5. "Yeni Personel" → kart açılır; zorunlu alanları doldur, Kaydet → listede görünür. Aynı TC ile tekrar dene → `form-error`'da 409 mesajı.
6. Mevcut kaydı aç → alanlar dolu; bir alanı değiştir, Kaydet → güncellenir.
7. Evrak sekmesi → dosya yükle → listede görünür; indir → dosya iner.
8. Foto yükle → toast "Fotoğraf yüklendi."
9. Rol değiştirici → Yönetici → `#/personnel`: "Yeni Personel"/"Düzenle"/"Pasife al" görünmez; kart salt-okur açılır.

- [x] **Step 3: Görsel parite** — eski `#/personnel` (4173) ile yeni (5173) yan yana: filter bar, tablo, bulk bar, modal sekmeleri. Bilinçli farklar: maskeli TC, evrak sekmesindeki gerçek liste/tür alanı, foto butonunun buton olması. Başka fark → DOM/class düzeltmesi.

- [x] **Step 4: Günlük + kapanış commit**

`docs/gelistirme-gunlugu.md`: "Şu an neredeyiz" → Dilim 4 (İzin & Onay + Puantaj); Dilim 3 kaydı eklenir. Dilim 3 plan kutuları işaretlenir.

```bash
git add frontend/ docs/
git commit -m "test(frontend): dilim 3 duman testi, parite kontrolü ve günlük güncellemesi"
```

---

## Sonraki dilimler

Dilim 4 (İzin & Onay + Puantaj) planı bu dilim main'e merge edildikten sonra yazılır.
