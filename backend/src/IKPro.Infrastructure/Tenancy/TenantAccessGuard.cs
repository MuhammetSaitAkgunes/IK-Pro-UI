using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.Extensions.Logging;

namespace IKPro.Infrastructure.Tenancy;

/// <summary>
/// Erişim kapısının tek uygulaması: kütükten (<see cref="ITenantRegistry"/>) durumu
/// okur, yalnız <see cref="TenantStatus.Active"/> geçer.
///
/// Kiracı bulunamazsa (kütük <c>null</c> döner) da reddeder — var olmayan
/// kiracıya erişim verilmez. Bilinçli bir sınır: kütük "yok" sonucunu da
/// önbelleğe alıyor (bkz. <see cref="TenantRegistry"/>), yani teorik olarak bir
/// kiracı sağlanmadan hemen önce sorgulanmışsa "yok" cevabı TTL dolana kadar
/// (5 dk) yapışık kalabilir. Gerçekçi bulunmadı (provizyon anında henüz hiçbir
/// kullanıcı token isteyemez) — ek kod (ör. negatif sonuç için ayrı/kısa TTL)
/// bilinçli olarak yazılmadı.
/// </summary>
public sealed class TenantAccessGuard(
    ITenantRegistry registry,
    ILogger<TenantAccessGuard> logger) : ITenantAccessGuard
{
    private const string KullaniciMesaji = "Şirket hesabınız şu anda kullanıma kapalı. Yöneticinizle iletişime geçin.";

    public async Task EnsureAccessibleAsync(int tenantId, CancellationToken cancellationToken)
    {
        var durum = await registry.GetStatusAsync(tenantId, cancellationToken);

        if (durum == TenantStatus.Active)
        {
            return;
        }

        // Gerçek durum yalnız loga yazılır — kullanıcıya iç durum adı (Frozen/Purging)
        // sızdırılmaz, "kullanıma kapalı" yeterli ve doğru düzeyde bilgi verir.
        logger.LogWarning(
            "Kiracı erişimi reddedildi: TenantId={TenantId}, Durum={Durum}",
            tenantId, durum?.ToString() ?? "bulunamadı");

        throw new TenantInaccessibleException(KullaniciMesaji);
    }
}
