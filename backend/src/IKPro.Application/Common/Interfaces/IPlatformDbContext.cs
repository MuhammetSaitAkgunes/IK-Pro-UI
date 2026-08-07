using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// Platform veritabanı: kiracı kimliği ve yönlendirme. Kiracı İK verisi burada
/// DEĞİLDİR — o <see cref="IApplicationDbContext"/> tarafındadır.
///
/// Bu context'te global kiracı filtresi YOKTUR: platform tablolarının kendisi
/// kiracı-üstüdür. Kiracıya göre süzmek gerektiğinde açıkça yazılır.
/// </summary>
public interface IPlatformDbContext
{
    DbSet<Tenant> Tenants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
