using FluentAssertions;
using IKPro.API.Services;
using IKPro.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Zamanlanmış temizlik: BackgroundService'in tek geçişi (RunOnceAsync) doğrulanmamış
/// eski kiracıları siler, aktif/doğrulanmış kiracıları korur. Timer'dan bağımsız test.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ScheduledCleanupTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task RunOnce_PurgesUnverified_KeepsActive()
    {
        // Doğrulanmamış (davet kabul edilmemiş) self-servis kiracı, tarihi geçmişe çekilmiş.
        var email = $"sched-{Guid.NewGuid():N}@cleanup.local";
        (await Factory.CreateClient().PostAsJsonAsync("/api/tenants/signup",
            new { companyName = "Zamanlı Temizlik A.Ş.", adminName = "X", adminEmail = email }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        int unverifiedId;
        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            var t = await platform.Tenants.FirstAsync(x => x.Name == "Zamanlı Temizlik A.Ş.");
            unverifiedId = t.Id;
            t.CreatedAtUtc = DateTime.UtcNow.AddDays(-40);
            await platform.SaveChangesAsync(CancellationToken.None);
        }

        var kept = await ProvisionAndActivateAsync("Zamanlı Aktif A.Ş.", $"k-{Guid.NewGuid():N}@cleanup.local");

        var options = Options.Create(new UnverifiedTenantCleanupOptions
        {
            Enabled = true,
            OlderThanDays = 7,
        });
        var service = new UnverifiedTenantCleanupService(
            Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<UnverifiedTenantCleanupService>.Instance);

        await service.RunOnceAsync(CancellationToken.None);

        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            (await platform.Tenants.AnyAsync(x => x.Id == unverifiedId)).Should().BeFalse("doğrulanmamış temizlenmeli");
            (await platform.Tenants.AnyAsync(x => x.Id == kept.TenantId)).Should().BeTrue("aktif kiracı korunmalı");
        }
    }

    [Fact]
    public async Task Start_WithNonPositiveInterval_DoesNotThrow()
    {
        // Yanlış yapılandırma (IntervalHours=0) host'u çökertmemeli — servis sessizce devre dışı kalır.
        var options = Options.Create(new UnverifiedTenantCleanupOptions
        {
            Enabled = true,
            IntervalHours = 0,
            OlderThanDays = 7,
        });
        var service = new UnverifiedTenantCleanupService(
            Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<UnverifiedTenantCleanupService>.Instance);

        var start = async () =>
        {
            await ((IHostedService)service).StartAsync(CancellationToken.None);
            await ((IHostedService)service).StopAsync(CancellationToken.None);
        };
        await start.Should().NotThrowAsync("IntervalHours<=0 yapılandırması BackgroundService'i çökertmemeli");
    }
}
