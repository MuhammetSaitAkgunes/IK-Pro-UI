namespace IKPro.Domain.Entities.Tenancy;

/// <summary>
/// Bir müşteri şirketi (kiracı). Multi-tenant izolasyonun köküdür.
/// Bilinçli olarak <c>BaseEntity</c>'den TÜREMEZ — kendisi kiracıya bağlı değildir
/// (kiracının kiracısı olmaz), bu yüzden global TenantId filtresine tabi tutulmaz.
/// </summary>
public class Tenant
{
    public int Id { get; set; }

    /// <summary>Görünen şirket adı, ör. "Acme Teknoloji A.Ş.".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL/alt alan dostu benzersiz kısa ad, ör. "acme".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Yaşam döngüsü durumu. Yalnız <see cref="TenantStatus.Active"/> erişime izin verir.
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

    public DateTime CreatedAtUtc { get; set; }
}
