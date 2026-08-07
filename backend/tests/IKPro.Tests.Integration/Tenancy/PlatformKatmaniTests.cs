using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
                IsActive = true,
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
        kiraci.IsActive.Should().BeTrue();

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
}
