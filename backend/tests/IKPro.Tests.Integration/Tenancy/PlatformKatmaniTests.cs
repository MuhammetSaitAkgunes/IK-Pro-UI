using FluentAssertions;
using IKPro.Application.Common.Interfaces;
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
    public async Task PlatformVeritabani_AyagaKalkarVeSorgulanabilir()
    {
        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();

        // Migration uygulanmışsa sorgu çalışır; uygulanmamışsa SqlException atar.
        var kiraciSayisi = await platform.Tenants.CountAsync();

        kiraciSayisi.Should().BeGreaterThanOrEqualTo(0);
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
