using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;

namespace IKPro.Application.Features.Tenancy;

/// <summary>
/// Kiracı + ilk hr-admin oluşturmanın ortak adımları (provizyon ve self-servis paylaşır).
/// Admin ŞİFRESİZ oluşturulur; davet e-postası CreateTenantAdminAsync içinde gönderilir.
/// Çağıran slug'ı (provizyonda istemciden, kayıtta türetilmiş) ve aktiflik durumunu belirler.
/// </summary>
public static class TenantOnboarding
{
    public static async Task<Tenant> CreateWithAdminAsync(
        IApplicationDbContext context,
        IIdentityService identityService,
        string companyName,
        string slug,
        string adminName,
        string adminEmail,
        bool isActive,
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
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        await identityService.CreateTenantAdminAsync(tenant.Id, adminName, adminEmail, tenant.Name, cancellationToken);
        return tenant;
    }
}
