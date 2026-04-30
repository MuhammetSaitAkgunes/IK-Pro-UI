# İK Pro UI

İK Pro UI, kurumsal İK operasyonları için tasarlanmış premium SaaS arayüz prototipidir. Proje; risk odaklı İK dashboard'u, genel durum görünümü, personel, işe alım, mesai, izin, bordro, global aksiyon merkezi, denetim izi, login/signup ve rol bazlı demo görünüm katmanlarını içerir.

Bu sürüm backend içermez. Frontend, mock veri ve localStorage ile çalışır; ancak ileride C# .NET 9 Clean Architecture / MVC / Onion Architecture backend'ine bağlanabilecek şekilde service adapter mantığıyla hazırlanmıştır.

## İçindekiler

- [Öne Çıkan Özellikler](#öne-çıkan-özellikler)
- [Teknoloji ve Mimari](#teknoloji-ve-mimari)
- [Proje Yapısı](#proje-yapısı)
- [Çalıştırma](#çalıştırma)
- [Demo Login ve Roller](#demo-login-ve-roller)
- [Routing Yapısı](#routing-yapısı)
- [Modüller](#modüller)
- [Bordro Modülü](#bordro-modülü)
- [Service Adapter ve Backend Hazırlığı](#service-adapter-ve-backend-hazırlığı)
- [State ve Veri Yönetimi](#state-ve-veri-yönetimi)
- [UI/UX Tasarım Prensipleri](#uiux-tasarım-prensipleri)
- [Test Checklist](#test-checklist)
- [Bilinen Sınırlar](#bilinen-sınırlar)
- [Önerilen Sonraki Fazlar](#önerilen-sonraki-fazlar)

## Öne Çıkan Özellikler

- Login/signup ekranı ve demo session akışı
- Rol bazlı frontend görünüm simülasyonu
- Hash tabanlı client-side routing
- Browser refresh ve back/forward desteği
- Risk & Aksiyon Merkezi
- Önceki operasyonel dashboard'un `Genel Durum` olarak korunması
- Risk detay sayfaları:
  - Ayrılma riski
  - Tükenmişlik sinyali
  - Yönetici yükü
  - Aksiyon merkezi detayı
  - Çalışan nabzı
  - Uyum / evrak / denetim riski
- Global Aksiyon Merkezi
- Denetim izi / aktivite geçmişi
- Personel yönetimi
- İzinler
- Mesai & Puantaj
- İşe alım
- Yönetici konsolu
- Bordro modülü
- Tekil bordro hesaplama ekranı
- Bordro default ayarları
- Light/dark mode
- Açılır/kapanır sidebar
- .NET backend'e bağlanmaya hazır API client şablonu

## Teknoloji ve Mimari

Bu proje şu anda framework kullanmayan vanilla frontend yapısındadır:

- HTML
- CSS
- Vanilla JavaScript
- Chart.js
- Font Awesome
- localStorage
- Hash routing

Frontend tarafı basit ama bilinçli şekilde katmanlara ayrılmıştır:

- `components/`: Ekran ve UI bileşenleri
- `services/`: Auth, API client ve mock veri servisleri
- `styles/`: Ortak ve modül bazlı stiller
- `routes.js`: Merkezi route config
- `main.js`: App bootstrap, routing, auth guard, shell yönetimi

Bu ayrım, ileride backend geldiğinde component'lerin mümkün olduğunca az etkilenmesini hedefler.

## Proje Yapısı

```text
.
├── index.html
├── main.js
├── routes.js
├── README.md
├── components/
│   ├── actions.js
│   ├── attendance.js
│   ├── auth.js
│   ├── dashboard.js
│   ├── layout.js
│   ├── leaves.js
│   ├── manager.js
│   ├── payroll.js
│   ├── personnel.js
│   ├── recruitment.js
│   └── settings.js
├── services/
│   ├── apiClient.js
│   ├── authService.js
│   └── mockData.js
└── styles/
    ├── actions.css
    ├── attendance.css
    ├── auth.css
    ├── layout.css
    ├── leaves.css
    ├── main.css
    ├── manager.css
    ├── payroll.css
    ├── personnel.css
    ├── recruitment.css
    └── settings.css
```

## Çalıştırma

Proje statik dosya olarak çalışır.

1. `index.html` dosyasını tarayıcıda açın.
2. Login ekranında `Giriş yap` butonuna basın.
3. Uygulama `Risk Merkezi` ekranı ile açılır.

Alternatif olarak herhangi bir statik sunucu ile de çalıştırılabilir:

```bash
npx serve .
```

veya:

```bash
python -m http.server 3000
```

Ardından:

```text
http://localhost:3000
```

## Demo Login ve Roller

Login/signup gerçek kimlik doğrulama yapmaz. Demo session localStorage'a yazılır.

Varsayılan kullanıcı:

```text
Ad: İK Yöneticisi
Rol: hr-admin
Görünen rol: İK Admin
```

Header alanında demo rol değiştirici bulunur:

- `hr-admin`
- `manager`
- `employee`

Rol bazlı görünüm simülasyonu:

| Rol | Açıklama |
|---|---|
| `hr-admin` | Tüm ana modülleri görür. |
| `manager` | Risk, ekip, personel, izin, mesai ve aksiyon odaklı görünüm alır. Bordro ve ayarlar kısıtlıdır. |
| `employee` | Genel durum, aksiyonlar, izinler ve bordro self-servis kapsamındaki sınırlı görünümü alır. |

Not: Bu yalnızca frontend simülasyonudur. Gerçek ürün güvenliği backend authorization policy ile sağlanmalıdır.

## Routing Yapısı

Proje hash tabanlı client-side routing kullanır. Bu tercih, projenin doğrudan `index.html` olarak açılabilmesini sağlar.

Örnek route'lar:

```text
#/login
#/signup
#/dashboard
#/overview
#/actions
#/personnel
#/recruitment
#/attendance
#/leaves
#/payroll
#/payroll/calculator
#/payroll/settings
#/manager
#/settings
#/risk/attrition
#/risk/burnout
#/risk/manager-load
#/risk/action-center
#/risk/employee-voice
#/risk/compliance
```

Route config merkezi olarak `routes.js` içinde tutulur.

Her route şu bilgileri taşıyabilir:

- `key`
- `path`
- `title`
- `eyebrow`
- `navKey`
- `roles`
- `public`
- `authMode`
- `render`
- `afterRender`

Auth guard davranışı:

- Session yokken protected route açılırsa `#/login` gösterilir.
- Login olan kullanıcı `#/login` veya `#/signup` açarsa `#/dashboard` yönlendirilir.
- Yetkisiz route açılırsa `Yetki Gerekli` ekranı gösterilir.
- Refresh sonrası mevcut route korunur.
- Browser back/forward desteklenir.

## Modüller

### Risk Merkezi

Ana karar dashboard'udur. Şu soruya cevap vermeyi hedefler:

```text
Hangi risk büyüyor, neden büyüyor ve hangi aksiyon alınmalı?
```

İçerikler:

- İK Risk Skoru
- Yönetici Yük Endeksi
- Ayrılma Riski
- Kritik Aksiyonlar
- 90 günlük risk trendi
- Departman bazlı risk ısı haritası
- Aksiyon Merkezi
- Yetenek ve kapasite sinyalleri
- Kurumsal sinyaller

### Genel Durum

Operasyonel İK dashboard'udur.

İçerikler:

- Aktif personel
- Onay bekleyenler
- Açık pozisyonlar
- Kritik hatırlatmalar
- Departman dağılımı
- İşe alım hunisi
- Anlık çalışma durumu
- Bekleyen aksiyonlar

### Global Aksiyon Merkezi

Tüm modüllerden gelen aksiyonları tek merkezde toplar.

İçerikler:

- Bugün kapanacak aksiyonlar
- Geciken aksiyonlar
- Yüksek öncelikler
- Tamamlanan işler
- Açık / Bu Hafta / Tamamlanan sekmeleri
- Denetim izi sekmesi
- Kaynak modüle hızlı geçiş

Mock kaynaklar:

- Risk Merkezi
- Bordro
- Uyum
- Çalışan Nabzı
- İzin / Mesai

### Denetim İzi

Global Aksiyon Merkezi içinde timeline olarak gösterilir.

Örnek kayıtlar:

- Kullanıcı giriş yaptı
- Bordro ayarı güncellendi
- Bordro ön hesabı çalıştırıldı
- Uyum evrak durumu değişti
- Risk aksiyonu onaya gönderildi

### Personel Yönetimi

Personel listesi ve yeni personel ekleme modalı içerir.

### İzinler

İzin bakiyesi, bekleyen talepler, ekip yokluğu ve izin talebi oluşturma akışını içerir.

### Mesai & Puantaj

Canlı çalışma durumu ve aylık puantaj görünümünü içerir.

### İşe Alım

Aday takip, CV, mülakat notları, değerlendirme ve geçmiş sekmeleri içerir.

### Yönetici Konsolu

Yönetici odaklı ekip, onay ve performans/operasyon takip görünümüdür.

### Ayarlar

Genel ayarlar, bildirim, güvenlik ve faturalama benzeri demo sekmeleri içerir.

## Bordro Modülü

Bordro modülü Türkiye özel sektör 4/a bordro akışı için demo ön hesap mantığıyla tasarlanmıştır.

Route'lar:

```text
#/payroll
#/payroll/calculator
#/payroll/settings
```

### Dönem Bordrosu

İçerikler:

- Dönem durumu
- Çalışan sayısı
- Toplam brüt
- Toplam net
- İşveren maliyeti
- Bordro akışı
- Kontrol merkezi
- Çalışan bordro tablosu
- Çalışan bordro detay paneli
- Bordro pusulası önizleme

### Tekil Hesaplama

Tekil bordro senaryosu oluşturmak için kullanılır.

Form alanları:

- Personel
- Brüt ücret
- Çalışılan gün
- Fazla mesai saati
- Fazla mesai çarpanı
- Prim
- Yol yardımı
- Yemek yardımı
- Yan hak / ek ödeme
- Özel kesinti
- Önceki gelir vergisi matrahı

Fazla mesai hesabı:

```text
Saatlik ücret = Brüt ücret / Aylık çalışma saati
Fazla mesai tutarı = Saatlik ücret × Fazla mesai saati × Fazla mesai çarpanı
```

### Bordro Ayarları

localStorage ile saklanan varsayılan ayarları içerir:

- Fazla mesai çarpanı
- Aylık çalışma saati
- Varsayılan çalışılan gün
- SGK işçi oranı
- İşsizlik işçi oranı
- SGK işveren oranı
- İşsizlik işveren oranı
- Damga vergisi oranı
- SGK PEK alt sınırı
- SGK PEK üst sınırı
- Asgari ücret gelir vergisi istisnası
- Asgari ücret damga vergisi istisnası

Not: Bordro hesapları demo/ön hesap niteliğindedir. Üretim kullanımında mevzuat ve müşavir doğrulaması gerekir.

## Service Adapter ve Backend Hazırlığı

Backend bu projede yoktur. Ancak frontend servis katmanı, ileride .NET 9 backend'e bağlanacak şekilde isimlendirilmiştir.

### `services/apiClient.js`

Ortak API istemci şablonudur.

Hazır fonksiyonlar:

```js
apiGet(path)
apiPost(path, body)
apiPut(path, body)
apiDelete(path)
getAuthHeaders()
```

Varsayılan API base:

```js
const API_BASE_URL = window.IKPRO_API_BASE_URL || "https://localhost:7001/api";
```

Beklenen backend endpoint örnekleri:

```text
POST  /api/auth/login
POST  /api/auth/register
GET   /api/me
GET   /api/actions
GET   /api/audit-logs
PATCH /api/actions/{id}/status
```

### `services/authService.js`

Demo auth ve rol simülasyonu içerir.

Hazır fonksiyonlar:

```js
login(credentials)
signup(payload)
logout()
getCurrentUser()
isAuthenticated()
hasRole(roleOrRoles)
canAccessRoute(route)
canAccessPage(page)
switchDemoRole(role)
```

### `services/mockData.js`

Demo kullanıcı, global aksiyon ve audit log verilerini içerir.

## .NET 9 Backend Entegrasyon Notları

Planlanan backend mimarisi:

```text
Clean Architecture + MVC + Onion Architecture
```

Önerilen çözüm yapısı:

```text
backend/
├── IkPro.Api
├── IkPro.Application
├── IkPro.Domain
├── IkPro.Infrastructure
└── IkPro.Persistence
```

Frontend açısından önerilen entegrasyon sırası:

1. Auth endpointleri bağlanır.
2. Demo session yerine JWT veya secure cookie kullanılır.
3. `GET /api/me` ile kullanıcı/rol bilgisi backend'den okunur.
4. Global aksiyonlar `GET /api/actions` ile beslenir.
5. Audit log `GET /api/audit-logs` ile beslenir.
6. Bordro, personel, izin, mesai ve risk verileri modül bazlı API servislerine ayrılır.

Backend güvenliği için öneriler:

- Rol ve yetki kontrolleri backend policy ile yapılmalı.
- Bordro ve çalışan verileri için endpoint seviyesinde authorization şart olmalı.
- Audit log backend tarafında otomatik üretilmeli.
- Frontend rol görünümü sadece UX kolaylığı olarak kalmalı.

## State ve Veri Yönetimi

Bu sürümde kullanılan localStorage anahtarları:

| Anahtar | Amaç |
|---|---|
| `ikpro-demo-session` | Demo kullanıcı session bilgisi |
| `ikpro-theme` | Light/dark tema tercihi |
| `ikpro-sidebar` | Sidebar expanded/collapsed tercihi |
| `ikpro-payroll-settings` | Bordro varsayılan ayarları |

Mock veriler runtime sırasında kalıcı olarak güncellenmez.

## UI/UX Tasarım Prensipleri

Arayüz operasyonel SaaS mantığıyla tasarlanmıştır:

- Yoğun ama okunabilir bilgi hiyerarşisi
- Kartların sınırlı ve işlevsel kullanımı
- Dashboard'larda karar ve aksiyon önceliği
- Premium ama sakin renk paleti
- Light/dark mode desteği
- Tablo, form, badge ve button stillerinde ortak tasarım dili
- Rol bazlı görünüm simülasyonu
- Mobilde yatay taşmayı önleyen responsive davranış

Ortak sınıflar:

```text
btn
btn-primary
btn-secondary
input-control
status-pill
data-table
page-header
surface
card
stat-box
```

## Test Checklist

Manuel test için önerilen akış:

### Auth

- Session yokken `#/login` açılmalı.
- `Giriş yap` dashboard'a almalı.
- `Hesap oluştur` demo session oluşturmalı.
- `Çıkış yap` tekrar login ekranına döndürmeli.

### Routing

- `#/dashboard` açılmalı.
- `#/actions` açılmalı.
- `#/payroll/calculator` refresh sonrası aynı sekmede kalmalı.
- `#/payroll/settings` açılmalı.
- `#/risk/attrition` risk detayını açmalı.
- Browser back/forward çalışmalı.

### Rol Simülasyonu

- `hr-admin` tüm ana modülleri görmeli.
- `manager` bordro ve ayarlar gibi kısıtlı alanları görmemeli.
- `employee` sadece sınırlı self-servis menüleri görmeli.
- Yetkisiz route açıldığında `Yetki Gerekli` ekranı görünmeli.

### Dashboard ve Modüller

- Risk Merkezi grafik ve kartları dolu görünmeli.
- Genel Durum dashboard'u çalışmalı.
- Personel modalı açılıp kapanmalı.
- İzin modalı çalışmalı.
- Mesai tabları çalışmalı.
- İşe alım tabları çalışmalı.
- Bordro detay paneli açılıp kapanmalı.
- Tekil bordro hesaplama sonuç üretmeli.
- Bordro ayarları localStorage'da kalmalı.
- Aksiyon Merkezi sekmeleri çalışmalı.
- Denetim izi dolu görünmeli.

### UI

- Light/dark mode bozulmamalı.
- Sidebar collapse/expand çalışmalı.
- Desktop/tablet/mobile genişliklerde taşma olmamalı.

## Geliştirici Notları

Bu proje şu anda build adımı gerektirmez. JS dosyaları doğrudan script olarak yüklenir.

Sözdizimi kontrolü için:

```bash
node --check main.js
node --check routes.js
```

PowerShell ile tüm component/service JS dosyalarını kontrol etmek için:

```powershell
Get-ChildItem services,components -Filter *.js | ForEach-Object { node --check $_.FullName }
```

## Bilinen Sınırlar

- Gerçek backend yoktur.
- Auth gerçek güvenlik sağlamaz.
- Rol bazlı görünüm frontend simülasyonudur.
- Bordro hesapları demo/ön hesap niteliğindedir.
- Aksiyonlar ve audit log gerçek olarak persist edilmez.
- Dosya yükleme, bildirim, banka dosyası, muhasebe fişi ve resmi bildirge entegrasyonları yoktur.
- Hash routing kullanılır; path-based routing'e geçiş backend host kararına göre yapılmalıdır.

## Önerilen Sonraki Fazlar

1. .NET 9 backend solution oluşturma
2. Auth ve kullanıcı/rol yönetimi
3. Gerçek action/audit log API'leri
4. Personel master data API'si
5. Bordro hesap motorunun backend'e taşınması
6. Bordro pusulası PDF üretimi
7. Dosya/evrak yükleme altyapısı
8. Bildirim ve görev hatırlatma sistemi
9. Veri tutarlılığı kontrol motoru
10. Path-based routing'e geçiş ve backend fallback yapılandırması

## Kısa Ürün Vizyonu

İK Pro, klasik İK operasyon ekranlarından öteye geçerek şu üç alanı bir araya getirmeyi hedefler:

```text
Operasyonel İK + Risk/Aksiyon Intelligence + Bordro ve Uyum Kontrolü
```

Bu sürüm, gerçek backend öncesi ürün deneyimini göstermek ve .NET tabanlı kurumsal mimariye hazırlanmak için tasarlanmış frontend prototipidir.
