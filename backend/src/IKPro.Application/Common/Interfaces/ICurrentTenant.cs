namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// İstek bağlamındaki aktif kiracı (müşteri şirket). JWT <c>tenant</c> claim'inden
/// çözülür. <c>AppDbContext</c> global query filter ve audit interceptor bunu kullanır.
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Aktif kiracı kimliği; kimlik doğrulanmamış/kiracısız bağlamda <c>null</c>.</summary>
    int? TenantId { get; }

    /// <summary>Kiracı zorunlu olan bağlamda; yoksa anlamlı hata.</summary>
    int TenantIdOrThrow();

    /// <summary>
    /// Platform/seed işlemleri için aktif kiracıyı geçici olarak sabitler (impersonation).
    /// HTTP isteği dışında (ör. veri tohumlama) global filtre ve damgalamanın doğru
    /// kiracıya çalışmasını sağlar.
    /// </summary>
    void Impersonate(int tenantId);
}
