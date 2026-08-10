using IKPro.Application.Common.Exceptions;
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

/// <param name="CakisanEpostalar">
/// Geri yüklenen kiracının bir kullanıcısıyla AYNI e-postayı hâlâ BAŞKA bir
/// kiracının adına tutan, bu yüzden atlanan satırlar. Boş değilse operatör elle
/// müdahale etmeli — "tek e-posta = tek kiracı" kuralı gevşetilmez, çakışan
/// e-posta bu kiracıya devredilmez.
/// </param>
public sealed record RebuildDirectoryResult(int TenantId, int YazilanKayit, IReadOnlyList<string> CakisanEpostalar);

public sealed class RebuildDirectoryCommandHandler(
    IPlatformDbContext platform,
    IUserDirectorySource kullanicilar)
    : IRequestHandler<RebuildDirectoryCommand, RebuildDirectoryResult>
{
    public async Task<RebuildDirectoryResult> Handle(
        RebuildDirectoryCommand request, CancellationToken cancellationToken)
    {
        var tenantVarMi = await platform.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantVarMi)
        {
            throw new NotFoundException("Kiracı", request.TenantId);
        }

        var epostalar = await kullanicilar.NormalizedEmailsAsync(request.TenantId, cancellationToken);

        var mevcut = await platform.Directory
            .Where(d => d.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        platform.Directory.RemoveRange(mevcut);

        // Yazmadan ÖNCE çakışmayı denetle: bu, tam da geri yükleme sonrası sapma
        // senaryosudur — geri yüklenen kullanıcılardan biri, dizinde HÂLÂ başka bir
        // kiracıya kayıtlı bir e-postayı taşıyabilir. NormalizedEmail birincil anahtar
        // olduğu için ham bir Add + SaveChanges tek satırda çakışsa bile TÜM
        // SaveChangesAsync çağrısını (bu kiracının diğer geçerli satırları dahil)
        // geri alırdı — kurtarma aracının en çok gerektiği anda hiçbir şey yapmadan
        // çökmesi demektir. Bunun yerine çakışan satır atlanır, kalanlar yazılır ve
        // atlananlar sonuçta açıkça raporlanır; operatör hangi e-postaların elle
        // müdahale gerektirdiğini görür.
        var digerKiracilardaki = await platform.Directory
            .Where(d => d.TenantId != request.TenantId && epostalar.Contains(d.NormalizedEmail))
            .Select(d => d.NormalizedEmail)
            .ToListAsync(cancellationToken);
        var cakisanlar = new HashSet<string>(digerKiracilardaki);

        var atlanan = new List<string>();
        foreach (var eposta in epostalar)
        {
            if (cakisanlar.Contains(eposta))
            {
                atlanan.Add(eposta);
                continue;
            }

            platform.Directory.Add(new TenantDirectoryEntry
            {
                NormalizedEmail = eposta,
                TenantId = request.TenantId,
            });
        }

        await platform.SaveChangesAsync(cancellationToken);
        return new RebuildDirectoryResult(request.TenantId, epostalar.Count - atlanan.Count, atlanan);
    }
}
