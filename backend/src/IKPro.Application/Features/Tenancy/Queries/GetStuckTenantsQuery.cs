using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Queries;

/// <summary>
/// Provizyonu ya da silmesi yarıda kalmış kiracıları listeler.
///
/// Kiracı satırı platform veritabanında, verisi uygulama veritabanında olduğu
/// için bu iki iş tek transaction değildir. Yarıda kalan bir iş sessiz enkaz
/// bırakmamalı; bu sorgu onu görünür kılar.
/// </summary>
public sealed record GetStuckTenantsQuery(int OlderThanMinutes) : IRequest<IReadOnlyList<StuckTenantDto>>;

public sealed record StuckTenantDto(int TenantId, string Slug, TenantStatus Status, DateTime CreatedAtUtc);

public sealed class GetStuckTenantsQueryHandler(IPlatformDbContext platform)
    : IRequestHandler<GetStuckTenantsQuery, IReadOnlyList<StuckTenantDto>>
{
    public async Task<IReadOnlyList<StuckTenantDto>> Handle(
        GetStuckTenantsQuery request, CancellationToken cancellationToken)
    {
        var esik = DateTime.UtcNow.AddMinutes(-request.OlderThanMinutes);

        return await platform.Tenants
            .Where(t => (t.Status == TenantStatus.Provisioning || t.Status == TenantStatus.Purging)
                        && t.CreatedAtUtc < esik)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new StuckTenantDto(t.Id, t.Slug, t.Status, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
