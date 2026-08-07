using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Tenancy.Commands;

/// <summary>
/// Bir kiracının tüm verisini kalıcı siler (KVKK unutulma hakkı). Yıkıcıdır:
/// <c>ConfirmSlug</c> hedef kiracının slug'ıyla eşleşmezse reddedilir (yanlış-id koruması).
/// </summary>
public sealed record PurgeTenantCommand(int TenantId, string ConfirmSlug) : IRequest<PurgeTenantResult>;

public sealed record PurgeTenantResult(int TenantId, string Slug);

public sealed class PurgeTenantCommandValidator : AbstractValidator<PurgeTenantCommand>
{
    public PurgeTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ConfirmSlug).NotEmpty();
    }
}

public sealed class PurgeTenantCommandHandler(IPlatformDbContext platform, ITenantPurger purger)
    : IRequestHandler<PurgeTenantCommand, PurgeTenantResult>
{
    public async Task<PurgeTenantResult> Handle(PurgeTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await platform.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Kiracı", request.TenantId);

        if (!string.Equals(tenant.Slug, request.ConfirmSlug.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Onay kısa adı (slug) kiracıyla eşleşmiyor; silme iptal edildi.");
        }

        var slug = tenant.Slug;
        await purger.PurgeAsync(tenant.Id, cancellationToken);
        return new PurgeTenantResult(request.TenantId, slug);
    }
}
