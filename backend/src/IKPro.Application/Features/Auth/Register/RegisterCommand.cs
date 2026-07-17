using FluentValidation;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Constants;
using MediatR;

namespace IKPro.Application.Features.Auth.Register;

/// <summary>
/// Anonim (self-servis) kayıt. Güvenlik: istemci rol seçemez — her zaman en düşük
/// yetkili rol (employee) atanır. Yükseltilmiş roller yalnız seed veya yetkili
/// yönetim akışıyla verilir (bu uç kimlik doğrulaması istemez).
/// </summary>
public sealed record RegisterCommand(string Name, string Email, string Password)
    : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class RegisterCommandHandler(IIdentityService identityService)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => identityService.RegisterAsync(
            request.Name, request.Email, request.Password, Roles.Employee, cancellationToken);
}
