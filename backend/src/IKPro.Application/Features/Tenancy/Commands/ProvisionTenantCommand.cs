using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Commands;

/// <summary>
/// Yeni bir müşteri şirketi (kiracı) ve onun ilk <c>hr-admin</c> hesabını oluşturur.
/// Platform seviyesi bir işlemdir (normal rol sisteminin dışında); TenancyController
/// bunu platform anahtarıyla korur. Faz 1: geçici şifre; Faz 5: davet + şifre-belirleme.
/// </summary>
public sealed record ProvisionTenantCommand(
    string CompanyName,
    string Slug,
    string AdminName,
    string AdminEmail) : IRequest<ProvisionTenantResult>;

public sealed record ProvisionTenantResult(int TenantId, string Slug, string AdminEmail);

public sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug)
            .NotEmpty().MaximumLength(64)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug yalnız küçük harf, rakam ve tire içerebilir (ör. 'acme').");
        RuleFor(x => x.AdminName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class ProvisionTenantCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    : IRequestHandler<ProvisionTenantCommand, ProvisionTenantResult>
{
    public async Task<ProvisionTenantResult> Handle(
        ProvisionTenantCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            throw new ConflictException($"'{slug}' kısa adıyla bir şirket zaten var.");
        }

        // Admin e-postasını önce doğrula — kiracı yazılmadan çakışmayı yakala (orphan önlenir).
        if (await identityService.EmailExistsAsync(request.AdminEmail, cancellationToken))
        {
            throw new ConflictException($"'{request.AdminEmail}' e-postasıyla kayıtlı bir hesap zaten var.");
        }

        var tenant = new Tenant
        {
            Name = request.CompanyName.Trim(),
            Slug = slug,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        await identityService.CreateTenantAdminAsync(
            tenant.Id, request.AdminName.Trim(), request.AdminEmail.Trim(), tenant.Name, cancellationToken);

        return new ProvisionTenantResult(tenant.Id, tenant.Slug, request.AdminEmail.Trim());
    }
}
