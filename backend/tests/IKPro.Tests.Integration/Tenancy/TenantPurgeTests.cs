using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Organization;
using IKPro.Infrastructure.Identity;
using IKPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Kiracı purge: bir kiracının tüm verisini siler, başka kiracıya dokunmaz; confirm-slug
/// ve platform-key güvenliği; doğrulanmamış kiracı temizliği.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantPurgeTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task Purge_RemovesTargetTenantData_LeavesOthersIntact()
    {
        var a = await ProvisionAndActivateAsync("Silinecek A.Ş.", $"a-{Guid.NewGuid():N}@purge.local");
        var b = await ProvisionAndActivateAsync("Kalacak A.Ş.", $"b-{Guid.NewGuid():N}@purge.local");
        await SeedInTenantAsync(a.TenantId, db => { db.Departments.Add(new Department { Name = "A-Dept" }); return Task.CompletedTask; });
        await SeedInTenantAsync(b.TenantId, db => { db.Departments.Add(new Department { Name = "B-Dept" }); return Task.CompletedTask; });

        using (var scope = Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ITenantPurger>()
                .PurgeAsync(a.TenantId, CancellationToken.None);
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<ICurrentTenant>().Impersonate(b.TenantId);
            var db = sp.GetRequiredService<AppDbContext>();

            (await db.Tenants.AnyAsync(t => t.Id == a.TenantId)).Should().BeFalse("kiracı satırı silinmeli");
            (await db.Set<ApplicationUser>().AnyAsync(u => u.TenantId == a.TenantId))
                .Should().BeFalse("kiracının kullanıcıları silinmeli");
            (await db.Departments.IgnoreQueryFilters().AnyAsync(d => d.TenantId == a.TenantId))
                .Should().BeFalse("kiracının verisi silinmeli");

            (await db.Departments.AnyAsync(d => d.Name == "B-Dept")).Should().BeTrue("başka kiracı korunmalı");
            (await db.Tenants.AnyAsync(t => t.Id == b.TenantId)).Should().BeTrue();
        }
    }

    private HttpClient PlatformClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        return client;
    }

    [Fact]
    public async Task Delete_WithWrongSlug_Returns409_AndKeepsData()
    {
        var t = await ProvisionAndActivateAsync("Yanlış Onay A.Ş.", $"w-{Guid.NewGuid():N}@purge.local");

        var resp = await PlatformClient().DeleteAsync($"/api/tenants/{t.TenantId}?confirmSlug=yanlis-slug");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Tenants.AnyAsync(x => x.Id == t.TenantId)).Should().BeTrue("yanlış slug'da silinmemeli");
    }

    [Fact]
    public async Task Delete_WithoutPlatformKey_Returns401()
    {
        var t = await ProvisionAndActivateAsync("Anahtarsız A.Ş.", $"k-{Guid.NewGuid():N}@purge.local");
        var resp = await Factory.CreateClient().DeleteAsync($"/api/tenants/{t.TenantId}?confirmSlug={t.Slug}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithPlatformKeyAndSlug_PurgesTenant()
    {
        var t = await ProvisionAndActivateAsync("Doğru Onay A.Ş.", $"ok-{Guid.NewGuid():N}@purge.local");
        var resp = await PlatformClient().DeleteAsync($"/api/tenants/{t.TenantId}?confirmSlug={t.Slug}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Tenants.AnyAsync(x => x.Id == t.TenantId)).Should().BeFalse("silinmiş olmalı");
    }

    [Fact]
    public async Task CleanupUnverified_PurgesUnverified_KeepsVerifiedAndActive()
    {
        // Doğrulanmamış (davet kabul edilmemiş) self-servis kiracı.
        var email = $"u-{Guid.NewGuid():N}@cleanup.local";
        (await Factory.CreateClient().PostAsJsonAsync("/api/tenants/signup",
            new { companyName = "Doğrulanmamış A.Ş.", adminName = "X", adminEmail = email }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // CreatedAtUtc'yi geçmişe çek (eski olsun).
        int unverifiedId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var t = await db.Tenants.FirstAsync(x => x.Name == "Doğrulanmamış A.Ş.");
            unverifiedId = t.Id;
            t.CreatedAtUtc = DateTime.UtcNow.AddDays(-30);
            await db.SaveChangesAsync();
        }

        // Doğrulanmış aktif kiracı (korunmalı).
        var kept = await ProvisionAndActivateAsync("Aktif A.Ş.", $"a-{Guid.NewGuid():N}@cleanup.local");

        var resp = await PlatformClient().PostAsync("/api/tenants/cleanup-unverified?olderThanDays=7", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Tenants.AnyAsync(x => x.Id == unverifiedId)).Should().BeFalse("doğrulanmamış temizlenmeli");
            (await db.Tenants.AnyAsync(x => x.Id == kept.TenantId)).Should().BeTrue("aktif kiracı korunmalı");
        }
    }
}
