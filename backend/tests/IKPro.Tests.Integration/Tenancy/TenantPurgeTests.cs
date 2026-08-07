using FluentAssertions;
using IKPro.Infrastructure.Storage;
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
            var platform = sp.GetRequiredService<IPlatformDbContext>();

            (await platform.Tenants.AnyAsync(t => t.Id == a.TenantId)).Should().BeFalse("kiracı satırı silinmeli");
            (await db.Set<ApplicationUser>().AnyAsync(u => u.TenantId == a.TenantId))
                .Should().BeFalse("kiracının kullanıcıları silinmeli");
            (await db.Departments.IgnoreQueryFilters().AnyAsync(d => d.TenantId == a.TenantId))
                .Should().BeFalse("kiracının verisi silinmeli");

            (await db.Departments.AnyAsync(d => d.Name == "B-Dept")).Should().BeTrue("başka kiracı korunmalı");
            (await platform.Tenants.AnyAsync(t => t.Id == b.TenantId)).Should().BeTrue();
        }
    }

    private HttpClient PlatformClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        return client;
    }

    [Fact]
    public async Task Purge_DeletesPhysicalDocumentFiles()
    {
        var t = await ProvisionAndActivateAsync("Dosyalı A.Ş.", $"f-{Guid.NewGuid():N}@purge.local");

        // Depo artık kiracıya bağlı ve SCOPED; kök sağlayıcıdan çözülemez.
        // Kiracı bağlamı TenantPurger'ın yaptığı gibi impersonate ile kurulur.
        StoredFile stored;
        using (var yazmaScope = Factory.Services.CreateScope())
        {
            yazmaScope.ServiceProvider.GetRequiredService<ICurrentTenant>().Impersonate(t.TenantId);
            stored = await yazmaScope.ServiceProvider.GetRequiredService<IFileStorage>().SaveAsync(
                new MemoryStream(new byte[] { 1, 2, 3 }), "gizli.pdf", "docs", CancellationToken.None);
        }

        await SeedInTenantAsync(t.TenantId, async db =>
        {
            var dept = new Department { Name = "Dosya-Dept" };
            db.Departments.Add(dept);
            await db.SaveChangesAsync();

            var emp = new Employee
            {
                FirstName = "Ada", LastName = "Kayıt", Title = "Uzman",
                DepartmentId = dept.Id, HireDate = new DateOnly(2024, 1, 1),
            };
            db.Employees.Add(emp);
            await db.SaveChangesAsync();

            db.EmployeeDocuments.Add(new EmployeeDocument
            {
                EmployeeId = emp.Id, DocumentType = "Sözleşme",
                FileName = "gizli.pdf", FilePath = stored.Path, SizeBytes = 3,
            });
        });

        var kiraciKlasoru = Path.Combine(Factory.StorageRoot, LocalFileStorage.TenantFolder(t.TenantId));
        var evrakYolu = Path.Combine(kiraciKlasoru, stored.Path.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(evrakYolu).Should().BeTrue("dosya purge öncesi diskte olmalı");

        // Evrak DIŞINDAKİ türler de kiracı alanındadır ve purge'de gitmeli.
        // Eski davranış yalnız EmployeeDocuments yollarını siliyordu; foto ve logo kalıyordu.
        var fotoYolu = Path.Combine(kiraciKlasoru, "photos", "vesikalik.png");
        var logoYolu = Path.Combine(kiraciKlasoru, "branding", "logo.png");
        Directory.CreateDirectory(Path.GetDirectoryName(fotoYolu)!);
        Directory.CreateDirectory(Path.GetDirectoryName(logoYolu)!);
        await File.WriteAllTextAsync(fotoYolu, "foto");
        await File.WriteAllTextAsync(logoYolu, "logo");

        using (var scope = Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ITenantPurger>()
                .PurgeAsync(t.TenantId, CancellationToken.None);
        }

        File.Exists(evrakYolu).Should().BeFalse("purge fiziksel evrak dosyasını da silmeli (KVKK)");
        File.Exists(fotoYolu).Should().BeFalse("çalışan fotoğrafı da silinmeli");
        File.Exists(logoYolu).Should().BeFalse("şirket logosu da silinmeli");
        Directory.Exists(kiraciKlasoru).Should().BeFalse("kiracının tüm dosya alanı gitmeli");
    }

    [Fact]
    public async Task Purge_WithUndeletableFile_DoesNotThrow_AndStillPurges()
    {
        var t = await ProvisionAndActivateAsync("Bozuk Dosya A.Ş.", $"b-{Guid.NewGuid():N}@purge.local");

        // Kiracı alanında AÇIK TUTULAN bir dosya: Directory.Delete başarısız olur.
        // Dosyalar silinemese bile veri temizliği yarıda kesilmemeli; hata loglanır.
        var kiraciKlasoru = Path.Combine(Factory.StorageRoot, LocalFileStorage.TenantFolder(t.TenantId));
        Directory.CreateDirectory(kiraciKlasoru);
        await using var kilit = new FileStream(
            Path.Combine(kiraciKlasoru, "kilitli.bin"),
            FileMode.Create, FileAccess.Write, FileShare.None);

        var purge = async () =>
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ITenantPurger>()
                .PurgeAsync(t.TenantId, CancellationToken.None);
        };
        await purge.Should().NotThrowAsync("silinemeyen dosya alanı purge'ü yarıda kesmemeli");

        using var check = Factory.Services.CreateScope();
        var platform2 = check.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        (await platform2.Tenants.AnyAsync(x => x.Id == t.TenantId)).Should().BeFalse("kiracı verisi yine de silinmeli");
    }

    [Fact]
    public async Task Delete_WithWrongSlug_Returns409_AndKeepsData()
    {
        var t = await ProvisionAndActivateAsync("Yanlış Onay A.Ş.", $"w-{Guid.NewGuid():N}@purge.local");

        var resp = await PlatformClient().DeleteAsync($"/api/tenants/{t.TenantId}?confirmSlug=yanlis-slug");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = Factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        (await platform.Tenants.AnyAsync(x => x.Id == t.TenantId)).Should().BeTrue("yanlış slug'da silinmemeli");
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
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
        (await platform.Tenants.AnyAsync(x => x.Id == t.TenantId)).Should().BeFalse("silinmiş olmalı");
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
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            var t = await platform.Tenants.FirstAsync(x => x.Name == "Doğrulanmamış A.Ş.");
            unverifiedId = t.Id;
            t.CreatedAtUtc = DateTime.UtcNow.AddDays(-30);
            await platform.SaveChangesAsync(CancellationToken.None);
        }

        // Doğrulanmış aktif kiracı (korunmalı).
        var kept = await ProvisionAndActivateAsync("Aktif A.Ş.", $"a-{Guid.NewGuid():N}@cleanup.local");

        var resp = await PlatformClient().PostAsync("/api/tenants/cleanup-unverified?olderThanDays=7", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            (await platform.Tenants.AnyAsync(x => x.Id == unverifiedId)).Should().BeFalse("doğrulanmamış temizlenmeli");
            (await platform.Tenants.AnyAsync(x => x.Id == kept.TenantId)).Should().BeTrue("aktif kiracı korunmalı");
        }
    }
}
