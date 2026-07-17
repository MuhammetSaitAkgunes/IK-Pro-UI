using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Departments;
using IKPro.Domain.Constants;
using IKPro.Domain.Entities.Organization;
using IKPro.Domain.Entities.Tenancy;
using IKPro.Infrastructure.Identity;
using IKPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Faz 0 kanıtı: multi-tenant izolasyon. İkinci bir kiracı (Globex) provizyonlanır;
/// bir kiracının kullanıcısı DİĞER kiracının verisini asla göremez (çift yönlü).
/// İzolasyon EF Core global query filter + JWT tenant claim ile sağlanır.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantIsolationTests(IKProApiFactory factory)
{
    [Fact]
    public async Task Departments_AreIsolatedBetweenTenants()
    {
        // Globex kiracısı + hr-admin + ona ait bir departman doğrudan kurulur
        // (provizyon ucu Faz 1; burada seed yardımcısı yeterli).
        const string globexDept = "Globex Yazılım Ekibi";
        var globexAdminEmail = $"admin-{Guid.NewGuid():N}@globex.local";
        await ProvisionTenantAsync("Globex Holding", $"globex-{Guid.NewGuid():N}", globexAdminEmail, globexDept);

        // Globex admin yalnız Globex'in departmanını görür — varsayılan kiracınınkini DEĞİL.
        var globex = await AuthedClientAsync(globexAdminEmail);
        var globexDepts = await GetAsync<List<DepartmentDto>>(globex, "/api/departments");
        globexDepts.Should().ContainSingle()
            .Which.Name.Should().Be(globexDept, "Globex admin yalnız kendi kiracısını görmeli");

        // Varsayılan kiracının admin'i Globex'in departmanını GÖREMEZ.
        var demoAdmin = await AuthedClientAsync("ik@hrmaster.local");
        var demoDepts = await GetAsync<List<DepartmentDto>>(demoAdmin, "/api/departments");
        demoDepts.Should().NotBeEmpty("varsayılan kiracının seed departmanları görünmeli");
        demoDepts.Should().NotContain(d => d.Name == globexDept,
            "varsayılan kiracı Globex verisini asla görmemeli");
    }

    // --- yardımcılar ---

    private async Task ProvisionTenantAsync(string name, string slug, string adminEmail, string departmentName)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var tenantContext = sp.GetRequiredService<ICurrentTenant>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Kiracı, global filtreye tabi değil → doğrudan eklenir.
        var tenant = new Tenant { Name = name, Slug = slug, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // Bundan sonra bu scope'taki tüm yazımlar Globex'e damgalanır ve filtrelenir.
        tenantContext.Impersonate(tenant.Id);

        db.Departments.Add(new Department { Name = departmentName });
        await db.SaveChangesAsync();

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Globex Yöneticisi",
            Initials = "GY",
            TenantId = tenant.Id,
        };
        (await userManager.CreateAsync(admin, "demo123")).Succeeded
            .Should().BeTrue("Globex admin kullanıcısı oluşturulmalı");
        await userManager.AddToRoleAsync(admin, Roles.HrAdmin);
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
