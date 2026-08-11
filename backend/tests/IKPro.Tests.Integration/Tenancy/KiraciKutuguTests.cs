using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Kiracı kütüğü: durum bellekte önbelleklenir ki her istek platform
/// veritabanına gitmesin. Ama durum değiştiğinde ANINDA düşmeli — dondurma
/// işleminin bir sonraki istekte etkili olması buna bağlı.
/// </summary>
[Collection(ApiCollection.Name)]
public class KiraciKutuguTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task Kutuk_KiracininDurumunuDoner()
    {
        var kiraci = await ProvisionAndActivateAsync("Kutuk", $"kut-{Guid.NewGuid():N}@ornek.local");

        using var scope = Factory.Services.CreateScope();
        var kutuk = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();

        (await kutuk.GetStatusAsync(kiraci.TenantId, default)).Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task Kutuk_VarOlmayanKiraciIcinNullDoner()
    {
        using var scope = Factory.Services.CreateScope();
        var kutuk = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();

        (await kutuk.GetStatusAsync(-999, default)).Should().BeNull();
    }

    [Fact]
    public async Task Kutuk_DurumDegisipDusurulunce_YeniDurumuDoner()
    {
        var kiraci = await ProvisionAndActivateAsync("KutukDusur", $"kdz-{Guid.NewGuid():N}@ornek.local");

        // Önce önbelleğe girsin.
        using (var scope = Factory.Services.CreateScope())
        {
            var kutuk = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
            (await kutuk.GetStatusAsync(kiraci.TenantId, default)).Should().Be(TenantStatus.Active);
        }

        // Veritabanında değiştir ve kütüğü düşür.
        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            var satir = await platform.Tenants.FirstAsync(t => t.Id == kiraci.TenantId);
            satir.Status = TenantStatus.Frozen;
            await platform.SaveChangesAsync(default);

            scope.ServiceProvider.GetRequiredService<ITenantRegistry>().Invalidate(kiraci.TenantId);
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var kutuk = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
            (await kutuk.GetStatusAsync(kiraci.TenantId, default)).Should().Be(TenantStatus.Frozen,
                "düşürülen kayıt bir sonraki okumada veritabanından tazelenmeli");
        }
    }
}
