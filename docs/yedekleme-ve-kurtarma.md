# Yedekleme ve Kurtarma Runbook'u

> **Neden bu belge en kritik olanı:** İK Pro'daki diğer her hata geri alınabilir.
> Veri kaybı alınamaz. Bir müşterinin özlük dosyaları, bordro geçmişi ve izin
> kayıtları kaybolursa hizmet devam edemez ve KVKK açısından da ihlal doğar.

## Temel ilke

**Yedeğin var olması yeterli değildir; geri yüklenebildiği kanıtlanmalıdır.**

Test edilmemiş yedek, olmayan yedektir. Sektörde en sık görülen felaket, yedeğin
alınmamış olması değil, alınan yedeğin bozuk çıkması ya da geri yükleme
prosedürünün ilk kez felaket anında denenmesidir. Bu yüzden aşağıdaki tatbikat
zorunludur.

## Hedefler (üretim için belirlenmeli)

Bu iki sayı ticari taahhüdünüzü belirler; müşteri sözleşmesine yazılmadan önce
kararlaştırılmalıdır.

| Hedef | Anlamı | Öneri |
| --- | --- | --- |
| **RPO** (Recovery Point Objective) | En fazla ne kadarlık veri kaybını göze alıyorsunuz? | ≤ 1 saat → saatlik log yedeği gerekir |
| **RTO** (Recovery Time Objective) | Hizmet en geç ne kadar sürede geri gelmeli? | ≤ 4 saat |

> ⬜ **Karar bekliyor:** RPO ve RTO değerleri henüz belirlenmedi. Bunlar
> belirlenmeden yedek sıklığı ve saklama süresi netleşemez.

## Yedek planı

| Tür | Sıklık | Saklama | Not |
| --- | --- | --- | --- |
| Tam (FULL) | Günlük | 30 gün | Gece düşük trafikte |
| Fark (DIFFERENTIAL) | 6 saatte bir | 7 gün | Geri yükleme süresini kısaltır |
| İşlem günlüğü (LOG) | RPO'ya göre (ör. saatlik) | 7 gün | Yalnız FULL recovery model'de |

**Kritik kural:** Yedekler veritabanı sunucusuyla **aynı diskte tutulmaz.**
Üretimde ayrı fiziksel konum (ayrı sunucu / nesne depolama) zorunludur. Aynı
diskteki yedek, disk arızasında veriyle birlikte gider.

## Tatbikat

`scripts/backup-restore-drill.ps1` yedek alır, **ayrı bir veritabanı adına**
(`<ad>_RestoreDrill`) geri yükler, tablo satır sayılarını kaynakla karşılaştırır
ve kopyayı düşürür.

```powershell
pwsh scripts/backup-restore-drill.ps1 -Database IKProDb -BackupPath C:\yedek
```

> **Tuzak — yedek klasörü izni:** `BackupPath`, sizin değil **SQL Server servis
> hesabının** yazabildiği bir klasör olmalıdır. Kullanıcı `%TEMP%` klasörü
> genelde yazılamaz ve şu hatayı verir:
> `Cannot open backup device ... Operating system error 5 (Erişim engellendi)`.
>
> Güvenli varsayılanı sunucudan sorun:
> ```powershell
> sqlcmd -S localhost -E -h -1 -W -Q "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(400));"
> ```

- Çıkış kodu **0** → tatbikat başarılı.
- Çıkış kodu **1** → yedek, geri yükleme veya doğrulama adımlarından biri
  başarısız. **Bu bir olaydır**, sıradaki tatbikata ertelenmez.
- `-KeepRestoredCopy` verilirse tatbikat kopyası elle inceleme için bırakılır.

**Güvenlik:** Script geri yüklemeyi asla kaynak veritabanının üzerine yapmaz;
hedef ad kaynakla aynı olursa çalışmayı reddeder. Mevcut veri hiçbir koşulda
üzerine yazılmaz.

**Sıklık:** En az **3 ayda bir**, ayrıca her şema göçünden (migration) sonra.

**Kayıt:** Her tatbikat aşağıdaki tabloya işlenir. Tarih yoksa tatbikat
yapılmamış sayılır.

| Tarih | Yapan | Sonuç | Süre | Not |
| --- | --- | --- | --- | --- |
| 2026-08-05 | geliştirme | ⬜ ilk koşu | | Script devreye alındı |

## Felaket anı — sıra

1. **Durdur:** Uygulamayı kapat; bozuk durum üzerine yazmayı engelle.
2. **Teşhis et:** Veri kaybı mı, bozulma mı, silme mi? Hangi ana kadar sağlam?
3. **Haber ver:** Etkilenen müşterilere bilgi ver (KVKK açısından da gerekebilir).
4. **Geri yükle:** Son sağlam FULL + sonrasındaki DIFF + LOG zinciri.
5. **Doğrula:** Satır sayıları, son işlem tarihleri, kritik ekranlar açılıyor mu.
6. **Aç:** Uygulamayı devreye al.
7. **Yaz:** Olay sonrası not — kök neden ve tekrarını önleyecek adım.

## KVKK notu

**Yedekler de kişisel veri içerir.** Bu şu sonuçları doğurur:

- Yedekler şifrelenmiş olmalı ve erişim yetkisi sınırlı tutulmalıdır.
- Saklama süresi sonunda **gerçekten imha** edilmelidir.
- Bir kullanıcı silme talebinde bulunduğunda (unutulma hakkı), yedeklerdeki
  kopyanın saklama süresi dolunca kendiliğinden düşeceği kayıt altına alınmalı;
  yedekten seçici silme genelde mümkün değildir.
- Kiracı purge işlemi (`TenantPurger`) canlı veriyi siler; **yedeklerdeki kopya
  saklama süresi boyunca durmaya devam eder.** Aydınlatma metninde belirtilmeli.

## Sorumluluk

⬜ **Atama bekliyor:** Yedeklerin alındığını kimin, hangi sıklıkla kontrol
edeceği ve tatbikatı kimin koşacağı belirlenmelidir. Sahibi olmayan yedek
politikası uygulanmaz.

## Tam kurulum (dört bileşen birlikte)

```powershell
pwsh scripts/backup-restore-drill.ps1 `
  -Database IKProDb `
  -BackupPath "C:\SQLYedek" `
  -StoragePath "C:\IKPro\App_Data\storage" `
  -OffsitePath "\\yedek-sunucu\ikpro" `
  -LogPath "C:\SQLYedek\tatbikat.jsonl" `
  -AlertWebhookUrl "https://hooks.slack.com/services/..."
```

| Bileşen | Parametre | Ne sağlar |
| --- | --- | --- |
| Evrak dosyaları | `-StoragePath` | Özlük evrakları/fotoğraflar zip'lenip yedeğe eklenir. **Veritabanı tek başına yetmez:** DB yalnız dosya yollarını tutar, dosyalar diskte durur. |
| Off-site kopya | `-OffsitePath` | Yedek ikinci konuma kopyalanır ve boyut olarak doğrulanır. Aynı diskteki yedek, disk arızasında veriyle birlikte gider. |
| Denetim izi | `-LogPath` | Her koşum JSON satırı olarak eklenir (başarı **ve** başarısızlık). |
| Uyarı | `-AlertWebhookUrl` | Başarısızlıkta POST edilir (Slack/Teams uyumlu). Sessiz başarısızlık en tehlikeli durumdur. |

## Otomatik zamanlama

```powershell
# 1) Önce ne yapacağını gör (sistemi değiştirmez):
pwsh scripts/register-backup-task.ps1 -Database IKProDb -BackupPath "C:\SQLYedek" -WhatIf

# 2) YÖNETİCİ PowerShell'de kaydet:
pwsh scripts/register-backup-task.ps1 -Database IKProDb -BackupPath "C:\SQLYedek" `
  -StoragePath "C:\IKPro\App_Data\storage" -OffsitePath "\\yedek-sunucu\ikpro" `
  -LogPath "C:\SQLYedek\tatbikat.jsonl"

# 3) Hemen tetikleyip doğrula:
Start-ScheduledTask -TaskName IKPro-YedekTatbikati
Get-ScheduledTaskInfo -TaskName IKPro-YedekTatbikati | Select LastRunTime, LastTaskResult
```

`LastTaskResult` **0** = başarılı. Görev SYSTEM hesabıyla kaydedilir, çünkü SQL
Server yedek dizinine yazma izni kullanıcı hesabında genelde yoktur.

Kaldırmak için: `Unregister-ScheduledTask -TaskName IKPro-YedekTatbikati -Confirm:$false`

> **Not:** Zamanlanmış görev bu depoda **kaydedilmedi** — sistemde kalıcı
> yapılandırma oluşturduğu ve yönetici hakkı gerektirdiği için bilinçli olarak
> operatöre bırakıldı. Yukarıdaki komut çalıştırılana kadar yedekleme ELLE
> çalışır durumdadır.

## Kalan eksikler

- ⬜ **RPO/RTO belirlenmedi** — ticari karar; sözleşmeye yazılmadan önce netleşmeli.
- ⬜ **Zamanlanmış görev kurulmadı** — yukarıdaki komut operatör tarafından
  çalıştırılmalı.
- ⬜ **Off-site hedefi seçilmedi** — `-OffsitePath` için gerçek bir ağ paylaşımı
  veya nesne depolama belirlenmeli; SYSTEM hesabının o hedefe erişebildiği
  doğrulanmalı.
- ⬜ **Log dosyası kimse tarafından izlenmiyor** — webhook kurulana kadar
  `tatbikat.jsonl` dosyasına düzenli bakılmalı.
