using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;

namespace IKPro.Application.Features.Tenancy;

/// <summary>
/// Kiracı + ilk hr-admin oluşturmanın adımları — <c>ProvisionTenantCommand</c> (platform-key'li
/// operatör provizyonu) bunu doğrudan çağırır. Admin ŞİFRESİZ oluşturulur; davet e-postası
/// CreateTenantAdminAsync içinde gönderilir. Çağıran slug'ı (istemciden) ve hedef durumu belirler.
///
/// DİKKAT — İKİ KOPYA VAR: <c>RegisterTenantCommand</c> (public self-servis kayıt) bu metodu
/// ÇAĞIRMAZ; slug-çakışmasında yeniden deneme döngüsü (<c>CreateTenantWithUniqueSlugAsync</c>)
/// gerektiği için aynı "önce rezerve et, sonra admin oluştur" sırasını kendi içinde ayrıca
/// uygular. İki yolu değiştirirken ikisini de güncellemeyi unutma.
/// </summary>
public static class TenantOnboarding
{
    public static async Task<Tenant> CreateWithAdminAsync(
        IPlatformDbContext platform,
        IIdentityService identityService,
        ITenantDirectory directory,
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

        // Kiracı satırı ve uygulama verisi (admin kullanıcı) iki ayrı veritabanındadır,
        // dolayısıyla tek transaction değildir. Kiracı burada DAİMA Provisioning ile
        // yazılır — hedefDurum yalnız aşağıda, admin oluşturulduktan SONRA uygulanır.
        var tenant = new Tenant
        {
            Name = companyName,
            Slug = slug,
            Status = TenantStatus.Provisioning,
            CreatedAtUtc = DateTime.UtcNow,
        };
        platform.Tenants.Add(tenant);
        await platform.SaveChangesAsync(cancellationToken);

        // E-posta, admin kullanıcı oluşturulmadan ÖNCE rezerve edilir: eşzamanlı
        // iki kayıt aynı adresi alamaz. Rezervasyonu kullanıcı oluşturmaya
        // bıraksaydık iki müşteri yarışabilirdi.
        await directory.ReserveAsync(adminEmail, tenant.Id, cancellationToken);

        await identityService.CreateTenantAdminAsync(tenant.Id, adminName, adminEmail, tenant.Name, cancellationToken);

        // Son adım: kiracı ancak burada kullanılabilir hale gelir. Araya giren
        // bir hata kiracıyı Provisioning'de bırakır — erişilemez ama GÖRÜNÜR,
        // ve operatör yeniden deneyebilir ya da geri alabilir.
        tenant.Status = hedefDurum;
        await platform.SaveChangesAsync(cancellationToken);

        return tenant;
    }
}
