using FluentAssertions;
using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Compliance;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Faz 2 kanıtı: SQL view read-model'leri de kiracıya izole. Kritik vaka
/// vw_ComplianceReadiness'tir — eskiden TÜM kiracıların belgelerini tek satırda
/// topluyordu (sessiz çapraz-kiracı sızıntısı); artık TenantId'ye göre gruplanır.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantViewIsolationTests(IKProApiFactory factory)
{
    [Fact]
    public async Task ComplianceReadinessView_IsIsolatedPerTenant()
    {
        // Yeni (boş) bir kiracı: hiç uyum belgesi yok.
        var adminEmail = $"admin-{Guid.NewGuid():N}@readiness.local";
        var provision = await ProvisionAsync(new
        {
            companyName = "Readiness A.Ş.",
            slug = $"readiness-{Guid.NewGuid():N}",
            adminName = "Readiness Admin",
            adminEmail,
        });
        provision.StatusCode.Should().Be(HttpStatusCode.Created);

        // Yeni kiracının hazırlık özeti YALNIZ kendi (sıfır) belgesini yansıtmalı.
        // View izole değilse, varsayılan kiracının seed belgelerinin toplamı sızardı.
        var newAdmin = await AuthedClientAsync(adminEmail);
        var newReadiness = await GetAsync<ComplianceReadinessDto>(newAdmin, "/api/compliance/readiness");
        newReadiness.TotalCount.Should().Be(0, "yeni kiracının hiç uyum belgesi yok → view sızdırmamalı");

        // Varsayılan kiracının seed belgeleri hâlâ görünüyor (izolasyon çift yönlü).
        var demoAdmin = await AuthedClientAsync("ik@hrmaster.local");
        var demoReadiness = await GetAsync<ComplianceReadinessDto>(demoAdmin, "/api/compliance/readiness");
        demoReadiness.TotalCount.Should().BeGreaterThan(0,
            "varsayılan kiracının seed uyum belgeleri kendi hazırlık özetinde görünmeli");
    }

    // --- yardımcılar ---

    private Task<HttpResponseMessage> ProvisionAsync(object body)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        return client.PostAsJsonAsync("/api/tenants", body);
    }

    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "demo123" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"giriş başarısız: {email}");
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {url}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
