using IKPro.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IKPro.Infrastructure.Tenancy;

/// <summary>
/// <see cref="ITenantConnectionResolver"/>'ın Faz 1b uygulaması: kiracıdan
/// bağımsız olarak her zaman <c>ConnectionStrings:DefaultConnection</c>'ı döner.
///
/// Bu, kiracı-başına-veritabanı geçişinin (Faz 2) ayrılma noktasıdır — tesisat
/// burada kurulur, ama veri henüz bölünmez. <paramref name="tenantId"/> bu yüzden
/// şimdilik kullanılmaz; imzada durmasının nedeni çağıranların Faz 2'de
/// değişmeyecek olmasıdır.
/// </summary>
public sealed class TenantConnectionResolver(IConfiguration configuration) : ITenantConnectionResolver
{
    public string ResolveFor(int? tenantId)
    {
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil.");
    }
}
