# İK Pro — Geliştirme Günlüğü

> **Amaç:** Geliştirme kesilirse en son nerede kalındığını bu dosyadan anlamak.
> Her oturum sonunda buraya tarihli bir kayıt eklenir; en üstteki "Şu an neredeyiz"
> bölümü daima güncel tutulur.

## Şu an neredeyiz

- **Aktif iş:** React portu — **Dilim 2 tamamlandı**; dal `feature/react-port-dilim-2`
  main'e merge kararı bekliyor.
- **Son tamamlanan:** Dilim 2 (Overview + Risk Merkezi + 5 risk detayı) — 7 görev,
  33 birim test, duman testi ve parite kontrolü geçti.
- **Sıradaki adım:** Dilim 2'yi main'e merge et; sonra Dilim 3 (Personel + Departman)
  planını yeni dalda yaz (`docs/superpowers/plans/`).
- **Referanslar:**
  - Tasarım dokümanı: `docs/superpowers/specs/2026-07-13-react-frontend-port-design.md`
  - Dilim 1 planı (tamamlandı): `docs/superpowers/plans/2026-07-13-react-port-dilim-1-iskelet.md`
  - Dilim 2 planı (tamamlandı): `docs/superpowers/plans/2026-07-14-react-port-dilim-2-overview-dashboard.md`
  - Backend faz planı: `raporlar/backend-development-plan.md`
- **Çalıştırma komutları:**
  - Backend: `cd backend && dotnet run --project src/IKPro.API --launch-profile http` → `http://localhost:5053`
  - Frontend: `cd frontend && npm run dev` → `http://localhost:5173`
  - Testler: `cd frontend && npm test -- --run`
  - Eski mock uygulama (parite referansı): kökteki `index.html` (statik sunucuyla aç)

---

## Kayıtlar (yeni → eski)

### 2026-07-14 — Dilim 2: Overview + Risk Merkezi + 5 risk detayı

- Plan yazıldı (`2026-07-14-react-port-dilim-2-overview-dashboard.md`) ve 7 görev
  TDD ile uygulandı; 33 birim test + `npm run build` yeşil.
- Sayfalar: `OverviewPage`, `RiskCenterPage` (5 paralel sorgu), `AttritionDetailPage`,
  `BurnoutDetailPage`, `ManagerLoadPage`, `EmployeeVoicePage`, `ComplianceRiskPage` —
  hepsi `src/features/` altında, `routes.tsx` `pageFor`'a kayıtlı.
- Altyapı: chart.js 4 + react-chartjs-2 5 (renkler `chartToken` ile CSS token'larından),
  `PageLoading/PageError`, test tarafında `stubApi` (path bazlı fetch stub) ve
  Vitest alias'lı `chartStub` (jsdom'da canvas yok).
- **Veri eşleme kararları** (mock ↔ backend farkları planın tablosunda): ısı haritası
  sürücü metni sayılardan türetilir; dashboard aksiyonları `GET /api/actions`'tan;
  Overview 4. KPI "Bugün İzinli"; Overview alt grid (onay kartları + doğum günleri)
  Dilim 4'e ertelendi; nabız "Riskli Ekipler" `level != low` departmanlardan türetilir;
  uyum sayfasının denetim çeklisti Dilim 6'ya bırakıldı (backend'de yok).
- Duman testi: 15 Playwright kontrolü geçti (grafikler, tablolar, rol geçişi,
  yetki ekranı, API kesintisinde PageError). Bulunan tek gerçek fark düzeltildi:
  Overview "Talepleri incele" `<button>` yerine eskisi gibi `<a>` (`Link`) oldu.
- Not: rol değiştirici testinde yarış var — değişimin bitmesi kullanıcı etiketinden
  beklenmeli (duman script'lerinde `user-profile strong` kontrolü).

### 2026-07-14 — Dilim 1 kapanışı: duman testi, parite, merge

- Task 7 tamamlandı: 15 kontrollük Playwright duman testi **hepsi geçti**
  (hr-admin girişi → dashboard; rol değiştirici → çalışan → `#/overview`;
  çalışanken `#/settings` → "Bu alan için yetki gerekli"; hatalı şifre →
  ProblemDetails mesajı; tema + daraltılmış menü yenilemede kalıcı; Ctrl+K →
  "izin" → İzinlerim → Enter; çıkış → login).
- Görsel parite: eski mock (4173) ile React portu (5173) login + shell ekran
  görüntüleri birebir; kod düzeltmesi gerekmedi. Tek bilinçli fark login metni
  (gerçek backend oturumu açıklaması) ve içerik alanındaki placeholder
  (dashboard Dilim 2'de portlanacak).
- `feature/react-port-dilim-1` main'e merge edildi (fast-forward + uzaktaki
  README commit'i `93e6ce1` merge edildi), merge sonrası 14/14 test yeşil,
  dal silindi.
- Durum: frontend iskeleti main'de; 5 test dosyası / 14 test.

### 2026-07-13 — Dilim 1: iskelet (Task 1–6)

- **Task 1:** Vite 6 + React 18.3 + TS strict iskeleti; 11 CSS dosyası
  değiştirilmeden `frontend/src/styles/`e kopyalandı; Vitest + RTL kuruldu;
  `/api` → `http://localhost:5053` proxy. (`bc11de9`, temizlik `a8c50ab`)
- **Task 2:** `npm run gen:api` — backend swagger'ından `src/api/schema.d.ts`
  tip üretimi; backend'de şema adı çakışması düzeltildi (`1743fd4`, `37b7048`).
- **Task 3 (TDD):** `session.ts` (`ikpro-session` localStorage) + `client.ts`
  fetch wrapper: Bearer, ProblemDetails → ApiError, 401'de tek-uçuş refresh +
  retry, refresh düşerse oturum temizle + `#/login`. (`13e130f`)
- **Task 4 (TDD):** `AuthContext` + `LoginPage` (login/signup) — gerçek API'ye
  bağlı, eski `AuthScreen` DOM paritesi; signup'a şifre alanı eklendi (backend
  zorunlu kılıyor). Review düzeltmeleri: sekme çubuğu + çift-form DOM paritesi.
  (`7ff6345`, `70a5839`)
- **Task 5 (TDD):** `routes.tsx` — eski `routes.js` ile birebir 17 korumalı
  rota, rol matrisi (`hr-admin`/`manager`/`employee`), `roleHomeFor`;
  `guards.tsx` — `RequireAuth`, `PublicOnly`, `RouteGate` (yetkisiz rol
  redirect edilmez, kilit ekranı görür); hash router. Review düzeltmesi:
  sayfa component'leri JSX olarak render (hook güvenliği). (`27ff298`,
  `10da700`, `71120a4`)
- **Task 6:** `AppShell` (sidebar/header/tema/rozet/rol değiştirici),
  `GlobalSearch` (Ctrl+K, bu dilimde yalnız sayfa sonuçları), `ToastProvider`.
  Review düzeltmeleri: nav DOM düzleştirme, toast timer temizliği, arama
  eşiği; build'i kıran tip hataları giderildi. (`8a21129`, `1029a81`,
  `27c18c8`)

### 2026-07-13 — Spec + Dilim 1 planı

- React'e dönüşüm tasarım dokümanı yazıldı:
  `docs/superpowers/specs/2026-07-13-react-frontend-port-design.md` (`9cf48f1`).
- Dilim 1 (iskelet) uygulama planı yazıldı:
  `docs/superpowers/plans/2026-07-13-react-port-dilim-1-iskelet.md` (`8054b26`).
- Dilimleme: 1 iskelet · 2 Overview+Dashboard · 3 Personel · 4 İzin+Puantaj ·
  5 Bordro · 6 İşe Alım+Uyum · 7 Aksiyonlar+Arama+Ayarlar · 8 kapanış.
  Her dilimin planı bir önceki bittikten sonra yazılır; sayfa portları
  `routes.tsx`'teki `pageFor` eşlemesine gerçek component'lerini kaydeder.

### Öncesi — Backend

- `backend/` altında .NET 9 Clean Architecture (EF Core + MSSQL, CPM);
  ayrıntı ve faz planı: `raporlar/backend-development-plan.md`.
- Eski frontend: kökteki `index.html` + `components/` + `styles/` (mock,
  dokunulmaz — parite referansı).
