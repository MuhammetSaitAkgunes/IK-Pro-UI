# React Port — Dilim 4: İzin & Onay + Puantaj — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** İzinlerim ekranını (`/leaves`, tüm roller) gerçek izin API'sine bağla (bakiye, talepler, talep oluşturma/iptal, ekip yokluk widget'ı), yönetim rollerine Overview'da gerçek izin onay kartını ekle ve Mesai & Puantaj ekranını (`/attendance`, MGMT) canlı pano + aylık puantaj + manuel giriş/düzenleme ile portla.

**Architecture:** `src/features/leaves/` ve `src/features/attendance/` altında sayfa + modal + `queries.ts`. CSV yardımcıları `features/shared/csv.ts`'e taşınır (personel + puantaj ortak kullanır). Leaves ve Attendance görevleri Task 1'den sonra birbirinden bağımsızdır (paralel yürütülebilir); tek ortak dosya `routes.tsx` kaydıdır.

**Tech Stack:** Dilim 1–3 stack'i (yeni bağımlılık yok).

## Global Constraints

- Dilim 1–3 planlarındaki tüm kısıtlar geçerli (CSS'e dokunma, aynı class/DOM, Türkçe metinler birebir, her görev sonunda `cd frontend && npm test -- --run` yeşil).
- Değer kümeleri backend ile birebir: izin durumu `pending|approved|rejected|cancelled`; canlı pano durumu `ontime|late|absent|early`; puantaj satır durumu `ok|late|overtime|absent`; puantaj tipi `Tam|Mesai|Rapor`.
- Testlerde gerçek ağ yok (`stubApi`); tipler `schema.d.ts`'ten.

### Veri/davranış eşleme kararları (mock ↔ backend farkları)

| Eski mock davranışı | Karar |
|---|---|
| Bakiye kartı sabit ("14/24 gün, %65") | `GET /leaves/balance`: kalan=`remainingDays`, hak ediş=`entitledDays+carriedOverDays`, bar genişliği `round(remaining/hakEdiş*100)` |
| "Kullanılan Toplam" altındaki iki pill ("6 yıllık / 3 rapor") | Tür kırılımı backend'de yok → tek pill: `{usedDays} gün kullanıldı` |
| "Onay Bekleyen" kartı sabit cümle | Bekleyen talep sayısı `my` listesinden; açıklama ilk bekleyen talepten üretilir, yoksa "Bekleyen talebiniz yok." |
| Modal izin türleri sabit 4 kart | `GET /leaves/types`'tan; ikon/ton/alt metin ad eşlemesiyle (Yıllık→sun/annual/"Kalan: X gün", Rapor→notes-medical/sick/"Belge gerekli", Mazeret→clock/excuse/"Saatlik/günlük", Uzaktan→laptop-house/remote/"Evden çalışma", diğer→sun/annual/ad) |
| `calcDays` sahte ("3 gün / 15 Eki") | Takvim günü ön izleme: `fark+1` gün, işe dönüş `bitiş+1`; kesin gün sayısını backend hesaplar (talep yanıtındaki `days`) |
| "Yerine Bakacak Kişi" sabit isimler | MGMT rollerinde `GET /employees?status=active&pageSize=50` ile dolar; employee rolünde liste ucu yok (403) → yalnız "Seçiniz..." kalır, `substituteEmployeeId` null gider |
| Overview "Bekleyen Aksiyonlar" + "Önemli Günler" (Dilim 2'de ertelendi) | "Bekleyen Aksiyonlar" kartı **gerçek** `GET /leaves/pending` + onay/red ile MGMT rollerinde geri gelir; boş kuyrukta "Bekleyen talep yok." notu. "Önemli Günler" (doğum günü) backend'de yok → eklenmez |
| Puantaj başlık istatistikleri sabit | `GET /attendance/summary?year&month` toplamları: çalışma=`Σ totalWorkedMinutes/60` saat, mesai=`Σ totalOvertimeMinutes/60` saat, geç kalma=`lateDays>0` olan kişi sayısı, devamsızlık=`Σ absentDays` gün |
| Canlı kartlar sabit 5 kişi | `GET /attendance/live` (bugün); saat `checkIn` "HH:mm", yoksa "--:--" |
| "Manuel giriş ekle" ghost kartı işlevsiz | Tıklanınca gerçek manuel giriş modalı (`POST /attendance`); satır kalemi de aynı modalla `PUT /attendance/{id}` |
| Ay değiştirici sabit 5 aylık dizi | Gerçek yıl/ay state'i (bugünden başlar, serbest ±1 ay) |
| Puantaj kişi seçici sabit 2 isim | `GET /employees?status=active&pageSize=50` (MGMT policy'si zaten var); ilk kişi otomatik seçilir |
| Mola "1s" | `breakMinutes`: 0→"-", 60'ın katı→`{m/60}s`, değilse `{m} dk` |

---

### Task 1: CSV'yi shared'a taşı + izin/puantaj query katmanları

**Files:**
- Move: `frontend/src/features/personnel/csv.ts` → `frontend/src/features/shared/csv.ts`; `csv.test.ts` de taşınır
- Modify: `frontend/src/features/personnel/PersonnelPage.tsx` (import yolu)
- Create: `frontend/src/features/leaves/queries.ts`, `frontend/src/features/attendance/queries.ts`

**Interfaces:**
- Produces:
  - `shared/csv.ts`: aynı `tableToCsvLines`, `downloadCsv` imzaları.
  - `leaves/queries.ts`:
    - Tipler: `LeaveBalanceDto`, `LeaveRequestDto`, `LeaveTypeDto`, `TeamLeaveDto`, `CreateLeaveRequestCommand` (schema'dan)
    - `useLeaveBalance()` → `GET /leaves/balance`
    - `useMyLeaves()` → `GET /leaves/my`
    - `useLeaveTypes()` → `GET /leaves/types`
    - `useTeamLeaves()` → `GET /leaves/team`
    - `useSubstituteOptions(enabled: boolean)` → `GET /employees?status=active&pageSize=50`, `select: r => r.items ?? []`
    - `useCreateLeave()` → `POST /leaves` (body `CreateLeaveRequestCommand`); başarıda `["leaves"]` invalidation
    - `useCancelLeave()` → `POST /leaves/{id}/cancel`; başarıda `["leaves"]` invalidation
    - `usePendingLeaves(enabled: boolean)` → `GET /leaves/pending`
    - `useDecideLeave()` → mutation `{ id: number; approve: boolean }` → `POST /leaves/{id}/approve|reject` gövde `{}`; başarıda `["leaves"]` + `["dashboard","overview"]` invalidation
  - `attendance/queries.ts`:
    - Tipler: `LiveBoardCardDto`, `TimesheetDto`, `TimesheetRowDto`, `AttendanceSummaryDto`, `AttendanceEntryModel`
    - `useLiveBoard()` → `GET /attendance/live`
    - `useTimesheet(employeeId: number | null, year: number, month: number)` → `GET /attendance?employeeId=&year=&month=` (`enabled: employeeId !== null`)
    - `useAttendanceSummary(year: number, month: number)` → `GET /attendance/summary?year=&month=`
    - `useEmployeeOptions()` → `GET /employees?status=active&pageSize=50`, `select: r => r.items ?? []`
    - `useSaveAttendanceEntry()` → mutation `{ id: number | null; employeeId: number; model: AttendanceEntryModel }`: id null → `POST /attendance` `{ employeeId, model }`, değilse `PUT /attendance/{id}` `model`; başarıda `["attendance"]` invalidation

- [x] **Step 1: csv taşı** — `git mv frontend/src/features/personnel/csv.ts frontend/src/features/shared/csv.ts` ve `git mv .../personnel/csv.test.ts .../shared/csv.test.ts`; `PersonnelPage.tsx` içindeki `from "./csv"` → `from "../shared/csv"`.

- [x] **Step 2: `leaves/queries.ts` yaz**

```ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../../api/client";
import type { components } from "../../api/schema";

export type LeaveBalanceDto = components["schemas"]["LeaveBalanceDto"];
export type LeaveRequestDto = components["schemas"]["LeaveRequestDto"];
export type LeaveTypeDto = components["schemas"]["LeaveTypeDto"];
export type TeamLeaveDto = components["schemas"]["TeamLeaveDto"];
export type CreateLeaveRequestCommand = components["schemas"]["CreateLeaveRequestCommand"];
type EmployeePagedResult = components["schemas"]["EmployeeListItemDtoPagedResult"];

export const useLeaveBalance = () =>
  useQuery({ queryKey: ["leaves", "balance"], queryFn: () => apiFetch<LeaveBalanceDto>("/leaves/balance") });

export const useMyLeaves = () =>
  useQuery({ queryKey: ["leaves", "my"], queryFn: () => apiFetch<LeaveRequestDto[]>("/leaves/my") });

export const useLeaveTypes = () =>
  useQuery({ queryKey: ["leaves", "types"], queryFn: () => apiFetch<LeaveTypeDto[]>("/leaves/types") });

export const useTeamLeaves = () =>
  useQuery({ queryKey: ["leaves", "team"], queryFn: () => apiFetch<TeamLeaveDto[]>("/leaves/team") });

export const useSubstituteOptions = (enabled: boolean) =>
  useQuery({
    queryKey: ["employees", "options"],
    queryFn: () => apiFetch<EmployeePagedResult>("/employees?status=active&pageSize=50"),
    select: (r) => r.items ?? [],
    enabled,
  });

export const useCreateLeave = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: CreateLeaveRequestCommand) =>
      apiFetch<LeaveRequestDto>("/leaves", { method: "POST", body: JSON.stringify(command) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leaves"] }),
  });
};

export const useCancelLeave = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => apiFetch<null>(`/leaves/${id}/cancel`, { method: "POST" }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["leaves"] }),
  });
};

export const usePendingLeaves = (enabled: boolean) =>
  useQuery({
    queryKey: ["leaves", "pending"],
    queryFn: () => apiFetch<LeaveRequestDto[]>("/leaves/pending"),
    enabled,
  });

export const useDecideLeave = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, approve }: { id: number; approve: boolean }) =>
      apiFetch<LeaveRequestDto>(`/leaves/${id}/${approve ? "approve" : "reject"}`, {
        method: "POST",
        body: JSON.stringify({}),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["leaves"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard", "overview"] });
    },
  });
};
```

- [x] **Step 3: `attendance/queries.ts` yaz**

```ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../../api/client";
import type { components } from "../../api/schema";

export type LiveBoardCardDto = components["schemas"]["LiveBoardCardDto"];
export type TimesheetDto = components["schemas"]["TimesheetDto"];
export type TimesheetRowDto = components["schemas"]["TimesheetRowDto"];
export type AttendanceSummaryDto = components["schemas"]["AttendanceSummaryDto"];
export type AttendanceEntryModel = components["schemas"]["AttendanceEntryModel"];
type EmployeePagedResult = components["schemas"]["EmployeeListItemDtoPagedResult"];

export const useLiveBoard = () =>
  useQuery({ queryKey: ["attendance", "live"], queryFn: () => apiFetch<LiveBoardCardDto[]>("/attendance/live") });

export const useTimesheet = (employeeId: number | null, year: number, month: number) =>
  useQuery({
    queryKey: ["attendance", "timesheet", employeeId, year, month],
    queryFn: () => apiFetch<TimesheetDto>(`/attendance?employeeId=${employeeId}&year=${year}&month=${month}`),
    enabled: employeeId !== null,
  });

export const useAttendanceSummary = (year: number, month: number) =>
  useQuery({
    queryKey: ["attendance", "summary", year, month],
    queryFn: () => apiFetch<AttendanceSummaryDto[]>(`/attendance/summary?year=${year}&month=${month}`),
  });

export const useEmployeeOptions = () =>
  useQuery({
    queryKey: ["employees", "options"],
    queryFn: () => apiFetch<EmployeePagedResult>("/employees?status=active&pageSize=50"),
    select: (r) => r.items ?? [],
  });

export const useSaveAttendanceEntry = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, employeeId, model }: { id: number | null; employeeId: number; model: AttendanceEntryModel }) =>
      id === null
        ? apiFetch<TimesheetRowDto>("/attendance", { method: "POST", body: JSON.stringify({ employeeId, model }) })
        : apiFetch<TimesheetRowDto>(`/attendance/${id}`, { method: "PUT", body: JSON.stringify(model) }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["attendance"] }),
  });
};
```

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS (csv testleri yeni yerinden koşar).

- [x] **Step 5: Commit**

```bash
git add frontend/src/
git commit -m "refactor(frontend): CSV yardımcıları shared'a; izin ve puantaj query katmanları"
```

---

### Task 2: İzinlerim sayfası (bakiye + hareketler + ekip yokluk)

**Files:**
- Create: `frontend/src/features/leaves/LeavesPage.tsx`, `frontend/src/features/leaves/format.ts`
- Modify: `frontend/src/routes.tsx` (pageFor: leaves)
- Test: `frontend/src/features/leaves/LeavesPage.test.tsx`

**Interfaces:**
- Consumes: Task 1 leaves hook'ları, `useToast`, `PageLoading/PageError`.
- Produces:
  - `format.ts`: `formatLeaveDate(v?: string): string` ("12 Ağu 2024"), `LEAVE_STATUS_TEXT: Record<string,string>` (`pending→Bekliyor, approved→Onaylandı, rejected→Reddedildi, cancelled→İptal Edildi`), `awayDateLabel(start?: string, end?: string): string` (bugün aralıktaysa "Bugün", başlangıç yarınsa "Yarın", değilse formatlı başlangıç).
  - `LeavesPage()` — talep modalı Task 3'te; bu görevde "İzin Talebi Oluşturu" butonu `setModalOpen(true)` state'ini kurar, modal `{modalOpen && null}` placeholder'ıyla bağlanır (Task 3 dolduracak).

- [x] **Step 1: Başarısız testleri yaz** — `LeavesPage.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { LeavesPage } from "./LeavesPage";

const balance = { year: 2026, entitledDays: 20, carriedOverDays: 4, usedDays: 10, remainingDays: 14 };
const myLeaves = [
  { id: 1, employeeId: 5, employeeName: "Ben", leaveTypeId: 1, leaveTypeName: "Yıllık İzin", startDate: "2026-08-12", endDate: "2026-08-18", days: 5, status: "approved", description: null, substituteEmployeeId: null, decisionNote: null, decisionAtUtc: null, createdAtUtc: "2026-07-01T08:00:00Z" },
  { id: 2, employeeId: 5, employeeName: "Ben", leaveTypeId: 3, leaveTypeName: "Mazeret İzni", startDate: "2026-09-20", endDate: "2026-09-20", days: 1, status: "pending", description: null, substituteEmployeeId: null, decisionNote: null, decisionAtUtc: null, createdAtUtc: "2026-07-10T08:00:00Z" },
];
const team = [
  { employeeId: 7, employeeName: "Selin Koç", initials: "SK", leaveTypeName: "Raporlu", startDate: "2026-07-14", endDate: "2026-07-15" },
];

beforeEach(() =>
  stubApi({
    "/api/leaves/balance": balance,
    "/api/leaves/my": myLeaves,
    "/api/leaves/team": team,
    "/api/leaves/2/cancel": null,
  }),
);
afterEach(() => vi.unstubAllGlobals());

const renderLeaves = () =>
  renderPage(
    <ToastProvider>
      <LeavesPage />
    </ToastProvider>,
  );

test("bakiye kartları backend verisiyle dolar", async () => {
  renderLeaves();
  expect(await screen.findByText("Kalan Yıllık İzin")).toBeInTheDocument();
  expect(screen.getByText("Hak ediş: 24 gün")).toBeInTheDocument();
  expect(screen.getByText("10 gün kullanıldı")).toBeInTheDocument();
  expect(screen.getByText(/mazeret izni talebiniz yönetici onayı bekliyor/i)).toBeInTheDocument();
});

test("hareket tablosu durum pill'leri ve iptal butonu", async () => {
  renderLeaves();
  expect(await screen.findByText("Yıllık İzin")).toBeInTheDocument();
  expect(screen.getByText("Onaylandı")).toHaveClass("status-pill", "approved");
  // Yalnız pending satırda iptal butonu var
  expect(screen.getAllByTitle("Talebi iptal et")).toHaveLength(1);
});

test("iptal isteği cancel ucuna gider", async () => {
  renderLeaves();
  await screen.findByText("Mazeret İzni");
  await userEvent.click(screen.getByTitle("Talebi iptal et"));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.some(([u]) => String(u) === "/api/leaves/2/cancel");
    expect(posted).toBe(true);
  });
});

test("ekip yokluk widget'ı dolar", async () => {
  renderLeaves();
  expect(await screen.findByText("Selin Koç")).toBeInTheDocument();
  expect(screen.getByText(/Raporlu/)).toBeInTheDocument();
});
```

Run: `npm test -- --run src/features/leaves` → FAIL.

- [x] **Step 2: `format.ts` yaz**

```ts
export const formatLeaveDate = (value?: string | null): string =>
  value
    ? new Date(value).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" })
    : "";

export const LEAVE_STATUS_TEXT: Record<string, string> = {
  pending: "Bekliyor",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  cancelled: "İptal Edildi",
};

const dayDiff = (a: Date, b: Date): number =>
  Math.round((b.setHours(0, 0, 0, 0) - a.setHours(0, 0, 0, 0)) / 86_400_000);

export const awayDateLabel = (start?: string | null, end?: string | null): string => {
  if (!start) return "";
  const today = new Date();
  const startDiff = dayDiff(new Date(), new Date(start));
  const endDiff = end ? dayDiff(new Date(), new Date(end)) : startDiff;
  if (startDiff <= 0 && endDiff >= 0) return "Bugün";
  if (startDiff === 1) return "Yarın";
  return formatLeaveDate(start);
};
```

- [x] **Step 3: `LeavesPage.tsx` yaz** (eski Leaves() DOM paritesi; modal Task 3'te)

```tsx
import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { PageError, PageLoading } from "../shared/PageState";
import { LEAVE_STATUS_TEXT, awayDateLabel, formatLeaveDate } from "./format";
import { useCancelLeave, useLeaveBalance, useMyLeaves, useTeamLeaves } from "./queries";

export function LeavesPage() {
  const { showToast } = useToast();
  const [modalOpen, setModalOpen] = useState(false);
  const balanceQ = useLeaveBalance();
  const myQ = useMyLeaves();
  const teamQ = useTeamLeaves();
  const cancelLeave = useCancelLeave();

  const queries = [balanceQ, myQ, teamQ];
  if (queries.some((q) => q.isPending)) return <PageLoading />;
  const failed = queries.find((q) => q.isError);
  if (failed) return <PageError error={failed.error} />;

  const balance = balanceQ.data!;
  const myLeaves = myQ.data!;
  const team = teamQ.data!;

  const entitlement = (balance.entitledDays ?? 0) + (balance.carriedOverDays ?? 0);
  const remaining = balance.remainingDays ?? 0;
  const progress = entitlement > 0 ? Math.round((remaining / entitlement) * 100) : 0;
  const pendingLeaves = myLeaves.filter((l) => l.status === "pending");
  const firstPending = pendingLeaves[0];

  const handleCancel = async (id: number | undefined, type: string) => {
    if (id === undefined) return;
    try {
      await cancelLeave.mutateAsync(id);
      showToast(`${type} talebi iptal edildi.`, "info");
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "Talep iptal edilemedi.", "error");
    }
  };

  return (
    <div id="leaves-screen">
      <div className="page-header">
        <div>
          <h2>İzinlerim</h2>
          <p>Bakiye, geçmiş talepler ve ekip yokluk takibini tek alanda görüntüleyin.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setModalOpen(true)}>
          <i aria-hidden="true" className="fa-solid fa-plus" /> İzin Talebi Oluştur
        </button>
      </div>

      <div className="balance-grid">
        <div className="bal-card primary">
          <div className="bal-header">
            <div className="bal-icon"><i aria-hidden="true" className="fa-solid fa-umbrella-beach" /></div>
            <span className="status-pill info">Aktif bakiye</span>
          </div>
          <div className="bal-info">
            <span>Kalan Yıllık İzin</span>
            <strong>{remaining} <small>gün</small></strong>
          </div>
          <div className="bal-progress"><div className="prog-bar" style={{ width: `${progress}%` }} /></div>
          <span className="bal-sub">Hak ediş: {entitlement} gün</span>
        </div>
        <div className="bal-card">
          <div className="bal-header"><div className="bal-icon"><i aria-hidden="true" className="fa-solid fa-clock-rotate-left" /></div></div>
          <div className="bal-info">
            <span>Kullanılan Toplam</span>
            <strong>{balance.usedDays ?? 0} <small>gün</small></strong>
          </div>
          <div className="bal-stats">
            <div className="stat-pill"><span className="dot approved" /> {balance.usedDays ?? 0} gün kullanıldı</div>
          </div>
        </div>
        <div className="bal-card">
          <div className="bal-header"><div className="bal-icon"><i aria-hidden="true" className="fa-solid fa-hourglass-half" /></div></div>
          <div className="bal-info">
            <span>Onay Bekleyen</span>
            <strong>{pendingLeaves.length} <small>talep</small></strong>
          </div>
          <p className="pending-desc">
            {firstPending
              ? `${formatLeaveDate(firstPending.startDate)} tarihli ${(firstPending.leaveTypeName ?? "izin").toLocaleLowerCase("tr-TR")} talebiniz yönetici onayı bekliyor.`
              : "Bekleyen talebiniz yok."}
          </p>
        </div>
      </div>

      <div className="leaves-layout">
        <div className="leaves-list-section">
          <div className="section-header"><h3>İzin Hareketleri</h3></div>
          <div className="table-scroll">
            <table className="leaf-table">
              <thead>
                <tr>
                  <th>Tür</th>
                  <th>Tarih Aralığı</th>
                  <th>Süre</th>
                  <th>Durum</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {myLeaves.map((leave) => (
                  <tr key={leave.id}>
                    <td>
                      <div className="l-type">
                        <span className={`dot ${(leave.leaveTypeName ?? "").includes("Rapor") ? "sick" : "annual"}`} />
                        <strong>{leave.leaveTypeName}</strong>
                      </div>
                    </td>
                    <td>{formatLeaveDate(leave.startDate)} - {formatLeaveDate(leave.endDate)}</td>
                    <td><span className="days-badge">{leave.days} gün</span></td>
                    <td>
                      <span className={`status-pill ${leave.status ?? ""}`}>
                        {LEAVE_STATUS_TEXT[leave.status ?? ""] ?? leave.status}
                      </span>
                    </td>
                    <td className="text-right">
                      {leave.status === "pending" && (
                        <button
                          className="btn-icon-sm"
                          title="Talebi iptal et"
                          aria-label={`${leave.leaveTypeName} talebini iptal et`}
                          onClick={() => handleCancel(leave.id, leave.leaveTypeName ?? "İzin")}
                        >
                          <i aria-hidden="true" className="fa-solid fa-trash" />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="sidebar-col">
          <div className="team-calendar-widget card">
            <div className="widget-header"><h3>Ofiste Kimler Yok?</h3></div>
            <div className="away-list">
              {team.map((member) => (
                <div key={member.employeeId} className="away-item">
                  <div className="away-avatar">{member.initials}</div>
                  <div className="away-info">
                    <strong>{member.employeeName}</strong>
                    <span>{awayDateLabel(member.startDate, member.endDate)} • {member.leaveTypeName}</span>
                  </div>
                </div>
              ))}
              {team.length === 0 && <p className="pending-desc">Bu hafta ekipten izinli kimse yok.</p>}
            </div>
          </div>
        </div>
      </div>

      {/* İzin talebi modalı Task 3'te */}
      {modalOpen && null}
    </div>
  );
}
```

- [x] **Step 4: `routes.tsx` `pageFor`'a ekle** (`leaves: LeavesPage` + import)

- [x] **Step 5: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS.

- [x] **Step 6: Commit**

```bash
git add frontend/src/
git commit -m "feat(frontend): İzinlerim sayfası — bakiye, hareketler, iptal, ekip yokluk"
```

---

### Task 3: İzin talebi modalı

**Files:**
- Create: `frontend/src/features/leaves/LeaveRequestModal.tsx`
- Modify: `frontend/src/features/leaves/LeavesPage.tsx` (modal bağla)
- Test: `frontend/src/features/leaves/LeaveRequestModal.test.tsx`

**Interfaces:**
- Consumes: `useLeaveTypes`, `useCreateLeave`, `useSubstituteOptions`, `useLeaveBalance`, `useAuth`, `useToast`.
- Produces: `LeaveRequestModal({ onClose }: { onClose: () => void })`.

Tür kartı eşlemesi (ada göre, `includes`): "Yıllık"→`{icon:"fa-sun",tone:"annual"}` alt metin `Kalan: {remainingDays} gün`; "Rapor"→`{"fa-notes-medical","sick"}` "Belge gerekli"; "Mazeret"→`{"fa-clock","excuse"}` "Saatlik/günlük"; "Uzaktan"→`{"fa-laptop-house","remote"}` "Evden çalışma"; diğer→`{"fa-sun","annual"}` + tür adı.

- [x] **Step 1: Başarısız testleri yaz** — `LeaveRequestModal.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { AuthProvider } from "../../auth/AuthContext";
import { ToastProvider } from "../../layout/ToastProvider";
import { SESSION_KEY } from "../../api/session";
import { LeaveRequestModal } from "./LeaveRequestModal";

const types = [
  { id: 1, name: "Yıllık İzin", code: "YIL", deductsFromAnnualBalance: true, requiresApproval: true },
  { id: 2, name: "Raporlu", code: "RAP", deductsFromAnnualBalance: false, requiresApproval: false },
];
const balance = { year: 2026, entitledDays: 20, carriedOverDays: 4, usedDays: 10, remainingDays: 14 };
const created = { id: 9, leaveTypeId: 1, leaveTypeName: "Yıllık İzin", startDate: "2026-08-01", endDate: "2026-08-03", days: 2, status: "pending" };

const setRole = (role: string) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify({
    token: "T", refreshToken: "R",
    user: { id: "u", name: "X", email: "x@x", role, roleLabel: "X", initials: "XX", employeeId: 5 },
  }));

beforeEach(() => {
  localStorage.clear();
  stubApi({
    "/api/leaves/types": types,
    "/api/leaves/balance": balance,
    "/api/leaves": created,
    "/api/employees": { items: [{ id: 7, name: "Selin Koç", title: "UI", departmentId: 2, department: "Tasarım", status: "active", initials: "SK", hireDate: "2022-01-01", nationalIdMasked: "1" }], total: 1, page: 1, pageSize: 50, totalPages: 1 },
  });
});
afterEach(() => vi.unstubAllGlobals());

const renderModal = () =>
  renderPage(
    <AuthProvider>
      <ToastProvider>
        <LeaveRequestModal onClose={() => {}} />
      </ToastProvider>
    </AuthProvider>,
  );

test("izin türleri backend'den dolar, yıllık kartında kalan gün yazar", async () => {
  setRole("employee");
  renderModal();
  expect(await screen.findByText("Yıllık İzin")).toBeInTheDocument();
  expect(screen.getByText("Kalan: 14 gün")).toBeInTheDocument();
  expect(screen.getByText("Belge gerekli")).toBeInTheDocument();
});

test("tarih seçilince süre ve işe dönüş ön izlemesi hesaplanır", async () => {
  setRole("employee");
  renderModal();
  await screen.findByText("Yıllık İzin");
  await userEvent.type(screen.getByLabelText("Başlangıç Tarihi"), "2026-08-01");
  await userEvent.type(screen.getByLabelText("Bitiş Tarihi"), "2026-08-03");
  expect(screen.getByText("3 gün")).toBeInTheDocument();
  expect(screen.getByText("04 Ağu 2026")).toBeInTheDocument();
});

test("talep gönderilince POST /leaves doğru gövdeyle gider", async () => {
  setRole("manager");
  renderModal();
  await screen.findByText("Yıllık İzin");
  await userEvent.type(screen.getByLabelText("Başlangıç Tarihi"), "2026-08-01");
  await userEvent.type(screen.getByLabelText("Bitiş Tarihi"), "2026-08-03");
  await userEvent.selectOptions(await screen.findByLabelText("Yerine Bakacak Kişi"), "7");
  await userEvent.click(screen.getByRole("button", { name: /Talebi Gönder/ }));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/leaves" && i?.method === "POST",
    );
    expect(posted).toBeTruthy();
    const body = JSON.parse(String(posted![1]?.body));
    expect(body).toMatchObject({ leaveTypeId: 1, startDate: "2026-08-01", endDate: "2026-08-03", substituteEmployeeId: 7 });
  });
});

test("tarihler boşsa uyarı toast'ı, istek gitmez", async () => {
  setRole("employee");
  renderModal();
  await screen.findByText("Yıllık İzin");
  await userEvent.click(screen.getByRole("button", { name: /Talebi Gönder/ }));
  expect(await screen.findByText("Başlangıç ve bitiş tarihlerini seçin.")).toBeInTheDocument();
  const posted = vi.mocked(fetch).mock.calls.some(([u, i]) => String(u) === "/api/leaves" && i?.method === "POST");
  expect(posted).toBe(false);
});
```

Run: `npm test -- --run src/features/leaves/LeaveRequestModal.test.tsx` → FAIL.

- [x] **Step 2: `LeaveRequestModal.tsx` yaz**

```tsx
import { useState } from "react";
import { ApiError } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { useToast } from "../../layout/ToastProvider";
import { formatLeaveDate } from "./format";
import { useCreateLeave, useLeaveBalance, useLeaveTypes, useSubstituteOptions, type LeaveTypeDto } from "./queries";

const typeCardMeta = (type: LeaveTypeDto, remaining: number): { icon: string; tone: string; small: string } => {
  const name = type.name ?? "";
  if (name.includes("Yıllık")) return { icon: "fa-sun", tone: "annual", small: `Kalan: ${remaining} gün` };
  if (name.includes("Rapor")) return { icon: "fa-notes-medical", tone: "sick", small: "Belge gerekli" };
  if (name.includes("Mazeret")) return { icon: "fa-clock", tone: "excuse", small: "Saatlik/günlük" };
  if (name.includes("Uzaktan")) return { icon: "fa-laptop-house", tone: "remote", small: "Evden çalışma" };
  return { icon: "fa-sun", tone: "annual", small: name };
};

export function LeaveRequestModal({ onClose }: { onClose: () => void }) {
  const { user } = useAuth();
  const { showToast } = useToast();
  const typesQ = useLeaveTypes();
  const balanceQ = useLeaveBalance();
  const substitutesQ = useSubstituteOptions(user?.role !== "employee");
  const createLeave = useCreateLeave();

  const [typeId, setTypeId] = useState<number | null>(null);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [description, setDescription] = useState("");
  const [substituteId, setSubstituteId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const types = typesQ.data ?? [];
  const remaining = balanceQ.data?.remainingDays ?? 0;
  const selectedTypeId = typeId ?? types[0]?.id ?? null;

  // Takvim günü ön izlemesi; kesin süreyi backend hesaplar.
  const start = startDate ? new Date(startDate) : null;
  const end = endDate ? new Date(endDate) : null;
  const validRange = start && end && end.getTime() >= start.getTime();
  const previewDays = validRange ? Math.round((end!.getTime() - start!.getTime()) / 86_400_000) + 1 : null;
  const returnDate = validRange ? new Date(end!.getTime() + 86_400_000) : null;

  const submit = async () => {
    setError(null);
    if (!startDate || !endDate) {
      showToast("Başlangıç ve bitiş tarihlerini seçin.", "warning");
      return;
    }
    try {
      await createLeave.mutateAsync({
        leaveTypeId: selectedTypeId ?? undefined,
        startDate,
        endDate,
        description: description || null,
        substituteEmployeeId: substituteId ? Number(substituteId) : null,
      });
      showToast("İzin talebiniz yönetici onayına gönderildi.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
    }
  };

  return (
    <div id="leave-modal-overlay" className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>Yeni İzin Talebi</h3>
            <p>Talep detaylarını net ve eksiksiz doldurun.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="İzin talebi penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          <label className="input-label">İzin Türü</label>
          <div className="type-grid">
            {types.map((type) => {
              const meta = typeCardMeta(type, remaining);
              return (
                <label key={type.id} className="type-card">
                  <input
                    type="radio"
                    name="leaveType"
                    checked={selectedTypeId === type.id}
                    onChange={() => setTypeId(type.id ?? null)}
                  />
                  <div className="tc-content">
                    <div className={`tc-icon ${meta.tone}`}><i aria-hidden="true" className={`fa-solid ${meta.icon}`} /></div>
                    <span>{type.name}</span>
                    <small>{meta.small}</small>
                  </div>
                </label>
              );
            })}
          </div>

          <div className="form-grid-2 mt-4">
            <div className="input-group">
              <label className="input-label" htmlFor="start-date">Başlangıç Tarihi</label>
              <input type="date" className="input-control" id="start-date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="end-date">Bitiş Tarihi</label>
              <input type="date" className="input-control" id="end-date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
            </div>
          </div>

          <div className="calc-box">
            <div className="cb-item"><span>Süre</span><strong id="calc-days">{previewDays !== null ? `${previewDays} gün` : "- gün"}</strong></div>
            <div className="cb-item"><span>İşe dönüş</span><strong id="return-date">{returnDate ? formatLeaveDate(returnDate.toISOString()) : "-"}</strong></div>
          </div>

          <div className="input-group mt-4">
            <label className="input-label" htmlFor="leave-desc">Açıklama / Adres</label>
            <textarea id="leave-desc" className="input-control" rows={2} placeholder="İzin nedeni veya bulunacağınız adres" value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>

          <div className="input-group mt-3">
            <label className="input-label" htmlFor="leave-substitute">Yerine Bakacak Kişi</label>
            <select id="leave-substitute" className="input-control" value={substituteId} onChange={(e) => setSubstituteId(e.target.value)}>
              <option value="">Seçiniz...</option>
              {(substitutesQ.data ?? []).map((emp) => (
                <option key={emp.id} value={emp.id}>{emp.name}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={createLeave.isPending}>
            <i aria-hidden="true" className="fa-solid fa-paper-plane" /> Talebi Gönder
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [x] **Step 3: `LeavesPage.tsx`'te bağla** — `{modalOpen && null}` → `{modalOpen && <LeaveRequestModal onClose={() => setModalOpen(false)} />}` + import.

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS.

- [x] **Step 5: Commit**

```bash
git add frontend/src/features/leaves/
git commit -m "feat(frontend): izin talebi modalı — dinamik türler, süre ön izleme, gerçek POST"
```

---

### Task 4: Overview izin onay kartı (MGMT)

**Files:**
- Modify: `frontend/src/features/overview/OverviewPage.tsx`
- Test: `frontend/src/features/overview/OverviewApprovals.test.tsx`

**Interfaces:**
- Consumes: `usePendingLeaves`, `useDecideLeave` (leaves/queries), `useAuth`, `useToast`, `formatLeaveDate`.

- [x] **Step 1: Başarısız testleri yaz** — `OverviewApprovals.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { AuthProvider } from "../../auth/AuthContext";
import { ToastProvider } from "../../layout/ToastProvider";
import { SESSION_KEY } from "../../api/session";
import { OverviewPage } from "./OverviewPage";

const overview = {
  activeEmployees: 3, pendingApprovals: 1, openPositions: 0, newApplications: 0,
  inOfficeToday: 2, onLeaveToday: 0, pulseScore: 60,
  departmentDistribution: [{ dept: "Yazılım", count: 3 }],
  recruitmentFunnel: { total: 0, new: 0, interview: 0, offer: 0, rejected: 0, hired: 0 },
};
const pending = [
  { id: 4, employeeId: 5, employeeName: "Ahmet Yılmaz", leaveTypeId: 1, leaveTypeName: "Yıllık İzin", startDate: "2026-08-12", endDate: "2026-08-18", days: 5, status: "pending" },
];

const setRole = (role: string) =>
  localStorage.setItem(SESSION_KEY, JSON.stringify({
    token: "T", refreshToken: "R",
    user: { id: "u", name: "X", email: "x@x", role, roleLabel: "X", initials: "XX", employeeId: null },
  }));

beforeEach(() => {
  localStorage.clear();
  stubApi({
    "/api/dashboard/overview": overview,
    "/api/leaves/pending": pending,
    "/api/leaves/4/approve": { ...pending[0], status: "approved" },
  });
});
afterEach(() => vi.unstubAllGlobals());

const renderOverview = () =>
  renderPage(
    <AuthProvider>
      <ToastProvider>
        <OverviewPage />
      </ToastProvider>
    </AuthProvider>,
  );

test("yönetici bekleyen izin taleplerini görür ve onaylar", async () => {
  setRole("manager");
  renderOverview();
  expect(await screen.findByText("Bekleyen Aksiyonlar")).toBeInTheDocument();
  expect(screen.getByText("Ahmet Yılmaz - Yıllık İzin")).toBeInTheDocument();
  await userEvent.click(screen.getByTitle("Onayla"));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.some(([u]) => String(u) === "/api/leaves/4/approve");
    expect(posted).toBe(true);
  });
});

test("çalışan onay kartını görmez", async () => {
  setRole("employee");
  renderOverview();
  await screen.findByText("Aktif Personel");
  expect(screen.queryByText("Bekleyen Aksiyonlar")).not.toBeInTheDocument();
});
```

Run: `npm test -- --run src/features/overview/OverviewApprovals.test.tsx` → FAIL.

Not: mevcut `OverviewPage.test.tsx` AuthProvider'sız render ediyor ve kart MGMT'ye bağlı olduğundan `useAuth` çağrısı patlar → o testin `renderPage` sarmalayıcısına da `AuthProvider` eklenir (oturum yazmadan: user null → kart görünmez, testler aynen geçer). `stubApi` haritasına `"/api/leaves/pending": []` eklemek gerekmez (user null → sorgu disabled).

- [x] **Step 2: `OverviewPage.tsx`'i genişlet**

Eklenen import'lar:

```tsx
import { useAuth } from "../../auth/AuthContext";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { formatLeaveDate } from "../leaves/format";
import { useDecideLeave, usePendingLeaves } from "../leaves/queries";
```

Component başına:

```tsx
  const { user } = useAuth();
  const { showToast } = useToast();
  const isManagement = user?.role === "hr-admin" || user?.role === "manager";
  const pendingQ = usePendingLeaves(isManagement);
  const decide = useDecideLeave();

  const handleDecision = async (id: number | undefined, name: string, approve: boolean) => {
    if (id === undefined) return;
    try {
      await decide.mutateAsync({ id, approve });
      showToast(`${name} izin talebi ${approve ? "onaylandı" : "reddedildi"}.`, approve ? "success" : "info");
    } catch (e) {
      showToast(e instanceof ApiError ? e.message : "İşlem tamamlanamadı.", "error");
    }
  };
```

`charts-grid` kapanışından sonra (eski bottom-grid DOM paritesi; yalnız MGMT):

```tsx
      {isManagement && (
        <div className="bottom-grid">
          <div className="card task-list">
            <div className="card-header-clean">
              <div>
                <h4>Bekleyen Aksiyonlar</h4>
                <p className="text-muted">Öncelik sırasına göre</p>
              </div>
              <span className="badge-count">{(pendingQ.data ?? []).length}</span>
            </div>
            <div className="task-stack">
              {(pendingQ.data ?? []).map((request) => (
                <div key={request.id} className="task-item">
                  <div className="task-icon warning"><i aria-hidden="true" className="fa-solid fa-plane-departure" /></div>
                  <div className="task-desc">
                    <strong>{request.employeeName} - {request.leaveTypeName}</strong>
                    <small>{formatLeaveDate(request.startDate)} - {formatLeaveDate(request.endDate)}, {request.days} gün</small>
                  </div>
                  <div className="toolbar-actions">
                    <button className="btn-icon-sm" aria-label={`${request.employeeName} izin talebini onayla`} title="Onayla" onClick={() => handleDecision(request.id, request.employeeName ?? "", true)}>
                      <i aria-hidden="true" className="fa-solid fa-check" />
                    </button>
                    <button className="btn-icon-sm" aria-label={`${request.employeeName} izin talebini reddet`} title="Reddet" onClick={() => handleDecision(request.id, request.employeeName ?? "", false)}>
                      <i aria-hidden="true" className="fa-solid fa-xmark" />
                    </button>
                  </div>
                </div>
              ))}
              {(pendingQ.data ?? []).length === 0 && <p className="pending-desc">Bekleyen talep yok.</p>}
            </div>
          </div>
        </div>
      )}
```

- [x] **Step 3: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS (OverviewPage.test.tsx sarmalayıcı güncellemesi dahil).

- [x] **Step 4: Commit**

```bash
git add frontend/src/features/overview/
git commit -m "feat(frontend): Overview'a gerçek izin onay kartı (MGMT) — approve/reject akışı"
```

---

### Task 5: Mesai & Puantaj sayfası

**Files:**
- Create: `frontend/src/features/attendance/AttendancePage.tsx`, `frontend/src/features/attendance/format.ts`
- Modify: `frontend/src/routes.tsx` (pageFor: attendance)
- Test: `frontend/src/features/attendance/AttendancePage.test.tsx`

**Interfaces:**
- Consumes: Task 1 attendance hook'ları, `shared/csv`, `useToast`.
- Produces:
  - `format.ts`: `minutesToHhMm(m?: number): string` ("08:00"), `formatTime(t?: string | null): string` ("09:00" | "--:--"), `formatBreak(m?: number): string` ("-"|"1s"|"45 dk"), `formatTsDate(v?: string): string` ("01 Eki Pzt"), `monthLabel(year: number, month: number): string` ("Ekim 2025"), `LIVE_STATUS_TEXT` (`ontime→Zamanında, late→Geç kaldı, absent→Gelmedi, early→Erken giriş`).
  - `AttendancePage()` — manuel giriş modalı Task 6'da; ghost kart ve satır kalem butonu `setEntry(...)` state kurar, `{entry && null}` placeholder.

- [x] **Step 1: Başarısız testleri yaz** — `AttendancePage.test.tsx`:

```tsx
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { AttendancePage } from "./AttendancePage";

const live = [
  { employeeId: 1, name: "Ahmet Yılmaz", initials: "AY", department: "Yazılım", checkIn: "08:45:00", status: "ontime" },
  { employeeId: 2, name: "Burak Demir", initials: "BD", department: "Satış", checkIn: null, status: "absent" },
];
const summary = [
  { employeeId: 1, employeeName: "Ahmet Yılmaz", department: "Yazılım", totalDays: 20, presentDays: 19, absentDays: 1, lateDays: 2, totalWorkedMinutes: 9600, totalOvertimeMinutes: 120 },
  { employeeId: 2, employeeName: "Burak Demir", department: "Satış", totalDays: 20, presentDays: 18, absentDays: 2, lateDays: 0, totalWorkedMinutes: 8400, totalOvertimeMinutes: 0 },
];
const employees = { items: [
  { id: 1, name: "Ahmet Yılmaz", title: "Dev", departmentId: 1, department: "Yazılım", status: "active", initials: "AY", hireDate: "2021-01-01", nationalIdMasked: "1" },
  { id: 2, name: "Burak Demir", title: "Satış", departmentId: 2, department: "Satış", status: "active", initials: "BD", hireDate: "2021-01-01", nationalIdMasked: "2" },
], total: 2, page: 1, pageSize: 50, totalPages: 1 };
const timesheet = {
  employeeId: 1, employeeName: "Ahmet Yılmaz", year: 2026, month: 7,
  rows: [
    { id: 11, workDate: "2026-07-01", type: "Tam", checkIn: "09:00:00", checkOut: "18:00:00", breakMinutes: 60, workedMinutes: 480, overtimeMinutes: 0, status: "ok", note: null },
    { id: 12, workDate: "2026-07-02", type: "Mesai", checkIn: "09:00:00", checkOut: "20:00:00", breakMinutes: 60, workedMinutes: 600, overtimeMinutes: 120, status: "overtime", note: null },
  ],
  totalWorkedMinutes: 1080, totalOvertimeMinutes: 120,
};

beforeEach(() =>
  stubApi({
    "/api/attendance/live": live,
    "/api/attendance/summary": summary,
    "/api/attendance": timesheet,
    "/api/employees": employees,
  }),
);
afterEach(() => vi.unstubAllGlobals());

const renderAttendance = () =>
  renderPage(
    <ToastProvider>
      <AttendancePage />
    </ToastProvider>,
  );

test("istatistik şeridi özet toplamlarından türetilir", async () => {
  renderAttendance();
  // 9600+8400=18000 dk = 300 saat; mesai 120 dk = 2 saat; geç kalan 1 kişi; devamsızlık 3 gün
  expect(await screen.findByText("300")).toBeInTheDocument();
  expect(screen.getByText("Geç Kalma").closest(".stat-box")).toHaveTextContent("1");
  expect(screen.getByText("Devamsızlık").closest(".stat-box")).toHaveTextContent("3");
});

test("canlı kartlar durum rozetiyle dolar", async () => {
  renderAttendance();
  expect(await screen.findByText("Zamanında")).toBeInTheDocument();
  expect(screen.getByText("Gelmedi", { selector: ".lc-badge" })).toBeInTheDocument();
  expect(screen.getByText("Manuel giriş ekle")).toBeInTheDocument();
});

test("puantaj sekmesi tabloyu ve aylık toplamı gösterir", async () => {
  renderAttendance();
  await screen.findByText("Zamanında");
  await userEvent.click(screen.getByRole("button", { name: /Aylık Puantaj/ }));
  expect(await screen.findByText("Fazla mesai")).toBeInTheDocument();
  expect(screen.getByText("18:00", { selector: ".text-blue" })).toBeInTheDocument(); // 1080 dk aylık toplam
});
```

Run: `npm test -- --run src/features/attendance` → FAIL.

- [x] **Step 2: `format.ts` yaz**

```ts
export const minutesToHhMm = (minutes?: number): string => {
  const total = minutes ?? 0;
  const h = Math.floor(total / 60);
  const m = total % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
};

export const formatTime = (value?: string | null): string => (value ? value.slice(0, 5) : "--:--");

export const formatBreak = (minutes?: number): string => {
  if (!minutes) return "-";
  return minutes % 60 === 0 ? `${minutes / 60}s` : `${minutes} dk`;
};

export const formatTsDate = (value?: string): string => {
  if (!value) return "";
  const date = new Date(value);
  const dayMonth = date.toLocaleDateString("tr-TR", { day: "2-digit", month: "short" });
  const weekday = date.toLocaleDateString("tr-TR", { weekday: "short" });
  return `${dayMonth} ${weekday}`;
};

export const monthLabel = (year: number, month: number): string =>
  new Date(year, month - 1, 1).toLocaleDateString("tr-TR", { month: "long", year: "numeric" });

export const LIVE_STATUS_TEXT: Record<string, string> = {
  ontime: "Zamanında",
  late: "Geç kaldı",
  absent: "Gelmedi",
  early: "Erken giriş",
};
```

- [x] **Step 3: `AttendancePage.tsx` yaz** (eski Attendance() DOM paritesi)

```tsx
import { useEffect, useRef, useState } from "react";
import { useToast } from "../../layout/ToastProvider";
import { downloadCsv, tableToCsvLines } from "../shared/csv";
import { PageError, PageLoading } from "../shared/PageState";
import { LIVE_STATUS_TEXT, formatBreak, formatTime, formatTsDate, minutesToHhMm, monthLabel } from "./format";
import {
  useAttendanceSummary, useEmployeeOptions, useLiveBoard, useTimesheet, type TimesheetRowDto,
} from "./queries";

type EntryTarget = { rowId: number | null; row?: TimesheetRowDto };

export function AttendancePage() {
  const { showToast } = useToast();
  const now = new Date();
  const [period, setPeriod] = useState({ year: now.getFullYear(), month: now.getMonth() + 1 });
  const [activeTab, setActiveTab] = useState<"live-view" | "timesheet-view">("live-view");
  const [employeeId, setEmployeeId] = useState<number | null>(null);
  const [entry, setEntry] = useState<EntryTarget | null>(null);
  const tableRef = useRef<HTMLTableElement>(null);

  const liveQ = useLiveBoard();
  const summaryQ = useAttendanceSummary(period.year, period.month);
  const employeesQ = useEmployeeOptions();
  const timesheetQ = useTimesheet(employeeId, period.year, period.month);

  useEffect(() => {
    if (employeeId === null && (employeesQ.data ?? []).length > 0) {
      setEmployeeId(employeesQ.data![0].id ?? null);
    }
  }, [employeesQ.data, employeeId]);

  if (liveQ.isPending || summaryQ.isPending || employeesQ.isPending) return <PageLoading />;
  if (liveQ.isError) return <PageError error={liveQ.error} />;
  if (summaryQ.isError) return <PageError error={summaryQ.error} />;
  if (employeesQ.isError) return <PageError error={employeesQ.error} />;

  const summary = summaryQ.data;
  const totalWorkedHours = Math.round(summary.reduce((t, s) => t + (s.totalWorkedMinutes ?? 0), 0) / 60);
  const totalOvertimeHours = Math.round(summary.reduce((t, s) => t + (s.totalOvertimeMinutes ?? 0), 0) / 60);
  const lateCount = summary.filter((s) => (s.lateDays ?? 0) > 0).length;
  const absentDays = summary.reduce((t, s) => t + (s.absentDays ?? 0), 0);

  const changeMonth = (step: number) =>
    setPeriod((current) => {
      const date = new Date(current.year, current.month - 1 + step, 1);
      return { year: date.getFullYear(), month: date.getMonth() + 1 };
    });

  const exportCsv = () => {
    if (!tableRef.current) {
      showToast("Dışa aktarılacak tablo bulunamadı.", "error");
      return;
    }
    downloadCsv(tableToCsvLines(tableRef.current), "puantaj-raporu");
    showToast("CSV raporu indirildi.", "success");
  };

  const rows = timesheetQ.data?.rows ?? [];

  return (
    <div id="attendance-screen">
      <div className="page-header">
        <div>
          <h2>Mesai & Puantaj Takibi</h2>
          <p>Giriş-çıkış saatleri, vardiya durumu ve aylık puantaj kontrolü.</p>
        </div>
        <div className="header-actions">
          <div className="date-picker-wrapper">
            <button className="btn-icon" aria-label="Önceki ay" title="Önceki ay" onClick={() => changeMonth(-1)}>
              <i aria-hidden="true" className="fa-solid fa-chevron-left" />
            </button>
            <span className="current-month">
              <i aria-hidden="true" className="fa-solid fa-calendar-days" /> <span id="attendance-month">{monthLabel(period.year, period.month)}</span>
            </span>
            <button className="btn-icon" aria-label="Sonraki ay" title="Sonraki ay" onClick={() => changeMonth(1)}>
              <i aria-hidden="true" className="fa-solid fa-chevron-right" />
            </button>
          </div>
          <button className="btn btn-primary" onClick={exportCsv}>
            <i aria-hidden="true" className="fa-solid fa-file-export" /> Rapor Al
          </button>
        </div>
      </div>

      <div className="stats-stripe">
        <div className="stat-box"><span className="sb-label">Toplam Çalışma</span><strong className="sb-val">{totalWorkedHours} <small>saat</small></strong></div>
        <div className="stat-box"><span className="sb-label">Fazla Mesai</span><strong className="sb-val text-orange">{totalOvertimeHours} <small>saat</small></strong></div>
        <div className="stat-box"><span className="sb-label">Geç Kalma</span><strong className="sb-val text-red">{lateCount} <small>kişi</small></strong></div>
        <div className="stat-box"><span className="sb-label">Devamsızlık</span><strong className="sb-val">{absentDays} <small>gün</small></strong></div>
      </div>

      <div className="att-content surface">
        <div className="att-tabs">
          <button className={`att-tab ${activeTab === "live-view" ? "active" : ""}`} onClick={() => setActiveTab("live-view")}>
            <i aria-hidden="true" className="fa-solid fa-video" /> Canlı İzleme
          </button>
          <button className={`att-tab ${activeTab === "timesheet-view" ? "active" : ""}`} onClick={() => setActiveTab("timesheet-view")}>
            <i aria-hidden="true" className="fa-solid fa-table" /> Aylık Puantaj
          </button>
        </div>

        <div id="live-view" className={`att-section ${activeTab === "live-view" ? "active" : ""}`}>
          <div className="live-grid">
            {liveQ.data.map((card) => (
              <div key={card.employeeId} className={`live-card ${card.status ?? ""}`}>
                <div className="lc-header">
                  <span className={`lc-badge ${card.status ?? ""}`}>{LIVE_STATUS_TEXT[card.status ?? ""] ?? card.status}</span>
                  <span className="lc-time"><i aria-hidden="true" className="fa-regular fa-clock" /> {formatTime(card.checkIn)}</span>
                </div>
                <div className="lc-body">
                  <div className="lc-avatar">{card.initials}</div>
                  <div>
                    <h4>{card.name}</h4>
                    <p>{card.department}</p>
                  </div>
                </div>
              </div>
            ))}
            {/* div: button elementi tarayıcı çerçevesi getirir (parite) */}
            <div className="live-card ghost" role="button" tabIndex={0} onClick={() => setEntry({ rowId: null })}
              onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") setEntry({ rowId: null }); }}>
              <i aria-hidden="true" className="fa-solid fa-plus" />
              <span>Manuel giriş ekle</span>
            </div>
          </div>
        </div>

        <div id="timesheet-view" className={`att-section ${activeTab === "timesheet-view" ? "active" : ""}`}>
          <div className="ts-filter">
            <label className="sr-only" htmlFor="ts-user-select">Personel seç</label>
            <select
              id="ts-user-select"
              className="user-select"
              value={employeeId ?? ""}
              onChange={(e) => setEmployeeId(Number(e.target.value))}
            >
              {(employeesQ.data ?? []).map((emp) => (
                <option key={emp.id} value={emp.id}>{emp.name} ({emp.department})</option>
              ))}
            </select>
            <div className="legend">
              <span className="leg-item"><span className="dot ok" /> Normal</span>
              <span className="leg-item"><span className="dot warn" /> Geç/Erken</span>
              <span className="leg-item"><span className="dot danger" /> Eksik</span>
            </div>
          </div>

          <div className="table-wrapper">
            <table className="att-table" ref={tableRef}>
              <thead>
                <tr>
                  <th>Tarih</th><th>Tip</th><th>Giriş</th><th>Çıkış</th><th>Mola</th><th>Net Süre</th><th>Durum</th><th className="csv-skip"></th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td><span className="date-cell">{formatTsDate(row.workDate)}</span></td>
                    <td>
                      {row.type === "Tam"
                        ? <span className="type-badge reg">Normal</span>
                        : row.type === "Mesai"
                          ? <span className="type-badge over">Fazla mesai</span>
                          : <span className="type-badge abs">İzin/Rapor</span>}
                    </td>
                    <td className="mono">{formatTime(row.checkIn)}</td>
                    <td className="mono">{formatTime(row.checkOut)}</td>
                    <td className="mono">{formatBreak(row.breakMinutes)}</td>
                    <td><strong className="mono">{minutesToHhMm(row.workedMinutes)}</strong></td>
                    <td>
                      {row.status === "late"
                        ? <span className="warn-text"><i aria-hidden="true" className="fa-solid fa-triangle-exclamation" /> Geç</span>
                        : row.status === "overtime"
                          ? <span className="success-text"><i aria-hidden="true" className="fa-solid fa-star" /> +{Math.round((row.overtimeMinutes ?? 0) / 60)}s mesai</span>
                          : row.status === "absent"
                            ? <span className="danger-text">Gelmedi</span>
                            : <span className="ok-text"><i aria-hidden="true" className="fa-solid fa-check" /> Uygun</span>}
                    </td>
                    <td className="text-right csv-skip">
                      <button className="btn-icon-sm" title="Kaydı düzenle" aria-label={`${formatTsDate(row.workDate)} puantaj kaydını düzenle`} onClick={() => setEntry({ rowId: row.id ?? null, row })}>
                        <i aria-hidden="true" className="fa-solid fa-pen" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={5} className="text-right"><strong>Aylık Toplam:</strong></td>
                  <td><strong className="text-blue mono">{minutesToHhMm(timesheetQ.data?.totalWorkedMinutes)}</strong></td>
                  <td colSpan={2}></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>

      {/* Manuel giriş/düzenleme modalı Task 6'da */}
      {entry && null}
    </div>
  );
}
```

- [x] **Step 4: `routes.tsx` `pageFor`'a ekle** (`attendance: AttendancePage` + import)

- [x] **Step 5: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS.

- [x] **Step 6: Commit**

```bash
git add frontend/src/
git commit -m "feat(frontend): Mesai & Puantaj — canlı pano, aylık puantaj, özet istatistikleri, CSV"
```

---

### Task 6: Manuel giriş / satır düzenleme modalı

**Files:**
- Create: `frontend/src/features/attendance/AttendanceEntryModal.tsx`
- Modify: `frontend/src/features/attendance/AttendancePage.tsx` (modal bağla)
- Test: `frontend/src/features/attendance/AttendanceEntryModal.test.tsx`

**Interfaces:**
- Consumes: `useSaveAttendanceEntry`, `useEmployeeOptions`, `useToast`.
- Produces: `AttendanceEntryModal({ rowId, initial, defaultEmployeeId, onClose })`:
  - `rowId: number | null` (null → yeni kayıt `POST`), `initial?: TimesheetRowDto` (düzenlemede alanları doldurur), `defaultEmployeeId: number | null`.

- [x] **Step 1: Başarısız testleri yaz** — `AttendanceEntryModal.test.tsx`:

```tsx
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, test, vi } from "vitest";
import { stubApi } from "../../test/apiStub";
import { renderPage } from "../../test/renderPage";
import { ToastProvider } from "../../layout/ToastProvider";
import { AttendanceEntryModal } from "./AttendanceEntryModal";

const employees = { items: [
  { id: 1, name: "Ahmet Yılmaz", title: "Dev", departmentId: 1, department: "Yazılım", status: "active", initials: "AY", hireDate: "2021-01-01", nationalIdMasked: "1" },
], total: 1, page: 1, pageSize: 50, totalPages: 1 };
const savedRow = { id: 33, workDate: "2026-07-14", type: "Tam", checkIn: "09:00:00", checkOut: "18:00:00", breakMinutes: 60, workedMinutes: 480, overtimeMinutes: 0, status: "ok", note: null };

beforeEach(() => stubApi({ "/api/employees": employees, "/api/attendance": savedRow, "/api/attendance/33": savedRow }));
afterEach(() => vi.unstubAllGlobals());

const renderModal = (rowId: number | null, initial?: typeof savedRow) =>
  renderPage(
    <ToastProvider>
      <AttendanceEntryModal rowId={rowId} initial={initial} defaultEmployeeId={1} onClose={() => {}} />
    </ToastProvider>,
  );

test("yeni kayıt POST /attendance gövdesiyle gider", async () => {
  renderModal(null);
  await screen.findByText("Manuel Puantaj Girişi");
  await userEvent.type(screen.getByLabelText("Tarih"), "2026-07-14");
  await userEvent.type(screen.getByLabelText("Giriş"), "09:00");
  await userEvent.type(screen.getByLabelText("Çıkış"), "18:00");
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  await waitFor(() => {
    const posted = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/attendance" && i?.method === "POST",
    );
    expect(posted).toBeTruthy();
    const body = JSON.parse(String(posted![1]?.body));
    expect(body.employeeId).toBe(1);
    expect(body.model).toMatchObject({ workDate: "2026-07-14", checkIn: "09:00", checkOut: "18:00", type: "Tam" });
  });
});

test("düzenleme PUT /attendance/{id} ile gider ve alanlar dolu gelir", async () => {
  renderModal(33, savedRow);
  await screen.findByText("Puantaj Kaydını Düzenle");
  expect(screen.getByLabelText("Tarih")).toHaveValue("2026-07-14");
  await userEvent.click(screen.getByRole("button", { name: /Kaydet/ }));
  await waitFor(() => {
    const put = vi.mocked(fetch).mock.calls.find(
      ([u, i]) => String(u) === "/api/attendance/33" && i?.method === "PUT",
    );
    expect(put).toBeTruthy();
  });
});
```

Run: `npm test -- --run src/features/attendance/AttendanceEntryModal.test.tsx` → FAIL.

- [x] **Step 2: `AttendanceEntryModal.tsx` yaz** (mevcut modal-overlay/modal-card sınıflarıyla)

```tsx
import { useState } from "react";
import { ApiError } from "../../api/client";
import { useToast } from "../../layout/ToastProvider";
import { useEmployeeOptions, useSaveAttendanceEntry, type TimesheetRowDto } from "./queries";

export function AttendanceEntryModal({ rowId, initial, defaultEmployeeId, onClose }: {
  rowId: number | null;
  initial?: TimesheetRowDto;
  defaultEmployeeId: number | null;
  onClose: () => void;
}) {
  const { showToast } = useToast();
  const employeesQ = useEmployeeOptions();
  const save = useSaveAttendanceEntry();

  const [employeeId, setEmployeeId] = useState(String(defaultEmployeeId ?? ""));
  const [workDate, setWorkDate] = useState(initial?.workDate ?? "");
  const [type, setType] = useState(initial?.type ?? "Tam");
  const [checkIn, setCheckIn] = useState(initial?.checkIn?.slice(0, 5) ?? "");
  const [checkOut, setCheckOut] = useState(initial?.checkOut?.slice(0, 5) ?? "");
  const [breakMinutes, setBreakMinutes] = useState(String(initial?.breakMinutes ?? 60));
  const [note, setNote] = useState(initial?.note ?? "");
  const [error, setError] = useState<string | null>(null);

  const isEdit = rowId !== null;

  const submit = async () => {
    setError(null);
    if (!workDate) {
      showToast("Tarih seçin.", "warning");
      return;
    }
    try {
      await save.mutateAsync({
        id: rowId,
        employeeId: Number(employeeId) || 0,
        model: {
          workDate,
          checkIn: checkIn || null,
          checkOut: checkOut || null,
          breakMinutes: Number(breakMinutes) || 0,
          type,
          note: note || null,
        },
      });
      showToast(isEdit ? "Puantaj kaydı güncellendi." : "Manuel giriş eklendi.", "success");
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Beklenmeyen bir hata oluştu.");
    }
  };

  return (
    <div className="modal-overlay" style={{ display: "flex" }}>
      <div className="modal-card scale-in">
        <div className="modal-head">
          <div>
            <h3>{isEdit ? "Puantaj Kaydını Düzenle" : "Manuel Puantaj Girişi"}</h3>
            <p>Giriş-çıkış saatlerini ve gün tipini doğru girin.</p>
          </div>
          <button className="btn-icon-sm" onClick={onClose} title="Kapat" aria-label="Puantaj penceresini kapat">
            <i aria-hidden="true" className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className="modal-body-scroll">
          {error && <p className="form-error" role="alert">{error}</p>}
          {!isEdit && (
            <div className="input-group">
              <label className="input-label" htmlFor="ae-employee">Personel</label>
              <select id="ae-employee" className="input-control" value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
                {(employeesQ.data ?? []).map((emp) => (
                  <option key={emp.id} value={emp.id}>{emp.name} ({emp.department})</option>
                ))}
              </select>
            </div>
          )}
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-date">Tarih</label>
              <input id="ae-date" type="date" className="input-control" value={workDate} onChange={(e) => setWorkDate(e.target.value)} disabled={isEdit} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-type">Tip</label>
              <select id="ae-type" className="input-control" value={type ?? "Tam"} onChange={(e) => setType(e.target.value)}>
                <option>Tam</option><option>Mesai</option><option>Rapor</option>
              </select>
            </div>
          </div>
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-in">Giriş</label>
              <input id="ae-in" type="time" className="input-control" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-out">Çıkış</label>
              <input id="ae-out" type="time" className="input-control" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} />
            </div>
          </div>
          <div className="form-grid-2 mt-3">
            <div className="input-group">
              <label className="input-label" htmlFor="ae-break">Mola (dk)</label>
              <input id="ae-break" type="number" className="input-control" value={breakMinutes} onChange={(e) => setBreakMinutes(e.target.value)} />
            </div>
            <div className="input-group">
              <label className="input-label" htmlFor="ae-note">Not</label>
              <input id="ae-note" type="text" className="input-control" value={note ?? ""} onChange={(e) => setNote(e.target.value)} />
            </div>
          </div>
        </div>

        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onClose}>Vazgeç</button>
          <button className="btn btn-primary" onClick={submit} disabled={save.isPending}>
            <i aria-hidden="true" className="fa-solid fa-check" /> Kaydet
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [x] **Step 3: `AttendancePage.tsx`'e bağla** — `{entry && null}` yerine:

```tsx
      {entry && (
        <AttendanceEntryModal
          rowId={entry.rowId}
          initial={entry.row}
          defaultEmployeeId={employeeId}
          onClose={() => setEntry(null)}
        />
      )}
```

`import { AttendanceEntryModal } from "./AttendanceEntryModal";` ekle.

- [x] **Step 4: Testleri doğrula** — Run: `npm test -- --run` → tümü PASS. `npm run build` → hatasız.

- [x] **Step 5: Commit**

```bash
git add frontend/src/features/attendance/
git commit -m "feat(frontend): puantaj manuel giriş ve satır düzenleme modalı"
```

---

### Task 7: Uçtan uca duman testi + parite + günlük + kapanış

**Files:**
- Modify: `docs/gelistirme-gunlugu.md`; gerekirse küçük düzeltmeler.

- [x] **Step 1: Backend + frontend'i başlat** (önceki dilimlerle aynı)

- [x] **Step 2: Duman testi**

1. Çalışan (`ahmet.yilmaz@hrmaster.local`) → `#/leaves`: bakiye kartları gerçek; talep oluştur (yarın→+2 gün) → listede "Bekliyor"; iptal et → listeden düşer/İptal.
2. Tekrar talep oluştur (onay testi için bırak).
3. hr-admin → `#/overview`: "Bekleyen Aksiyonlar" kartında talep görünür; Onayla → toast + karttan düşer.
4. Çalışan → `#/leaves`: talep "Onaylandı"; "Ofiste Kimler Yok?" (tarih bugünse) dolar.
5. hr-admin → `#/attendance`: istatistik şeridi + canlı kartlar gerçek; ay değiştirici çalışır.
6. Aylık Puantaj: kişi seç → satırlar; satır kalemi → düzenle → kaydet → tablo yenilenir.
7. Manuel giriş ekle → yeni satır. Rapor Al → CSV iner.
8. Çalışan `#/attendance` → "Bu alan için yetki gerekli".

- [x] **Step 3: Görsel parite** — eski/yeni `#/leaves` (+ modal) ve `#/attendance` (iki sekme) yan yana. Bilinçli farklar: tek stat-pill, dinamik pending cümlesi, Overview'da tek kart (Önemli Günler yok), manuel giriş modalı (eskide yoktu), gerçek ay etiketi. Diğer farklar DOM/class düzeltmesiyle giderilir.

- [x] **Step 4: Günlük + kapanış commit** — "Şu an neredeyiz" → Dilim 5 (Bordro); Dilim 4 kaydı; plan kutuları işaretlenir.

```bash
git add frontend/ docs/
git commit -m "test(frontend): dilim 4 duman testi, parite kontrolü ve günlük güncellemesi"
```

---

## Yürütme notu (paralellik)

Task 1 tamamlandıktan sonra Task 2–4 (izin) ile Task 5–6 (puantaj) bağımsızdır. Puantaj görevleri bir subagent'a devredilebilir; bu durumda subagent `routes.tsx`'e **dokunmaz** (kayıt Task 5 Step 4 ana oturumda yapılır) ve commit atmaz (ana oturum commit'ler).

## Sonraki dilimler

Dilim 5 (Bordro) planı bu dilim main'e merge edildikten sonra yazılır.
