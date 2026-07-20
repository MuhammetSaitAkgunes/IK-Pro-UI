using FluentValidation;
using IKPro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Commands;

/// <summary>
/// Self-servis kayıt: müşteri kendi şirketini ve ilk hr-admin'ini public formdan oluşturur.
/// Platform anahtarı GEREKMEZ; kötüye kullanım 'signup' rate-limit'iyle sınırlanır.
/// Kiracı PASİF oluşturulur; admin davet e-postasını kabul edince (accept-invite) etkinleşir.
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

public sealed class RegisterTenantCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var slug = await GenerateUniqueSlugAsync(request.CompanyName, cancellationToken);

        var tenant = await TenantOnboarding.CreateWithAdminAsync(
            context, identityService,
            request.CompanyName.Trim(), slug,
            request.AdminName.Trim(), request.AdminEmail.Trim(),
            isActive: false, cancellationToken);

        return new RegisterTenantResult(tenant.Slug, request.AdminEmail.Trim());
    }

    // Türetilmiş slug'ı benzersizleştir: çakışırsa "-2", "-3", ... ekle.
    private async Task<string> GenerateUniqueSlugAsync(string companyName, CancellationToken cancellationToken)
    {
        var baseSlug = TenantSlug.From(companyName);
        var candidate = baseSlug;
        var suffix = 2;
        while (await context.Tenants.AnyAsync(t => t.Slug == candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }
        return candidate;
    }
}
