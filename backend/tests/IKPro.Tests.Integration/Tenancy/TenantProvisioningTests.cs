using FluentAssertions;
using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Departments;
using IKPro.Application.Features.Tenancy.Commands;
using IKPro.Domain.Entities.Tenancy;
using IKPro.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Faz 1: kiracı provizyonu (şirket + ilk hr-admin) ve kiracı-farkında login.
/// Provizyon platform anahtarıyla korunur; provizyonlanan admin yalnız kendi
/// kiracısını görür; pasif kiracıya giriş reddedilir.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantProvisioningTests(IKProApiFactory factory)
{
    [Fact]
    public async Task Provision_CreatesTenantAndAdmin_WhoSeesOnlyOwnTenant()
    {
        var slug = $"acme-{Guid.NewGuid():N}";
        var adminEmail = $"admin-{Guid.NewGuid():N}@acme.local";

        var result = await ProvisionAsync(new
        {
            companyName = "Acme A.Ş.",
            slug,
            adminName = "Acme Yöneticisi",
            adminEmail,
        });
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        // Provizyonlanan admin geçici şifreyle giriş yapabilir.
        var admin = await AuthedClientAsync(adminEmail);

        // Yeni kiracının hiç departmanı yok → boş; varsayılan kiracının verisini GÖRMEZ.
        var depts = await admin.GetAsync("/api/departments");
        depts.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = (await depts.Content.ReadFromJsonAsync<List<DepartmentDto>>())!;
        list.Should().BeEmpty("yeni kiracıda henüz veri yok ve başka kiracının verisi görünmemeli");
    }

    [Fact]
    public async Task Provision_WithoutPlatformKey_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/tenants", new
        {
            companyName = "Yetkisiz A.Ş.",
            slug = $"yetkisiz-{Guid.NewGuid():N}",
            adminName = "X",
            adminEmail = $"x-{Guid.NewGuid():N}@x.local",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Provision_DuplicateSlug_Returns409()
    {
        var slug = $"dup-{Guid.NewGuid():N}";
        (await ProvisionAsync(new
        {
            companyName = "İlk A.Ş.", slug,
            adminName = "İlk", adminEmail = $"ilk-{Guid.NewGuid():N}@dup.local",
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        (await ProvisionAsync(new
        {
            companyName = "İkinci A.Ş.", slug,
            adminName = "İkinci", adminEmail = $"ikinci-{Guid.NewGuid():N}@dup.local",
        })).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_InactiveTenant_Rejected()
    {
        var slug = $"pasif-{Guid.NewGuid():N}";
        var adminEmail = $"admin-{Guid.NewGuid():N}@pasif.local";
        var result = await ProvisionAsync(new
        {
            companyName = "Pasif A.Ş.", slug, adminName = "Pasif Admin", adminEmail,
        });
        var body = (await result.Content.ReadFromJsonAsync<ProvisionTenantResult>())!;

        // Kiracıyı doğrudan pasife al.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenant = await db.Tenants.FindAsync(body.TenantId);
            tenant!.IsActive = false;
            await db.SaveChangesAsync();
        }

        var anonymous = factory.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/auth/login",
            new { email = adminEmail, password = "demo123" });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "pasif kiracıya giriş reddedilmeli");
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
}
