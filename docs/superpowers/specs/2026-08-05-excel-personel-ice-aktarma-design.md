# Excel'den Personel İçe Aktarma — Tasarım

**Tarih:** 2026-08-05
**Durum:** Onaylandı, uygulamaya hazır

## Problem

200 kişilik bir firma personelini tek tek ekrandan giremez. İçe aktarma olmadan
satış demosu ilerlemiyor; bu, faturalandırmadan bile önce gelen bir benimseme
engeli.

## Kapsam

**Dahil:** Excel (`.xlsx`) dosyasından toplu **özlük kartı** oluşturma; önizleme
ile doğrulama; hata ve mükerrer raporu; şablon indirme.

**Hariç (bilinçli):**
- Giriş hesabı / davet e-postası oluşturma — özlük kartı yeterli; hesap açma
  ayrı bir özellik olarak sonra ele alınır.
- Yönetici (`ManagerId`) atama — isimle eşleştirme belirsizlik doğurur (aynı
  isimden iki kişi) ve aynı dosyadaki kişiye referans tavuk-yumurta yaratır.
  İçe aktarmadan sonra ekrandan atanır.
- Departman otomatik oluşturma — "Yazılım" / "Yazilim" / "yazılım " yazım
  farkları ayrı departmanlar yaratır ve temizlemesi zordur.
- CSV desteği — Türkçe karakter kodlaması (UTF-8 / Windows-1254 / BOM) kronik
  destek yükü. Gerekirse sonra eklenir.
- Güncelleme (upsert) — Excel'de eksik bırakılmış bir alan sistemdeki doğru
  veriyi silebilir. Mevcut kayıt ATLANIR, değiştirilmez.

## Kararlar ve gerekçeleri

| Karar | Seçim | Gerekçe |
| --- | --- | --- |
| Kapsam | Yalnız özlük kartı | En hızlı değer, en az risk |
| Hata akışı | Önizleme → onay → aktar | 200 satırlık elle tutulmuş dosyada hata kesindir; sürpriz olmamalı |
| Departman | Var olmalı, yoksa satır hatası | Otomatik oluşturma çöp veri üretir |
| Yönetici | v1'de yok | İsim eşleştirmesi belirsiz |
| Mükerrer | TC ile tespit, **atla** | Aynı dosyanın iki kez yüklenmesi sık kaza; upsert veri silebilir |
| Mimari | Durumsuz çift yükleme | Sunucuda geçici durum yok; önizleme = aktarım eksi kayıt |
| Format | Yalnız `.xlsx` | CSV kodlama yükü |
| Kütüphane | ClosedXML (MIT) | EPPlus artık ticari lisanslı |

## Mimari

```
Excel seç → POST /preview → rapor → (düzelt, tekrar) → POST /import → sonuç
```

İki uç da **aynı** ayrıştırıcı ve doğrulayıcıyı kullanır; `preview` tek farkla
`import`'tur: kaydetmez. Bu sayede "önizlemede temiz görünüp aktarımda patlama"
durumu yapısal olarak imkânsızdır.

Sunucuda geçici durum tutulmaz. Alternatif olan token'lı oturum, çok-kiracılı
sistemde geçici durumun izolasyonu, süre aşımı ve temizliği gibi yeni
sorumluluklar (ve sızıntı riski) getirirdi.

### Bileşenler

| Birim | Sorumluluk |
| --- | --- |
| `EmployeeImportTemplate` | Şablon `.xlsx` üretir (başlıklar + örnek satır) |
| `EmployeeImportParser` | `.xlsx` → ham satır listesi (`ImportRow`). Sadece okur, yorumlamaz |
| `EmployeeImportValidator` | Ham satır + departman sözlüğü + mevcut TC kümesi → geçerli/hatalı ayrımı |
| `PreviewEmployeeImportCommand` | Ayrıştır + doğrula → `ImportPreviewDto` |
| `ImportEmployeesCommand` | Ayrıştır + doğrula + geçerli satırları kaydet → `ImportResultDto` |

Ayrıştırma ve doğrulama ayrı tutulur: ayrıştırıcı dosya biçimini, doğrulayıcı iş
kuralını bilir. Böylece doğrulayıcı, Excel olmadan (düz nesnelerle) test edilir.

## Şablon sütunları

| Sütun | Zorunlu | Kural |
| --- | --- | --- |
| Ad | ✅ | boş olamaz, ≤128 |
| Soyad | ✅ | boş olamaz, ≤128 |
| Unvan | ✅ | boş olamaz, ≤128 |
| Departman | ✅ | sistemde mevcut olmalı (büyük/küçük harf ve baş/son boşluk duyarsız) |
| İşe Giriş Tarihi | ✅ | tarih olarak okunabilmeli |
| TC Kimlik No | — | boşsa geçer; doluysa 11 hane rakam |
| Durum | — | boşsa `active`; `active` veya `passive` |
| Kişisel E-posta | — | boşsa geçer; doluysa geçerli e-posta |
| Telefon | — | serbest metin |
| IBAN | — | boşsa geçer; doluysa `TR` + 24 hane |

Şablon uçtan indirilir ki başlıklar sistemle garantili aynı olsun.

## Doğrulama katmanları

1. **Dosya:** uzantı `.xlsx`, boyut ≤ 5 MB, veri satırı ≤ 1000
2. **Başlık:** zorunlu sütunlar mevcut mu
3. **Satır:** zorunlu alanlar ve biçimler
4. **Referans:** departman adı çözümleniyor mu
5. **Mükerrer (sistem):** TC kiracıda kayıtlı mı → **atla**
6. **Mükerrer (dosya içi):** aynı TC iki kez → ikincisi hata

Kiracı izolasyonu ek iş gerektirmez: departman araması ve TC kontrolü global
sorgu filtresinden geçer.

## Sözleşmeler

```csharp
public sealed record ImportRowIssueDto(int SatirNo, string Alan, string Mesaj);

public sealed record ImportPreviewDto(
    int ToplamSatir, int GecerliSatir, int HataliSatir, int MukerrerSatir,
    IReadOnlyList<string> BilinmeyenDepartmanlar,
    IReadOnlyList<ImportRowIssueDto> Sorunlar);

public sealed record ImportResultDto(
    int OlusturulanSatir, int AtlananSatir,
    IReadOnlyList<ImportRowIssueDto> Sorunlar);
```

`SatirNo` **Excel satır numarasıdır** (başlık 1. satır → ilk veri 2. satır), ki
kullanıcı hatayı dosyada doğrudan bulabilsin.

## Uçlar

| Uç | Yetki | Dönen |
| --- | --- | --- |
| `GET /api/employees/import/template` | hr-admin | `.xlsx` dosya |
| `POST /api/employees/import/preview` | hr-admin | `ImportPreviewDto` |
| `POST /api/employees/import` | hr-admin | `ImportResultDto` |

Yetki, mevcut personel oluşturma politikasıyla (`Policies.HrAdminOnly`) aynıdır.

## İşlem bütünlüğü

`import` tek transaction'da koşar. Geçerli satırlar kaydedilir; beklenmedik bir
veritabanı hatasında **hiçbiri** kaydedilmez. Yarım aktarım durumu oluşmaz.

Doğrulama düzeyinde ise "geçerli satırlar" mantığı geçerlidir: hatalı ve mükerrer
satırlar atlanır, aktarımı engellemez.

## Arayüz

Personel sayfasına **"Excel'den İçe Aktar"** düğmesi → modal:

1. Şablon indirme bağlantısı
2. Dosya seçimi → otomatik önizleme çağrısı
3. Rapor: sayım kartları (toplam / geçerli / hatalı / mükerrer), bilinmeyen
   departmanlar listesi, sorunlar tablosu (satır no · alan · mesaj)
4. **"Aktar"** düğmesi — geçerli satır 0 ise pasif

Aktarım sonrası personel listesi tazelenir ve sonuç toast'u gösterilir.

## Test stratejisi

**Birim (doğrulayıcı — Excel'siz):** zorunlu alan eksik, TC biçimi, IBAN biçimi,
bilinmeyen departman, dosya içi mükerrer TC, durum varsayılanı.

**Birim (ayrıştırıcı):** ClosedXML ile bellekte üretilen dosya; başlık eksikliği,
boş satır atlama, tarih hücresi okuma.

**Entegrasyon:** önizleme raporu doğru sayımları verir · mükerrer satır atlanır ve
mevcut kayıt DEĞİŞMEZ · bilinmeyen departman satır hatası üretir · aktarım sonrası
personel listesi büyür · hr-admin olmayan 403 alır · 1000 satır sınırı reddedilir.

**Frontend:** modal raporu gösterir; geçerli satır 0 iken "Aktar" pasiftir.

## Riskler

| Risk | Azaltma |
| --- | --- |
| Büyük dosya belleği şişirir | 5 MB + 1000 satır sınırı, sınır aşımında erken red |
| Kullanıcı yanlış şablon kullanır | Şablon uçtan indirilir; başlık doğrulaması net hata verir |
| Aynı dosya iki kez yüklenir | TC ile mükerrer tespiti, atlama ve raporlama |
| TC boş satırlar mükerrer kontrolünden kaçar | Önizleme bunu açıkça uyarır |
