namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// Kiracı verisini kalıcı siler (KVKK unutulma hakkı). Tüm ITenantScoped tablolar +
/// kullanıcılar + refresh token'lar + fiziksel dosyalar + kiracı satırı, tek transaction.
/// Yalnız hedef kiracı etkilenir. Infrastructure'da EF metadata'sıyla implemente edilir.
/// </summary>
public interface ITenantPurger
{
    /// <summary>Verili kiracının tüm verisini siler.</summary>
    Task PurgeAsync(int tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Doğrulanmamış self-servis kiracıları siler: pasif + <paramref name="createdBeforeUtc"/>'den
    /// eski + hiçbir kullanıcısı şifre belirlememiş (davet hiç kabul edilmemiş). Şifreli
    /// kullanıcısı olan (askıya alınmış) kiracılar korunur. Silinen kiracı sayısını döndürür.
    /// </summary>
    Task<int> PurgeUnverifiedAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken);
}
