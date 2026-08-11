using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace IKPro.Infrastructure.Tenancy;

/// <summary>
/// Kütüğün önbelleği SINGLETON'dır (tüm istekler paylaşır), ama platform
/// context'i SCOPED'dır. Bu yüzden okuma anında kendi kapsamını açar —
/// singleton'ın scoped bir bağımlılığı yakalaması (captive dependency)
/// bağlantıyı ilk isteğe yapıştırırdı.
/// </summary>
public sealed class TenantRegistry(IServiceScopeFactory scopeFactory, IMemoryCache cache) : ITenantRegistry
{
    // Kısa TTL yalnız emniyet ağıdır: asıl tazeleme Invalidate ile ANINDA olur.
    // Bir yol Invalidate çağırmayı unutursa değişiklik en geç bu süre içinde görünür.
    private static readonly TimeSpan Omur = TimeSpan.FromMinutes(5);

    private static string Anahtar(int tenantId) => $"kiraci-durum-{tenantId}";

    public async Task<TenantStatus?> GetStatusAsync(int tenantId, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(Anahtar(tenantId), out TenantStatus? onbellekli))
        {
            return onbellekli;
        }

        using var scope = scopeFactory.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();

        var durum = await platform.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => (TenantStatus?)t.Status)
            .FirstOrDefaultAsync(cancellationToken);

        cache.Set(Anahtar(tenantId), durum, Omur);
        return durum;
    }

    public void Invalidate(int tenantId) => cache.Remove(Anahtar(tenantId));
}
