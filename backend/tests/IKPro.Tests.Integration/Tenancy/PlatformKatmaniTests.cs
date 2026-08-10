using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Platform katmanı: kiracı kimliği uygulama veritabanından ayrı, kendi
/// veritabanında tutulur. Bu fazda kiracı VERİSİ hâlâ tek uygulama
/// veritabanındadır — ayrılan yalnız kiracının kendisidir.
/// </summary>
[Collection(ApiCollection.Name)]
public class PlatformKatmaniTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task PlatformVeritabani_KiraciYazilipOkunabilirVeAlanlariKorunur()
    {
        var slug = $"platform-test-{Guid.NewGuid():N}";

        // Yazma: bir scope'ta ekle ve kaydet.
        using (var yazmaScope = Factory.Services.CreateScope())
        {
            var yazma = yazmaScope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            yazma.Tenants.Add(new Tenant
            {
                Name = "Platform Test A.Ş.",
                Slug = slug,
                Status = TenantStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await yazma.SaveChangesAsync(CancellationToken.None);
        }

        // Okuma: TAMAMEN AYRI bir scope/context — change tracker'dan değil,
        // gerçekten veritabanından okunduğunu garanti eder.
        using var okumaScope = Factory.Services.CreateScope();
        var okuma = okumaScope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        var kiraci = await okuma.Tenants.SingleAsync(t => t.Slug == slug);

        kiraci.Name.Should().Be("Platform Test A.Ş.");
        kiraci.Status.Should().Be(TenantStatus.Active);

        // Temizlik — test veritabanını sonraki testler için kirletmeden bırak.
        okuma.Tenants.Remove(kiraci);
        await okuma.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public Task PlatformVeritabani_UygulamaVeritabanindanFarklidir()
    {
        // Awaitlenecek bir şey yok (senkron bağlantı kontrolü) — async işaretlemek
        // CS1998 uyarısı doğurur ve derleme uyarısız olmalı (CI -warnaserror).
        using var scope = Factory.Services.CreateScope();
        var platform = (DbContext)scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        var uygulama = (DbContext)scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        platform.Database.GetDbConnection().Database
            .Should().NotBe(uygulama.Database.GetDbConnection().Database,
                "platform kimliği uygulama verisinden ayrı bir veritabanında durmalı");

        return Task.CompletedTask;
    }

    [Fact]
    public Task KiraciSatiri_UygulamaVeritabaninda_ARTIK_YOK()
    {
        // Awaitlenecek bir şey yok (senkron model kontrolü) — async işaretlemek
        // CS1998 uyarısı doğurur ve derleme uyarısız olmalı (CI -warnaserror).
        using var scope = Factory.Services.CreateScope();
        var uygulama = (DbContext)scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var tenantsTablosuVar = uygulama.Model.GetEntityTypes()
            .Any(e => e.GetTableName() == "Tenants");

        tenantsTablosuVar.Should().BeFalse(
            "kiracı kimliği platform veritabanına taşındı; uygulama modelinde kalması iki doğruluk kaynağı demektir");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProvizyonlananKiraci_PlatformVeritabaninda_Gorunur()
    {
        var kiraci = await ProvisionAndActivateAsync("PlatformKiraci", $"pk-{Guid.NewGuid():N}@ornek.local");

        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();

        var kayit = await platform.Tenants.FirstOrDefaultAsync(t => t.Id == kiraci.TenantId);

        kayit.Should().NotBeNull();
        kayit!.Slug.Should().Be(kiraci.Slug);
    }

    [Fact]
    public async Task DondurulmusKiraci_GirisYapamaz()
    {
        var eposta = $"donduk-{Guid.NewGuid():N}@ornek.local";
        var kiraci = await ProvisionAndActivateAsync("Donduk", eposta);

        // Giriş önce çalışıyor olmalı — testin anlamlı olması için.
        _ = await AuthedClientAsync(eposta);

        await DurumuDegistirAsync(kiraci.TenantId, TenantStatus.Frozen);

        var anonim = Factory.CreateClient();
        var yanit = await anonim.PostAsJsonAsync("/api/auth/login", new { email = eposta, password = DefaultPassword });

        yanit.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "dondurulmuş kiracıya giriş yapılamamalı");
    }

    private async Task DurumuDegistirAsync(int tenantId, TenantStatus durum)
    {
        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        var kiraci = await platform.Tenants.FirstAsync(t => t.Id == tenantId);
        kiraci.Status = durum;
        await platform.SaveChangesAsync(default);
    }

    [Fact]
    public async Task KullaniciOlusturulunca_DizineYazilir()
    {
        var eposta = $"dizin-{Guid.NewGuid():N}@ornek.local";
        var kiraci = await ProvisionAndActivateAsync("Dizin", eposta);

        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();

        var kayit = await platform.Directory
            .FirstOrDefaultAsync(d => d.NormalizedEmail == TenantDirectoryEntry.Normalize(eposta));

        kayit.Should().NotBeNull("admin oluşturulduğunda dizine yazılmalı");
        kayit!.TenantId.Should().Be(kiraci.TenantId);
    }

    [Fact]
    public async Task AyniEposta_IkinciKiracida_OnKontrolReddeder()
    {
        // Bu test yalnız kullanıcının gördüğü sonucu doğrular: ikinci kayıt denemesi
        // 409 alır. YOL ise burada Identity'nin FindByEmailAsync ön kontrolüdür
        // (TenantOnboarding.EmailExistsAsync) — dizinin birincil anahtar kısıtı hiç
        // devreye girmez, çünkü ilk admin zaten Identity'de bulunur ve provizyon
        // kiracı/kullanıcı oluşturmadan ÖNCE erken döner. Kısıtın kendisi ayrı testte
        // (AyniEposta_DizindeBaskaKiraciyaAitse_RezervasyondaReddedilir) sınanır.
        var eposta = $"tekil-{Guid.NewGuid():N}@ornek.local";
        await ProvisionAndActivateAsync("Birinci", eposta);

        var yanit = await ProvisionRawAsync(new
        {
            companyName = "Ikinci",
            slug = $"ikinci{Guid.NewGuid():N}"[..20],
            adminName = "Ikinci Yonetici",
            adminEmail = eposta,
        });

        yanit.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "tek e-posta = tek kiracı kuralı en azından ön kontrolle korunmalı");
    }

    [Fact]
    public async Task AyniEposta_DizindeBaskaKiraciyaAitse_RezervasyondaReddedilir()
    {
        // Yarış koşulunun simülasyonu: dizinde Identity'de karşılığı OLMAYAN bir satır
        // elle oluşturulur. Böylece EmailExistsAsync (Identity sorgusu) false döner ve
        // ön kontrolü geçer — ama ReserveEmailAsync'in yazdığı DizineYazAsync, dizinde
        // BAŞKA bir kiracıya ait aynı e-postayı bulur ve ConflictException fırlatır.
        // Bu, veritabanı seviyesindeki "tek e-posta = tek kiracı" kısıtını gerçekten
        // sınayan yoldur (ön kontrol devre dışı kalınca ne olduğu).
        var eposta = $"yaris-{Guid.NewGuid():N}@ornek.local";

        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            platform.Directory.Add(new TenantDirectoryEntry
            {
                NormalizedEmail = TenantDirectoryEntry.Normalize(eposta),
                TenantId = -1, // gerçek bir kiracıya ait değil; yalnız PK çakışmasını tetikler.
            });
            await platform.SaveChangesAsync(CancellationToken.None);
        }

        var yanit = await ProvisionRawAsync(new
        {
            companyName = "Yarisan",
            slug = $"yarisan{Guid.NewGuid():N}"[..20],
            adminName = "Yarisan Yonetici",
            adminEmail = eposta,
        });

        yanit.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "e-posta dizinde başka bir kiracıya ait olduğunda rezervasyon veritabanı kısıtına çarpmalı");
    }
}
