using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Constants;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Erişim kapısı: dondurulmuş bir kiracının HİÇBİR yolu çalışmamalı.
///
/// Faz 1a'da kapı yalnız login'deydi; elinde geçerli token olan kullanıcı
/// çalışmaya, elinde refresh token olan da oturumunu uzatmaya devam
/// edebiliyordu. Bu testler üç yolun üçünü de kapatır.
/// </summary>
[Collection(ApiCollection.Name)]
public class ErisimKapisiTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    /// <summary>
    /// Login 403 döner, 401 değil: kimlik bilgileri DOĞRU, engelleyen şey
    /// kiracının kapalı olması. 401 "parolan yanlış" anlamına gelir ve
    /// kullanıcıyı yanlış yönlendirirdi.
    /// </summary>
    [Fact]
    public async Task DondurulmusKiraci_LoginYapamaz()
    {
        var (eposta, tenantId) = await AktifKiraciAsync("KapiLogin");
        await DurumDegistirAsync(tenantId, TenantStatus.Frozen);

        var yanit = await Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email = eposta, password = DefaultPassword });

        yanit.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DondurulmusKiraci_ElindekiTokenlaIstekYapamaz()
    {
        var (eposta, tenantId) = await AktifKiraciAsync("KapiIstek");
        var client = await AuthedClientAsync(eposta);

        // Token dondurmadan ÖNCE alındı ve hâlâ geçerli.
        (await client.GetAsync("/api/departments")).StatusCode.Should().Be(HttpStatusCode.OK);

        await DurumDegistirAsync(tenantId, TenantStatus.Frozen);

        var yanit = await client.GetAsync("/api/departments");
        yanit.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "geçerli token, dondurulmuş kiracıya erişim hakkı vermemeli");
    }

    [Fact]
    public async Task DondurulmusKiraci_RefreshIleOturumUzatamaz()
    {
        var (eposta, tenantId) = await AktifKiraciAsync("KapiRefresh");

        var girisYaniti = await Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email = eposta, password = DefaultPassword });
        var auth = (await girisYaniti.Content.ReadFromJsonAsync<Application.Features.Auth.AuthResponse>())!;

        await DurumDegistirAsync(tenantId, TenantStatus.Frozen);

        var yanit = await Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.RefreshToken });

        yanit.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "dondurulmuş kiracının refresh token'ı yeni access token üretmemeli");
    }

    [Fact]
    public async Task AktifKiraci_UcYoldaDaCalisir()
    {
        var (eposta, _) = await AktifKiraciAsync("KapiAktif");
        var client = await AuthedClientAsync(eposta);

        (await client.GetAsync("/api/departments")).StatusCode.Should().Be(HttpStatusCode.OK,
            "kapı, aktif kiracıyı engellememelidir");
    }

    /// <summary>
    /// Düzeltme turu 1, IMPORTANT #1: kapı isteğin token'ına değil, UCUN kendi
    /// yetkilendirme gereksinimine bağlı olmalı. Dondurulmuş bir kiracıdan kalma,
    /// süresi henüz dolmamış bir Bearer token'ı taşıyan bir istemci (Postman,
    /// mobil, entegrasyon) <c>POST /api/auth/login</c> ile BAŞKA ve AKTİF bir
    /// kiracının hesabına girmeye çalışırsa, o eski token'ın kiracı bağlamı bu
    /// isteğin amacıyla ilgisizdir — engellenmemeli. Bu senaryo
    /// <see cref="DondurulmusKiraci_LoginYapamaz"/>'dan farklıdır: orada login
    /// isteği HEADER'SIZ (anonim istemci), burada dondurulmuş kiracıdan kalma bir
    /// Authorization header'ı istekle birlikte taşınıyor.
    /// </summary>
    [Fact]
    public async Task DondurulmusKiraciTokeniTasiyanIstemci_BaskaAktifKiraciyaLoginYapabilir()
    {
        var (dondurulanEposta, dondurulanTenantId) = await AktifKiraciAsync("KapiEskiToken");
        var dondurulanClient = await AuthedClientAsync(dondurulanEposta);
        await DurumDegistirAsync(dondurulanTenantId, TenantStatus.Frozen);

        var (aktifEposta, _) = await AktifKiraciAsync("KapiHedefAktif");

        // dondurulanClient'ın Authorization header'ı hâlâ dondurulmuş kiracının
        // token'ını taşıyor — ama hedef, BAŞKA ve aktif bir kiracının hesabı.
        var yanit = await dondurulanClient.PostAsJsonAsync(
            "/api/auth/login", new { email = aktifEposta, password = DefaultPassword });

        yanit.StatusCode.Should().Be(HttpStatusCode.OK,
            "dondurulmuş bir kiracıdan kalma token, BAŞKA ve aktif bir kiracıya girişi engellememeli");
    }

    /// <summary>
    /// Düzeltme turu 1, IMPORTANT #2: RegisterAsync artık kapıdan geçmiyor.
    /// <c>POST /api/auth/register</c> her zaman anonimdir ve hedef kiracıyı
    /// <c>DefaultTenantIdAsync</c> (platformdaki EN DÜŞÜK Id'li kiracı) belirler —
    /// gerçek/paylaşılan ilk kiracıyı burada dondurmak diğer testleri bozardı, bu
    /// yüzden aynı kod yolunu (RegisterAsync → IssueTokensAsync) impersonation ile
    /// TAZE ve İZOLE bir dondurulmuş kiracıda doğrudan çağırıyoruz. Kapı hâlâ
    /// devrede olsaydı bu, TenantInaccessibleException fırlatırdı.
    /// </summary>
    [Fact]
    public async Task DondurulmusKiraciyaRegisterKapidanGecmezVeCalisir()
    {
        var (_, tenantId) = await AktifKiraciAsync("KapiRegisterHedef");
        await DurumDegistirAsync(tenantId, TenantStatus.Frozen);

        using var kapsam = Factory.Services.GetRequiredService<ITenantScopeFactory>().Create(tenantId);
        var identity = kapsam.Services.GetRequiredService<IIdentityService>();

        var email = $"kapiregister-{Guid.NewGuid():N}@ornek.local";
        var auth = await identity.RegisterAsync("Kapi Register", email, "kayit-sifre-1", Roles.Employee, default);

        auth.Token.Should().NotBeNullOrWhiteSpace(
            "register anonim self-servis akışıdır; kapı burada bilinçli olarak atlanır");
    }

    private async Task<(string Eposta, int TenantId)> AktifKiraciAsync(string ad)
    {
        var eposta = $"{ad.ToLowerInvariant()}-{Guid.NewGuid():N}@ornek.local";
        var kiraci = await ProvisionAndActivateAsync(ad, eposta);
        return (eposta, kiraci.TenantId);
    }

    private async Task DurumDegistirAsync(int tenantId, TenantStatus durum)
    {
        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        var satir = await platform.Tenants.FirstAsync(t => t.Id == tenantId);
        satir.Status = durum;
        await platform.SaveChangesAsync(default);
        scope.ServiceProvider.GetRequiredService<ITenantRegistry>().Invalidate(tenantId);
    }
}
