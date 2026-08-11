using IKPro.Domain.Entities.Tenancy;

namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// Kiracı kütüğü: kiracının durumunu bellekte önbellekler.
///
/// Neden var: erişim kapısı her istekte kiracının durumunu soruyor. Bunu her
/// seferinde platform veritabanından okumak, ayırdığımız katmanı her istekte
/// yeniden birleştirmek olurdu.
///
/// Faz 2'de bu kütük katalog adını da taşıyacak; bugün yalnız durum yeterli
/// çünkü tüm kiracılar aynı veritabanını paylaşıyor.
/// </summary>
public interface ITenantRegistry
{
    /// <summary>Kiracının durumu; kiracı yoksa <c>null</c>.</summary>
    Task<TenantStatus?> GetStatusAsync(int tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Önbellek kaydını düşürür. Durum değiştiren her yol bunu çağırmalı —
    /// dondurmanın bir sonraki istekte etkili olması buna bağlıdır, süre
    /// dolmasını beklemeyiz.
    /// </summary>
    void Invalidate(int tenantId);
}
