namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// Kiracıdan veritabanı bağlantı dizesine giden ayrılma noktası.
///
/// Faz 1b'de tüm kiracılar aynı veritabanını paylaştığı için <see cref="ResolveFor"/>
/// parametresi KULLANILMAZ — ama imzada durur, çünkü ayrılma noktası budur: Faz 2'de
/// burası Tenants.DatabaseName'den katalog adı üretecek ve çağıranların hiçbiri
/// değişmeyecek.
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>
    /// Kiracının veritabanı bağlantı dizesi.
    ///
    /// Faz 1b'de tüm kiracılar aynı veritabanını paylaştığı için parametre
    /// KULLANILMAZ — ama imzada durur, çünkü ayrılma noktası budur: Faz 2'de
    /// burası Tenants.DatabaseName'den katalog adı üretecek ve çağıranların
    /// hiçbiri değişmeyecek.
    /// </summary>
    string ResolveFor(int? tenantId);
}
