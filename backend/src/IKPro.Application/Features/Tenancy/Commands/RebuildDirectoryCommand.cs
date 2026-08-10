using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Commands;

/// <summary>
/// Bir kiracının yönlendirme dizinini kiracı veritabanındaki kullanıcılardan
/// yeniden kurar.
///
/// Neden var: dizin TÜRETİLMİŞ bir tablodur. Bir geri yüklemeden sonra platform
/// veritabanı geri sarılmadığı için dizin, geri yüklenmiş kiracı veritabanıyla
/// sapabilir — dizinde olup kiracıda olmayan kullanıcılar kalabilir. Bu komut
/// sapmayı kalıcı olmaktan çıkarır ve geri yükleme prosedürünün zorunlu adımıdır.
/// </summary>
public sealed record RebuildDirectoryCommand(int TenantId) : IRequest<RebuildDirectoryResult>;

public sealed record RebuildDirectoryResult(int TenantId, int YazilanKayit);

public sealed class RebuildDirectoryCommandHandler(
    IPlatformDbContext platform,
    IUserDirectorySource kullanicilar)
    : IRequestHandler<RebuildDirectoryCommand, RebuildDirectoryResult>
{
    public async Task<RebuildDirectoryResult> Handle(
        RebuildDirectoryCommand request, CancellationToken cancellationToken)
    {
        var epostalar = await kullanicilar.NormalizedEmailsAsync(request.TenantId, cancellationToken);

        var mevcut = await platform.Directory
            .Where(d => d.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        platform.Directory.RemoveRange(mevcut);

        foreach (var eposta in epostalar)
        {
            platform.Directory.Add(new TenantDirectoryEntry
            {
                NormalizedEmail = eposta,
                TenantId = request.TenantId,
            });
        }

        await platform.SaveChangesAsync(cancellationToken);
        return new RebuildDirectoryResult(request.TenantId, epostalar.Count);
    }
}
