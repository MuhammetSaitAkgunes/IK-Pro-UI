using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Commands;

/// <summary>
/// Self-servis kayıt: müşteri kendi şirketini ve ilk hr-admin'ini public formdan oluşturur.
/// Platform anahtarı GEREKMEZ; kötüye kullanım 'signup' rate-limit'iyle sınırlanır.
/// Kiracı <see cref="TenantStatus.Provisioning"/> ile oluşturulur; admin davet e-postasını
/// kabul edince (accept-invite) <see cref="TenantStatus.Active"/>'e geçer.
/// </summary>
public sealed record RegisterTenantCommand(string CompanyName, string AdminName, string AdminEmail)
    : IRequest<RegisterTenantResult>;

public sealed record RegisterTenantResult(string Slug, string AdminEmail);

public sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class RegisterTenantCommandHandler(IPlatformDbContext platform, IIdentityService identityService)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private const int MaxSlugAttempts = 5;

    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var adminEmail = request.AdminEmail.Trim();
        // E-postayı önce doğrula — kiracı yazılmadan çakışmayı yakala (orphan önlenir).
        if (await identityService.EmailExistsAsync(adminEmail, cancellationToken))
        {
            throw new ConflictException($"'{adminEmail}' e-postasıyla kayıtlı bir hesap zaten var.");
        }

        var companyName = request.CompanyName.Trim();
        var tenant = await CreateTenantWithUniqueSlugAsync(companyName, cancellationToken);

        // E-posta, admin kullanıcı oluşturulmadan ÖNCE rezerve edilir — TenantOnboarding
        // ile aynı desen (bkz. TenantOnboarding.CreateWithAdminAsync): eşzamanlı iki kayıt
        // aynı adresi alamaz. CreateTenantAdminAsync'in içindeki dizin yazımı idempotent
        // olduğu için burada rezerve edilmiş olması onu 409'a düşürmez (aynı kiracı → no-op).
        await identityService.ReserveEmailAsync(adminEmail, tenant.Id, cancellationToken);

        await identityService.CreateTenantAdminAsync(
            tenant.Id, request.AdminName.Trim(), adminEmail, tenant.Name, cancellationToken);

        return new RegisterTenantResult(tenant.Slug, adminEmail);
    }

    // Kiracıyı benzersiz slug'la yazar. Eşzamanlı bir kayıt aynı slug'ı kaparsa
    // (unique index ihlali → DbUpdateException) izlenen eklemeyi bırakıp yeniden dener;
    // yeniden denemede kısa rastgele ekli slug ile hızla yakınsar. Provizyon yolu
    // (istemci-slug + 409) TenantOnboarding'de kalır — semantiği farklıdır.
    private async Task<Tenant> CreateTenantWithUniqueSlugAsync(string companyName, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var slug = await GenerateSlugAsync(companyName, attempt, cancellationToken);
            var tenant = new Tenant
            {
                Name = companyName,
                Slug = slug,
                Status = TenantStatus.Provisioning,
                CreatedAtUtc = DateTime.UtcNow,
            };
            platform.Tenants.Add(tenant);
            try
            {
                await platform.SaveChangesAsync(cancellationToken);
                return tenant;
            }
            catch (DbUpdateException) when (attempt < MaxSlugAttempts)
            {
                // Added durumundaki başarısız eklemeyi bırak (Remove → Detached, silme SQL'i üretmez).
                platform.Tenants.Remove(tenant);
            }
        }
    }

    private async Task<string> GenerateSlugAsync(string companyName, int attempt, CancellationToken cancellationToken)
    {
        var baseSlug = TenantSlug.From(companyName);

        if (attempt > 1)
        {
            // Yeniden denemede eşzamanlı çakışmayı hızla kır: kısa rastgele ek (≤64 için base kırpılır).
            var trimmed = baseSlug.Length > 56 ? baseSlug[..56] : baseSlug;
            return $"{trimmed}-{Guid.NewGuid():N}"[..(trimmed.Length + 7)];
        }

        // İlk deneme: temiz base; sıralı çakışmada "-2", "-3", ...
        var candidate = baseSlug;
        var suffix = 2;
        while (await platform.Tenants.AnyAsync(t => t.Slug == candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }
        return candidate;
    }
}
