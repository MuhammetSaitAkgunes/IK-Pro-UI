using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;

namespace IKPro.Application.Features.Tenancy;

/// <summary>
/// Kiracı + ilk hr-admin oluşturmanın ortak adımları (provizyon ve self-servis paylaşır).
/// Admin ŞİFRESİZ oluşturulur; davet e-postası CreateTenantAdminAsync içinde gönderilir.
/// Çağıran slug'ı (provizyonda istemciden, kayıtta türetilmiş) ve hedef durumu belirler.
/// </summary>
public static class TenantOnboarding
{
    public static async Task<Tenant> CreateWithAdminAsync(
        IPlatformDbContext platform,
        IIdentityService identityService,
        string companyName,
        string slug,
        string adminName,
        string adminEmail,
        TenantStatus hedefDurum,
        CancellationToken cancellationToken)
    {
        // Admin e-postasını önce doğrula — kiracı yazılmadan çakışmayı yakala (orphan önlenir).
        if (await identityService.EmailExistsAsync(adminEmail, cancellationToken))
        {
            throw new ConflictException($"'{adminEmail}' e-postasıyla kayıtlı bir hesap zaten var.");
        }

        var tenant = new Tenant
        {
            Name = companyName,
            Slug = slug,
            Status = hedefDurum,
            CreatedAtUtc = DateTime.UtcNow,
        };
        platform.Tenants.Add(tenant);
        await platform.SaveChangesAsync(cancellationToken);

        await identityService.CreateTenantAdminAsync(tenant.Id, adminName, adminEmail, tenant.Name, cancellationToken);
        return tenant;
    }
}
