using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Tenancy.Commands;
using IKPro.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Multi-tenant testleri için ortak yardımcılar: kiracı provizyonu, kiracı-kapsamlı
/// doğrudan tohumlama ve authed istemci. Provizyonlanan admin geçici şifre "demo123" alır.
/// </summary>
public abstract class TenancyTestBase(IKProApiFactory factory)
{
    protected IKProApiFactory Factory { get; } = factory;

    /// <summary>Platform anahtarıyla yeni kiracı + ilk hr-admin provizyonlar.</summary>
    protected async Task<ProvisionTenantResult> ProvisionTenantAsync(string companyName, string adminEmail)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        var response = await client.PostAsJsonAsync("/api/tenants", new
        {
            companyName,
            slug = $"{Slugify(companyName)}-{Guid.NewGuid():N}",
            adminName = $"{companyName} Yöneticisi",
            adminEmail,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"provizyon başarısız: {companyName}");
        return (await response.Content.ReadFromJsonAsync<ProvisionTenantResult>())!;
    }

    /// <summary>Ham provizyon isteği (hata senaryoları için).</summary>
    protected Task<HttpResponseMessage> ProvisionRawAsync(object body, bool withPlatformKey = true)
    {
        var client = Factory.CreateClient();
        if (withPlatformKey)
        {
            client.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        }
        return client.PostAsJsonAsync("/api/tenants", body);
    }

    /// <summary>Verili kiracıyı impersone edip DB'ye doğrudan veri yazar (çapraz-kiracı kurulum).</summary>
    protected async Task SeedInTenantAsync(int tenantId, Func<AppDbContext, Task> seed)
    {
        using var scope = Factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<ICurrentTenant>().Impersonate(tenantId);
        var db = sp.GetRequiredService<AppDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }

    protected async Task<HttpClient> AuthedClientAsync(string email, string password = "demo123")
    {
        var anonymous = Factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"giriş başarısız: {email}");
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    protected static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {url}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    // Slug yalnız ASCII [a-z0-9] olmalı (validator kuralı) — Türkçe/aksanlı harfler atılır.
    private static string Slugify(string name) =>
        new string(name.ToLowerInvariant()
            .Where(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            .ToArray());
}
