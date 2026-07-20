using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Organization;
using IKPro.Infrastructure.Identity;
using IKPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
}
