# İK Pro — Geliştirme Günlüğü

> **Amaç:** Geliştirme kesilirse en son nerede kalındığını bu dosyadan anlamak.
> Her oturum sonunda buraya tarihli bir kayıt eklenir; en üstteki "Şu an neredeyiz"
> bölümü daima güncel tutulur.

## Şu an neredeyiz

- **Aktif iş:** React portu — **Dilim 7 tamamlandı**; dal `feature/react-port-dilim-7`
  main'e merge kararı bekliyor.
- **Son tamamlanan:** Dilim 7 (Aksiyon Merkezi + Global Arama + Ayarlar +
  Yönetici Konsolu) — 7 görev, 128 birim test, 10 kontrollük duman testi
  (aksiyon CRUD + ileri yönlü durum akışı, gerçek denetim izi, birleşik arama,
  ayarlarda kalıcılık + şifre değişikliği, manager onay paneli + CSV) ve
  parite kontrolü geçti.
- **Sıradaki adım:** Dilim 7'yi main'e merge et; sonra Dilim 8 (Kapanış:
  eski dosyalar `legacy-frontend/`e, README/dokümantasyon güncellemesi)
  planını yeni dalda yaz (`docs/superpowers/plans/`).
- **Referanslar:**
  - Tasarım dokümanı: `docs/superpowers/specs/2026-07-13-react-frontend-port-design.md`
  - Dilim 1 planı (tamamlandı): `docs/superpowers/plans/2026-07-13-react-port-dilim-1-iskelet.md`
  - Dilim 2 planı (tamamlandı): `docs/superpowers/plans/2026-07-14-react-port-dilim-2-overview-dashboard.md`
  - Dilim 3 planı (tamamlandı): `docs/superpowers/plans/2026-07-14-react-port-dilim-3-personel.md`
  - Dilim 4 planı (tamamlandı): `docs/superpowers/plans/2026-07-14-react-port-dilim-4-izin-puantaj.md`
  - Dilim 5 planı (tamamlandı): `docs/superpowers/plans/2026-07-14-react-port-dilim-5-bordro.md`
  - Dilim 6 planı (tamamlandı): `docs/superpowers/plans/2026-07-16-react-port-dilim-6-isealim-uyum.md`
  - Dilim 7 planı (tamamlandı): `docs/superpowers/plans/2026-07-16-react-port-dilim-7-aksiyonlar-arama-ayarlar.md`
  - Backend faz planı: `raporlar/backend-development-plan.md`
- **Çalıştırma komutları:**
  - Backend: `cd backend && dotnet run --project src/IKPro.API --launch-profile http` → `http://localhost:5053`
  - Frontend: `cd frontend && npm run dev` → `http://localhost:5173`
  - Testler: `cd frontend && npm test -- --run`
  - Eski mock uygulama (parite referansı): kökteki `index.html` (statik sunucuyla aç)

---

## Kayıtlar (yeni → eski)

### 2026-07-16 — Dilim 7: Aksiyon Merkezi + Global Arama + Ayarlar + Yönetici Konsolu

- 7 görev TDD ile tamamlandı; 128 birim test + build yeşil.
- `src/features/actions/`: `format.ts` (öncelik/durum etiketleri + ileri yönlü
  `open→week→done` geçiş yardımcıları) + `queries.ts` (liste/audit/CRUD/status),
  `ActionsPage` (KPI'lar listeden, server-side öncelik/kaynak/sahip filtreleri,
  Açık/Bu Hafta/Tamamlanan sekmeleri, Denetim İzi gerçek `audit-logs` verisiyle
  yalnız hr-admin+manager; kartlarda durum ilerletme MGMT + silme hr-admin,
  "Kaynağa git" `sourceRoute`tan), `ActionModal` (Yeni Aksiyon, hr-admin);
  `/risk/action-center` aynı sayfaya bağlandı (eski routes.js paritesi).
- `layout/GlobalSearch`: 300ms debounce ile gerçek `GET /search` sonuçları
  (personel rol kapsamlı + aksiyon + aday) sayfa sonuçlarının altına eklenir;
  API hatası sessizce yutulur.
- `src/features/settings/`: `SettingsPage` — şirket profili (PUT + header
  "Değişiklikleri Kaydet"), logo yükleme (FormData) + korumalı logo
  `apiDownload`→blob URL ile gösterim (`<img src>` Bearer gönderemez),
  bildirim toggle'ları değişimde anında PUT, şifre değişikliği gerçek
  `POST /auth/change-password` (yanlış mevcut şifre → backend mesajı),
  2FA toggle, abonelik salt-okur.
- `src/features/manager/`: `ManagerPage` — "Şu An İzinli" (`overview.onLeaveToday`)
  ve "Onay Bekliyor" gerçek; Onay Bekleyenler paneli `usePendingLeaves` +
  `useDecideLeave`; trend grafiği/ısı haritası/departman tablosu ve 2 KPI
  **demo** (backend'de izin-analitik ucu yok, Demo pill'li); CSV dışa aktarım
  gerçek (`downloadCsv` uzantıyı kendisi ekler — çift `.csv.csv` düzeltildi).
- **Kararlar / bilinçli farklar:** aksiyon mutasyonları + "Yeni Aksiyon" eskide
  yoktu; Denetim İzi employee'ye kapalı (uç Management); manager'daki işlevsiz
  ay/departman seçicileri kaldırıldı; ayarlar gerçek kalıcı (eskide sahte).
- Duman: 10 kontrol geçti. Playwright notları: gizli switch checkbox'ları
  (`.switch` CSS) `force`/`evaluate(el.click())` ister; dilim 1 rota testi
  settings placeholder beklentisi gerçek sayfaya güncellendi.
- Parite: `#/actions`, `#/settings`, `#/manager` eskiyle yan yana birebir;
  tüm farklar plandaki bilinçli farklar. Kod düzeltmesi gerekmedi.

### 2026-07-16 — Dilim 6: İşe Alım (ATS) + Uyum

- 7 görev TDD ile tamamlandı; 107 birim test + build yeşil.
- `src/features/recruitment/`: `format.ts` (`formatTimeAgo`, `statusTagClass`,
  `scoreClass`) + `queries.ts` (aday listesi/detay, oluşturma, durum PATCH,
  not POST, hire POST — hire personel query'lerini de invalidate eder),
  `RecruitmentPage` (ATS yerleşimi, server-side arama 300ms debounce +
  Tümü/Yeni/Mülakat filtre sekmeleri, boş durum + "Yeni Aday"),
  `CandidateDetail` (Özgeçmiş/Mülakat Notları/Değerlendirme/Geçmiş sekmeleri,
  not ekleme, pipeline durum select'i, İşe Al modalı — departman/ünvan/tarih →
  personel kaydı), `CandidateModal` (yeni aday formu).
- `src/features/compliance/`: `queries.ts` (belgeler + readiness + 4 mutasyon),
  `CompliancePage` (KPI'lar readiness'tan, durum/risk/arama filtreleri,
  hr-admin'de satır içi durum select + düzenleme + "Yeni Belge",
  manager salt-okur; Yaklaşan Son Tarihler dueDate'li belgelerden türetilir),
  `DocumentModal` (oluşturma POST / düzenleme PUT + sorumlu değişince owner
  PATCH). Dashboard'daki salt-okur `ComplianceRiskPage` silindi; rota
  `/risk/compliance` yeni sayfaya bağlandı (`useComplianceRisk` özet kartı
  için `dashboard/queries`te kaldı).
- **Kararlar / bilinçli farklar:** aday seed'i yok → boş durum + "Yeni Aday"
  (eskide statik 4 aday); pipeline durum select'i ve uyum filtre/mutasyon
  UI'ları eskide yoktu; pozisyon/funnel uçları UI'da kullanılmadı (eski
  ekranda yoktu — YAGNI); değerlendirme sekmesi salt görüntüleme (ekleme
  eskide de yoktu); `.status-tag.hired` ve `.compliance-toolbar` stilleri
  token'larla `main.css`e eklendi.
- Duman: 11 kontrol geçti — aday oluştur/otomatik seçim, arama+filtre, not +
  geçmiş kaydı, 2 pipeline geçişi, işe alım (personelde görünür, select/buton
  pasif) + reddedilen adayda 409, manager recruitment kilidi, uyum tablo +
  filtre, Tamamlandı→risk Düşük + aynı durum 409 toast'ı, yeni belge +
  mükerrer 409, düzenleme (PUT+owner PATCH), manager salt-okur uyum.
  Not: Playwright `has-text` büyük/küçük harf duyarsız alt dizgi eşleşmesi
  yüzünden "Geçmiş" tıklaması "Özgeçmiş"e gidiyordu — script `exact: true`
  ile düzeltildi (uygulama hatası değildi).
- Parite: `#/recruitment` ve `#/risk/compliance` eski uygulamayla yan yana
  birebir; tüm farklar plandaki bilinçli farklar. Tek düzeltme: uyum filtre
  çubuğu yatay düzen CSS'i.

### 2026-07-16 — Dilim 5: Bordro

- 7 görev TDD ile tamamlandı; 88 birim test + build yeşil.
- `src/features/payroll/`: `format.ts` + `queries.ts` (dönem/satır/preview/ayar
  uçları), `PayrollPage` (sekmeler, dönem seçici + "Yeni Dönem", dönem onaya
  gönderme, çalışan rolünde `MyPayslipsView`), `PeriodTab` (KPI, akış, kontrol
  merkezi, parametreler, çalışan tablosu), `PayrollDetailPanel` (özet +
  kazanç/kesinti/matrah kartları + pusula önizleme + **yeni** girdi düzenleme
  formu; Kaydet `PUT` row, Onaya gönder `POST` approve, onaylı satırda Pusula
  İndir PDF), `CalculatorTab` (300ms debounce ile `POST /payroll/preview`,
  personel seçiminde formu satırdan doldurma), `SettingsTab` (12 alan,
  `PUT /payroll/settings`, varsayılana dön).
- **Kararlar / bilinçli farklar:** dönem seçici + "Yeni Dönem" eskide yoktu;
  detay panelinde girdi formu (eskide salt-okur); çalışan görünümü "Bordrolarım"
  (eskide demo tablo); ayar metni "yıla göre saklanır" (eskide tarayıcıda).
  Brüt 0 satırlarda backend SGK taban/istisna hesabı nedeniyle net negatif
  görünebilir — backend davranışı, girdi girilince normalleşir.
- Test düzeltmeleri: detay testinde isim başlık rolüyle sorgulanıyor (isim
  pusula önizlemede de geçiyor); calculator testi son preview POST'una bakıyor
  (ilk debounce brüt=0 ile yarışıyordu).
- Duman: 10 kontrol geçti — dönem oluşturma, girdi kaydetme, kontrol, 409
  "eksik veri" mesajı sonrası IBAN/puantaj işaretleyip satır onayı (4/4),
  dönem onayı + pusula PDF indirme, tekil hesaplama (backend sonucu ₺77.383),
  ayar kaydet + yenilemede kalıcılık, çalışan Bordrolarım + PDF + settings
  yetki kilidi.
- Parite: üç sekme + detay paneli eski uygulamayla yan yana birebir; tüm
  farklar plandaki bilinçli farklar. Kod düzeltmesi gerekmedi.

### 2026-07-14 — Dilim 4: İzin & Onay + Puantaj

- 6 görev TDD ile tamamlandı; 64 birim test + build yeşil.
- `src/features/leaves/`: `LeavesPage` (bakiye kartları `GET /leaves/balance`,
  hareket tablosu + pending iptal, "Ofiste Kimler Yok?" `GET /leaves/team`),
  `LeaveRequestModal` (türler `GET /leaves/types`tan dinamik, takvim günü süre
  ön izlemesi, MGMT'de yerine bakacak kişi listesi, gerçek `POST /leaves`),
  `format.ts`, `queries.ts`.
- `src/features/attendance/`: `AttendancePage` (canlı pano `GET /attendance/live`,
  özet şeridi `GET /attendance/summary` toplamları, gerçek yıl/ay değiştirici,
  aylık puantaj tablosu + CSV), `AttendanceEntryModal` (manuel giriş `POST`,
  satır düzenleme `PUT`), `format.ts`, `queries.ts`.
- Overview'a MGMT rollerine gerçek "Bekleyen Aksiyonlar" kartı eklendi
  (`GET /leaves/pending` + onay/red → `["leaves"]` + dashboard invalidation).
- CSV yardımcıları `features/shared/csv.ts`'e taşındı (personel + puantaj ortak).
- **Kararlar:** kullanılan toplam tek pill (tür kırılımı backend'de yok);
  "Önemli Günler" (doğum günü) eklenmedi (backend'de yok); manuel giriş modalı
  yeni (eskide ghost kart işlevsizdi); kesin izin gün sayısını backend hesaplar
  (UI yalnız takvim günü ön izler). hr-admin/manager hesabına bağlı personel
  kaydı olmadığından `#/leaves` bu rollerde backend'in 403 mesajını gösterir
  ("Hesabınıza bağlı personel kaydı yok...") — bilinçli backend davranışı.
- Duman: 16 kontrol geçti — çalışan talep oluştur/iptal, hr-admin onayı sonrası
  çalışan tarafında "Onaylandı", ekip yokluk widget'ı, canlı pano, ay değiştirici,
  manuel giriş → satır → düzenleme, CSV, çalışan `#/attendance` yetki ekranı.
  Not: backend puantaj seed'i yok; satırlar manuel giriş/gerçek check-in ile oluşur.
- Parite: `#/leaves` (+modal) ve `#/attendance` (iki sekme) yan yana birebir;
  tüm farklar veri kaynaklı (gerçek ay etiketi, gerçek personel) veya plandaki
  bilinçli farklar. Kod düzeltmesi gerekmedi.

### 2026-07-14 — Dilim 3: Personel + Departman

- 6 görev TDD ile tamamlandı; 49 birim test + build yeşil.
- Altyapı: `apiFetch`'e FormData desteği (Content-Type basılmıyor) + `apiDownload`
  (Bearer + 401-refresh + Content-Disposition dosya adı çözümü) — sonraki dilimlerde
  bordro pusulası vb. indirmeler de bunu kullanacak.
- `src/features/personnel/`: `PersonnelPage` (server-side arama 300ms debounce +
  departman/durum filtreleri, toplu seçim + `bulk-deactivate`, CSV dışa aktarma),
  `PersonnelModal` (6 sekme, gerçek POST/PUT, ProblemDetails → form-error, foto
  yükleme), `DocumentsTab` (evrak listesi + yükleme + indirme), `csv.ts`, `queries.ts`.
- **Kararlar:** TC listede maskeli (`nationalIdMasked`); mutasyon eylemleri yalnız
  hr-admin, manager kartı salt-okur; "Yakınlık / Telefon" tek input →
  `emergencyContactPhone`; foto önizleme ikonu kalır (backend foto servis ucu yok);
  sayfalama UI'sı yok (`pageSize=50`, seed küçük); departman CRUD ekranı yok
  (eskide de yoktu).
- Duman: 15 kontrol geçti — CRUD, 409 çakışma mesajı, evrak yükle/indir (backend
  yalnız pdf/jpg/png/doc kabul ediyor), foto, toplu pasife alma, CSV, rol kısıtları.
- Parite düzeltmeleri: "Fotoğraf Yükle" span'e çevrildi (buton çerçevesi
  görünüyordu). Eski uygulamanın bulk-bar/boş-durum görünürlük davranışı birebir
  korunuyor.

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
